using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkRequests.Api.Contracts;
using WorkRequests.Api.Data;
using WorkRequests.Api.Models;

namespace WorkRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController(RequestsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WorkRequest>>> List(string? search, RequestStatus? status, CancellationToken ct)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            return Problem(statusCode: 400, title: "Select a valid status.");

        var query = db.Requests.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var requests = await query.OrderByDescending(x => x.Id).ToListAsync(ct);

        return requests;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkRequest>> Get(int id, CancellationToken ct)
    {
        var request = await db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return request is null ? Problem(statusCode: 404, title: "Request not found.") : Ok(request);
    }

    [HttpPost]
    public async Task<ActionResult<WorkRequest>> Create(RequestInput input, CancellationToken ct)
    {
        if (input.Status.HasValue && input.Status != RequestStatus.New)
            return Problem(statusCode: 400, title: "New requests must start with New status.");
        var request = new WorkRequest();
        Apply(input, request);
        db.Requests.Add(request);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkRequest>> Update(int id, RequestInput input, CancellationToken ct)
    {
        var request = await db.Requests.FindAsync([id], ct);
        if (request is null) return Problem(statusCode: 404, title: "Request not found.");
        var next = input.Status ?? request.Status;
        if (next != request.Status && (int)next != (int)request.Status + 1)
            return Problem(statusCode: 409, title: "Status must follow the sequence New → In Progress → Completed.");
        var changed = await db.Requests.Where(x => x.Id == id && x.Status == request.Status)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Title, input.Title.Trim())
                .SetProperty(x => x.Description, input.Description.Trim())
                .SetProperty(x => x.Department, input.Department.Trim())
                .SetProperty(x => x.Status, next), ct);
        if (changed == 0) return Problem(statusCode: 409, title: "The request has changed. Refresh the list and try again.");
        Apply(input, request);
        request.Status = next;
        return Ok(request);
    }

    private static void Apply(RequestInput input, WorkRequest request)
    {
        request.Title = input.Title.Trim();
        request.Description = input.Description.Trim();
        request.Department = input.Department.Trim();

    }
}
