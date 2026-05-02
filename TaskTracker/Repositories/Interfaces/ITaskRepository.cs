using TaskTracker.Models;

namespace TaskTracker.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?>                                GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<TaskItem> Items, int Total)> GetAllAsync(
        int currentUserId,
        AppTaskStatus?   status   = null,
        AppTaskPriority? priority = null,
        int page         = 1,
        int pageSize     = 20,
        CancellationToken ct = default);
    Task<int> CreateAsync(TaskItem task, CancellationToken ct = default);
    Task      UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task      DeleteAsync(int id, CancellationToken ct = default);
}
