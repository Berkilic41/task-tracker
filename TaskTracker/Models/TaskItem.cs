namespace TaskTracker.Models;

public class TaskItem
{
    public int             Id                 { get; set; }
    public string          Title              { get; set; } = string.Empty;
    public string?         Description        { get; set; }
    public AppTaskStatus   Status             { get; set; }
    public AppTaskPriority Priority           { get; set; }
    public DateTime?       DueDate            { get; set; }
    public DateTime        CreatedAt          { get; set; }
    public DateTime        UpdatedAt          { get; set; }
    public int             CreatedByUserId    { get; set; }
    public int?            AssignedToUserId   { get; set; }

    // Joined display fields
    public string?         CreatedByUsername  { get; set; }
    public string?         AssignedToUsername { get; set; }

    public List<Comment>   Comments           { get; set; } = [];
}
