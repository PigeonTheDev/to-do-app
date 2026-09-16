using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkRequests.Api.Data;
using WorkRequests.Api.Models;

namespace WorkRequests.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string database = Path.Combine(Path.GetTempPath(), $"requests-test-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Requests"] = $"Data Source={database};Pooling=False" }));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(database)) File.Delete(database);
    }
}

public class RequestsTests
{
    [Fact]
    public async Task ExistingDatabaseUpgrade_PreservesRecordsAndAllowsNewRequests()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<RequestsDbContext>().UseSqlite(connection).Options;
        await using var db = new RequestsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Requests ADD COLUMN Priority INTEGER NOT NULL DEFAULT 1");
        db.Requests.Add(new WorkRequest { Title = "Existing request", Description = "Keep this record", Department = "IT" });
        await db.SaveChangesAsync();

        await DatabaseInitializer.InitializeAsync(db);
        await DatabaseInitializer.InitializeAsync(db);
        Assert.Equal("Existing request", (await db.Requests.AsNoTracking().SingleAsync()).Title);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Requests') WHERE name = 'Priority'";
        Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        db.Requests.Add(new WorkRequest { Title = "New request", Description = "Can still create", Department = "IT" });
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.Requests.CountAsync());
    }

    private static object Input(string title = "WORK REQUEST", string department = "IT", string? status = null) =>
        new { title, description = "The projector is not working.", department, status };

    [Fact]
    public async Task CreateEditSearchAndAdvance_PersistsAndFiltersCorrectly()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var created = await client.PostAsJsonAsync("/api/requests", Input());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(item.TryGetProperty("priority", out _));
        var id = item.GetProperty("id").GetInt32();
        Assert.Equal("New", item.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);

        var search = await client.GetFromJsonAsync<JsonElement>("/api/requests?search=" + Uri.EscapeDataString("work request"));
        Assert.Equal(1, search.GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Updated title", "Operations"))).StatusCode);
        var saved = await client.GetFromJsonAsync<JsonElement>($"/api/requests/{id}");
        Assert.Equal("Operations", saved.GetProperty("department").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Updated title", "Operations", "Completed"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Updated title", "Operations", "InProgress"))).StatusCode);
        var filtered = await client.GetFromJsonAsync<JsonElement>("/api/requests?status=New");
        Assert.Equal(0, filtered.GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Updated title", "Operations", "Completed"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Updated title", "Operations", "New"))).StatusCode);
        Assert.Equal("Completed", (await client.GetFromJsonAsync<JsonElement>($"/api/requests/{id}")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requests/{id}", Input("Edited after completion"))).StatusCode);
        Assert.Equal("Completed", (await client.GetFromJsonAsync<JsonElement>($"/api/requests/{id}")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/requests/{id}", Input(status: "Completed"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/requests/{id}/status", new { status = "Completed" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/requests/{id}", Input(status: "Invalid"))).StatusCode);
    }

    [Theory]
    [InlineData("", "IT")]
    [InlineData("   ", "IT")]
    [InlineData("Title", " ")]
    public async Task RequiredFields_AreValidated(string title, string department)
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/requests", Input(title, department));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task InvalidInputsAndMissingRecords_ReturnUsefulErrors()
    {
        using var app = new ApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/requests", new { title = "x", department = "IT" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/requests", new { title = "x", department = "IT", description = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/requests", Input(new string('a', 121)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/requests?status=99")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/requests/999", Input())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/requests/999", Input("Updated title", "Operations", "InProgress"))).StatusCode);
    }
}
