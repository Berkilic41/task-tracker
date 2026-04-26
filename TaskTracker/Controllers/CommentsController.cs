using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ICommentRepository _comments;

    public CommentsController(ICommentRepository comments) => _comments = comments;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int taskId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _comments.CreateAsync(new Comment
        {
            TaskId    = taskId,
            UserId    = userId,
            Content   = content.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        return RedirectToAction("Details", "Tasks", new { id = taskId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int taskId)
    {
        await _comments.DeleteAsync(id);
        return RedirectToAction("Details", "Tasks", new { id = taskId });
    }
}
