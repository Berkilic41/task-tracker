using TaskTracker.Models;

namespace TaskTracker.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByTaskIdAsync(int taskId);
    Task<int>                  CreateAsync(Comment comment);
    Task                       DeleteAsync(int id);
}
