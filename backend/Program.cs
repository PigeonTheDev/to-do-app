using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WorkRequests.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<RequestsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Requests") ?? "Data Source=requests.db"));

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = 500;
    await Results.Problem(statusCode: 500, title: "The operation could not be completed. Please try again.")
        .ExecuteAsync(context);
}));
app.UseStatusCodePages();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
}
app.Run();

public partial class Program { }
