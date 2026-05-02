using TaskTracker.Models;
using TaskTracker.Models.ViewModels;

namespace TaskTracker.Services.Interfaces;

public interface ITaskService
{
    Task<(IEnumerable<TaskItem> Items, int Total)> GetAllForUserAsync(
        int userId,
        AppTaskStatus?   status   = null,
        AppTaskPriority? priority = null,
        int page     = 1,
        int pageSize = 20);
    Task<TaskItem?>             GetByIdAsync(int id);
    Task<TaskItem>              GetOwnedByIdAsync(int id, int userId, bool requireCreator = false);
    Task<int>                   CreateAsync(int userId, TaskFormViewModel model);
    Task                        UpdateAsync(int id, int userId, TaskFormViewModel model);
    Task                        DeleteAsync(int id, int userId);
}
