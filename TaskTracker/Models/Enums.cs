using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models;

public enum AppTaskStatus
{
    [Display(Name = "To Do")]        Todo       = 0,
    [Display(Name = "In Progress")]  InProgress = 1,
    [Display(Name = "Done")]         Done       = 2
}

public enum AppTaskPriority
{
    [Display(Name = "Low")]    Low    = 0,
    [Display(Name = "Medium")] Medium = 1,
    [Display(Name = "High")]   High   = 2
}
