using TaskTracker.Models;
using TaskTracker.Models.ViewModels;
using TaskTracker.Repositories.Interfaces;
using TaskTracker.Services.Interfaces;

namespace TaskTracker.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository       _tasks;
    private readonly ILogger<TaskService>  _logger;

    public TaskService(ITaskRepository tasks, ILogger<TaskService> logger)
    {
        _tasks  = tasks;
        _logger = logger;
    }

    public Task<(IEnumerable<TaskItem> Items, int Total)> GetAllForUserAsync(
        int userId,
        AppTaskStatus?   status   = null,
        AppTaskPriority? priority = null,
        int page     = 1,
        int pageSize = 20)
        => _tasks.GetAllAsync(userId, status, priority, page, pageSize);

    public Task<TaskItem?> GetByIdAsync(int id) => _tasks.GetByIdAsync(id);

    public async Task<TaskItem> GetOwnedByIdAsync(int id, int userId, bool requireCreator = false)
    {
        var task = await _tasks.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        bool canAccess = requireCreator
            ? task.CreatedByUserId == userId
            : task.CreatedByUserId == userId || task.AssignedToUserId == userId;

        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized access to task {TaskId}", userId, id);
            throw new UnauthorizedAccessException("You do not have access to this task.");
        }

        return task;
    }

    public async Task<int> CreateAsync(int userId, TaskFormViewModel model)
    {
        _logger.LogInformation("User {UserId} creating task '{Title}'", userId, model.Title);
        var now = DateTime.UtcNow;
        var id = await _tasks.CreateAsync(new TaskItem
        {
            Title            = model.Title,
            Description      = model.Description,
            Status           = model.Status,
            Priority         = model.Priority,
            DueDate          = model.DueDate,
            AssignedToUserId = model.AssignedToUserId,
            CreatedByUserId  = userId,
            CreatedAt        = now,
            UpdatedAt        = now
        });
        _logger.LogInformation("Task {TaskId} created by user {UserId}", id, userId);
        return id;
    }

    public async Task UpdateAsync(int id, int userId, TaskFormViewModel model)
    {
        var task = await GetOwnedByIdAsync(id, userId, requireCreator: true);
        task.Title            = model.Title;
        task.Description      = model.Description;
        task.Status           = model.Status;
        task.Priority         = model.Priority;
        task.DueDate          = model.DueDate;
        task.AssignedToUserId = model.AssignedToUserId;
        task.UpdatedAt        = DateTime.UtcNow;
        await _tasks.UpdateAsync(task);
        _logger.LogInformation("Task {TaskId} updated by user {UserId}", id, userId);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        await GetOwnedByIdAsync(id, userId, requireCreator: true);
        await _tasks.DeleteAsync(id);
        _logger.LogInformation("Task {TaskId} deleted by user {UserId}", id, userId);
    }
}
