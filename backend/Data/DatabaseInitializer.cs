using Microsoft.EntityFrameworkCore;

namespace WorkRequests.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(RequestsDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await db.Database.OpenConnectionAsync();
        try
        {
            // EnsureCreated does not update databases created by earlier versions.
            var connection = db.Database.GetDbConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Requests') WHERE name = 'Priority'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) > 0)
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Requests DROP COLUMN Priority");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
