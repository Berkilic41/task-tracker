using TaskTracker.Models;

namespace TaskTracker.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?>             GetByIdAsync(int id);
    Task<IEnumerable<TaskItem>> GetAllAsync(AppTaskStatus? status = null, AppTaskPriority? priority = null);
    Task<int>                   CreateAsync(TaskItem task);
    Task                        UpdateAsync(TaskItem task);
    Task                        DeleteAsync(int id);
}
