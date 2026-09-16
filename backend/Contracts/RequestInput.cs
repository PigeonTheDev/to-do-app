using System.ComponentModel.DataAnnotations;
using WorkRequests.Api.Models;

namespace WorkRequests.Api.Contracts;

public sealed class RequestInput
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(120, ErrorMessage = "Title must not exceed 120 characters.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, ErrorMessage = "Description must not exceed 2000 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(80, ErrorMessage = "Department must not exceed 80 characters.")]
    public string Department { get; set; } = "";

    [EnumDataType(typeof(RequestStatus), ErrorMessage = "Select a valid status.")]
    public RequestStatus? Status { get; set; }
}
