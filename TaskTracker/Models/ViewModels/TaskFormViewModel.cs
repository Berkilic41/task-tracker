using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models.ViewModels;

public class TaskFormViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
    public string? Description { get; set; }

    [Required]
    public AppTaskStatus Status { get; set; } = AppTaskStatus.Todo;

    [Required]
    public AppTaskPriority Priority { get; set; } = AppTaskPriority.Medium;

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Assign To")]
    public int? AssignedToUserId { get; set; }
}
