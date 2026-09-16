using Microsoft.EntityFrameworkCore;
using WorkRequests.Api.Models;

namespace WorkRequests.Api.Data;

public class RequestsDbContext(DbContextOptions<RequestsDbContext> options) : DbContext(options)
{
    public DbSet<WorkRequest> Requests => Set<WorkRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<WorkRequest>();
        request.Property(x => x.Title).HasMaxLength(120).IsRequired();
        request.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        request.Property(x => x.Department).HasMaxLength(80).IsRequired();
    }
}
