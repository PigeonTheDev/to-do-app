namespace WorkRequests.Api.Models;


public enum RequestStatus { New, InProgress, Completed }

public class WorkRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Department { get; set; } = "";

    public RequestStatus Status { get; set; } = RequestStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
