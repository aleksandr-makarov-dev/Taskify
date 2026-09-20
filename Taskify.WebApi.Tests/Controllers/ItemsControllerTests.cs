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

    [Fact(DisplayName = "Getting all items returns 200 OK with all items")]
    public async Task GetItems_ReturnsAllItems()
    {
        // Arrange
        var firstRequest = new CreateItemRequest
        {
            Name = "Test_Name_1",
            Description = "Test_Description_1",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var secondRequest = new CreateItemRequest
        {
            Name = "Test_Name_2",
            Description = null,
            Priority = Priority.High,
            DueDateOnUtc = null
        };

        await _httpClient.PostAsJsonAsync("/api/v1/items", firstRequest, TestContext.Current.CancellationToken);

        await _httpClient.PostAsJsonAsync("/api/v1/items", secondRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await _httpClient.GetAsync("/api/v1/items", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var items =
            await response.Content.ReadFromJsonAsync<List<ItemResponse>>(TestContext.Current.CancellationToken);

        items.ShouldNotBeNull();
        items.Count.ShouldBe(2);

        var firstItem = items[0];
        firstItem.Name.ShouldBe(firstRequest.Name);
        firstItem.Priority.ShouldBe(firstRequest.Priority);
        // firstItem.DueDateOnUtc.ShouldBe(firstRequest.DueDateOnUtc);

        var secondItem = items[1];
        secondItem.Name.ShouldBe(secondRequest.Name);
        secondItem.Priority.ShouldBe(secondRequest.Priority);
        // secondItem.DueDateOnUtc.ShouldBe(secondRequest.DueDateOnUtc);
    }

    [Fact(DisplayName = "Getting an existing item returns 200 OK with item details")]
    public async Task GetItem_ReturnsItemDetails()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var createResponse =
            await _httpClient.PostAsJsonAsync("/api/v1/items", request, TestContext.Current.CancellationToken);

        var createdItem =
            await createResponse.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();

        // Act
        var response =
            await _httpClient.GetAsync($"/api/v1/items/{createdItem.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var item = await response.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        item.ShouldNotBeNull();
        item.Id.ShouldBe(createdItem.Id);
        item.Name.ShouldBe(request.Name);
        item.Description.ShouldBe(request.Description);
        item.Priority.ShouldBe(request.Priority);
        // item.DueDateOnUtc.ShouldBe(request.DueDateOnUtc);
    }

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
        var response =
            await _httpClient.PostAsJsonAsync("/api/v1/items", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var createdItem =
            await response.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();
        createdItem.Name.ShouldBe(request.Name);
        createdItem.Description.ShouldBe(request.Description);
        createdItem.Priority.ShouldBe(request.Priority);
        // createdItem.DueDateOnUtc.ShouldBe(request.DueDateOnUtc);
    }

    [Fact(DisplayName = "Updating a valid item returns 204 NoContent")]
    public async Task UpdateItem_Returns204NoContent()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var createResponse =
            await _httpClient.PostAsJsonAsync("/api/v1/items", request, TestContext.Current.CancellationToken);

        var createdItem =
            await createResponse.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();

        var dueDateOnUtc = DateTime.UtcNow.AddMinutes(5);

        var updateRequest = new UpdateItemRequest
        {
            Name = "Test_Name_Updated",
            Description = "Test_Description_Updated",
            Priority = Priority.Medium,
            DueDateOnUtc = dueDateOnUtc
        };

        // Act

        var itemId = createdItem.Id;

        var result =
            await _httpClient.PutAsJsonAsync($"/api/v1/items/{itemId}", updateRequest,
                TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updatedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == itemId, CancellationToken.None));

        updatedItem.ShouldNotBeNull();
        updatedItem.Name.ShouldBe(updateRequest.Name);
        updatedItem.Description.ShouldBe(updateRequest.Description);
        updatedItem.Priority.ShouldBe(updateRequest.Priority);
        // updatedItem.DueDateOnUtc.ShouldBe(updateRequest.DueDateOnUtc);
    }

    [Fact(DisplayName = "Completing an existing item returns 204 NoContent and marks item as complete")]
    public async Task CompleteItem_Returns204NoContent()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var createResponse =
            await _httpClient.PostAsJsonAsync("/api/v1/items", request, TestContext.Current.CancellationToken);

        var createdItem =
            await createResponse.Content.ReadFromJsonAsync<ItemDetailsResponse>(TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();

        var itemId = createdItem.Id;

        // Act
        var response = await _httpClient.PutAsync($"/api/v1/items/{itemId}/complete", null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var completedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == itemId, CancellationToken.None));

        completedItem.ShouldNotBeNull();
        completedItem.IsComplete.ShouldBeTrue();
        completedItem.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Deleting an existing item returns 204 NoContent and removes the item")]
    public async Task DeleteItem_Returns204NoContent()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test_Name",
            Description = "Test_Description",
            Priority = Priority.Low,
            DueDateOnUtc = factory.TimeProvider.GetUtcNow().UtcDateTime.AddHours(1)
        };

        var createResponse =
            await _httpClient.PostAsJsonAsync(
                "/api/v1/items",
                request,
                TestContext.Current.CancellationToken);

        var createdItem =
            await createResponse.Content.ReadFromJsonAsync<ItemDetailsResponse>(
                TestContext.Current.CancellationToken);

        createdItem.ShouldNotBeNull();

        var itemId = createdItem.Id;

        // Act
        var response =
            await _httpClient.DeleteAsync(
                $"/api/v1/items/{itemId}",
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var deletedItem = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.Items
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == itemId, CancellationToken.None));

        deletedItem.ShouldNotBeNull();
        deletedItem.IsDeleted.ShouldBeTrue();
        deletedItem.DeletedAtUtc.ShouldNotBeNull();
    }
}