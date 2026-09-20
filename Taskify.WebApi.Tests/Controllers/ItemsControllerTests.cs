using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Contracts.Responses;
using Taskify.WebApi.Domain;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Tests.Controllers;

[Collection(nameof(TestWebApplicationCollection))]
public class ItemsControllerTests(TestWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _httpClient = factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region GET /api/v1/items

    [Fact(DisplayName = "Getting all items returns 200 OK with non-deleted items")]
    public async Task GetItems_ReturnsAllActiveItems()
    {
        // Arrange
        var firstItem = new Item
        {
            Name = "Test_Name_1",
            Description = "Test_Description_1",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var secondItem = new Item
        {
            Name = "Test_Name_2",
            Description = null,
            Priority = Priority.High,
            DueDateOnUtc = null
        };

        var softDeletedItem = new Item
        {
            Name = "SoftDeleted_Name",
            Priority = Priority.Medium,
            IsDeleted = true,
            DeletedAtUtc = factory.TimeProvider.GetUtcNow().UtcDateTime
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.AddRange([firstItem, secondItem, softDeletedItem]);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.GetAsync("/api/v1/items", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ItemResponse>>(TestContext.Current.CancellationToken);

        items.ShouldNotBeNull();
        items.Count.ShouldBe(2);
        items.ShouldContain(x => x.Name == firstItem.Name);
        items.ShouldContain(x => x.Name == secondItem.Name);
        items.ShouldNotContain(x => x.Name == softDeletedItem.Name);
    }

    [Fact(DisplayName = "Getting an existing item returns 200 OK with item details")]
    public async Task GetItem_ReturnsItemDetails()
    {
        // Arrange
        var item = new Item
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.GetAsync($"/api/v1/items/{item.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(item.Id);
        result.Name.ShouldBe(item.Name);
        result.Description.ShouldBe(item.Description);
        result.Priority.ShouldBe(item.Priority);
    }

    #endregion

    #region POST /api/v1/items

    [Fact(DisplayName = "Creating a valid item returns 200 OK with full item details")]
    public async Task CreateItem_ReturnsCreatedItem()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/items", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var createdItem = await response.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();
        createdItem.Name.ShouldBe(request.Name);
        createdItem.Description.ShouldBe(request.Description);
        createdItem.Priority.ShouldBe(request.Priority);
        
        var persistedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == createdItem.Id, CancellationToken.None));

        persistedItem.ShouldNotBeNull();
    }

    #endregion

    #region PUT /api/v1/items/{id}

    [Fact(DisplayName = "Updating a valid item returns 204 NoContent")]
    public async Task UpdateItem_Returns204NoContent()
    {
        // Arrange
        var existingItem = new Item
        {
            Name = "Original_Name",
            Description = "Original_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(existingItem);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        var updateRequest = new UpdateItemRequest
        {
            Name = "Test_Name_Updated",
            Description = "Test_Description_Updated",
            Priority = Priority.Medium,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(5)
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            $"/api/v1/items/{existingItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updatedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == existingItem.Id, CancellationToken.None));

        updatedItem.ShouldNotBeNull();
        updatedItem.Name.ShouldBe(updateRequest.Name);
        updatedItem.Description.ShouldBe(updateRequest.Description);
        updatedItem.Priority.ShouldBe(updateRequest.Priority);
    }

    #endregion

    #region PUT /api/v1/items/{id}/complete

    [Fact(DisplayName = "Completing an existing item returns 204 NoContent and marks item as complete")]
    public async Task CompleteItem_Returns204NoContent()
    {
        // Arrange
        var existingItem = new Item
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            IsComplete = false
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(existingItem);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.PutAsync(
            $"/api/v1/items/{existingItem.Id}/complete",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var completedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == existingItem.Id, CancellationToken.None));

        completedItem.ShouldNotBeNull();
        completedItem.IsComplete.ShouldBeTrue();
        completedItem.CompletedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region DELETE /api/v1/items/{id}

    [Fact(DisplayName = "Deleting an existing item returns 204 NoContent and soft-deletes the item")]
    public async Task DeleteItem_Returns204NoContent()
    {
        // Arrange
        var existingItem = new Item
        {
            Name = "Test_Name",
            Priority = Priority.Low,
            IsDeleted = false
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(existingItem);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.DeleteAsync(
            $"/api/v1/items/{existingItem.Id}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var deletedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == existingItem.Id, CancellationToken.None));

        deletedItem.ShouldNotBeNull();
        deletedItem.IsDeleted.ShouldBeTrue();
        deletedItem.DeletedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region POST /api/v1/items/{id}/restore

    [Fact(DisplayName = "Restoring a soft-deleted item returns 204 NoContent and unmarks soft-delete")]
    public async Task RestoreItem_WhenItemIsSoftDeleted_Returns204NoContentAndRestoresItem()
    {
        // Arrange
        var softDeletedItem = new Item
        {
            Name = "Deleted_Item",
            Priority = Priority.Low,
            IsDeleted = true,
            DeletedAtUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10)
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(softDeletedItem);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.PostAsync(
            $"/api/v1/items/{softDeletedItem.Id}/restore",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var restoredItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == softDeletedItem.Id, CancellationToken.None));

        restoredItem.ShouldNotBeNull();
        restoredItem.IsDeleted.ShouldBeFalse();
        restoredItem.DeletedAtUtc.ShouldBeNull();
    }

    [Fact(DisplayName = "Restoring an active or non-existent item returns 404 NotFound")]
    public async Task RestoreItem_WhenItemIsNotSoftDeletedOrDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var activeItem = new Item
        {
            Name = "Active_Item",
            Priority = Priority.Low,
            IsDeleted = false
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.Add(activeItem);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
        
        var responseForActive = await _httpClient.PostAsync(
            $"/api/v1/items/{activeItem.Id}/restore",
            null,
            TestContext.Current.CancellationToken);
        
        var responseForNonExistent = await _httpClient.PostAsync(
            $"/api/v1/items/{Guid.NewGuid()}/restore",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        responseForActive.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        responseForNonExistent.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/v1/items/trash

    [Fact(DisplayName = "Getting trash items returns 200 OK with only soft-deleted items sorted descending by DeletedAtUtc")]
    public async Task GetTrashItems_ReturnsSoftDeletedItemsOrderedByDeletedAtUtcDescending()
    {
        // Arrange
        var baseUtc = factory.TimeProvider.GetUtcNow().UtcDateTime;

        var olderDeletedItem = new Item
        {
            Name = "Older_Deleted_Item",
            Priority = Priority.Low,
            IsDeleted = true,
            DeletedAtUtc = baseUtc.AddMinutes(-20)
        };

        var newerDeletedItem = new Item
        {
            Name = "Newer_Deleted_Item",
            Priority = Priority.High,
            IsDeleted = true,
            DeletedAtUtc = baseUtc.AddMinutes(-5)
        };

        var activeItem = new Item
        {
            Name = "Active_Item",
            Priority = Priority.Medium,
            IsDeleted = false
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.AddRange([olderDeletedItem, newerDeletedItem, activeItem]);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        var response = await _httpClient.GetAsync("/api/v1/items/trash", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trashItems = await response.Content.ReadFromJsonAsync<List<ItemResponse>>(TestContext.Current.CancellationToken);

        trashItems.ShouldNotBeNull();
        trashItems.Count.ShouldBe(2);
        
        trashItems[0].Name.ShouldBe(newerDeletedItem.Name);
        trashItems[1].Name.ShouldBe(olderDeletedItem.Name);
        
        trashItems.ShouldNotContain(x => x.Name == activeItem.Name);
    }

    #endregion
}