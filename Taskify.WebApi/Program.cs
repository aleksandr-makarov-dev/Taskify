using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Extensions;
using Taskify.WebApi.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityAndAuthentication(builder.Configuration);

builder.Services.AddInfrastructure();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    await dbContext.Database.MigrateAsync();

    if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
    {
        await roleManager.CreateAsync(new Role(RoleNames.Admin));
    }

    if (!await roleManager.RoleExistsAsync(RoleNames.User))
    {
        await roleManager.CreateAsync(new Role(RoleNames.User));
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();