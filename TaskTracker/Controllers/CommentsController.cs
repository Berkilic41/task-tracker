using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private const int MaxCommentLength = 2000;

    private readonly ICommentRepository _comments;

    public CommentsController(ICommentRepository comments) => _comments = comments;

    private int GetUserId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        return userId;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int taskId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaxCommentLength)
        {
            TempData["Error"] = $"Comment must be between 1 and {MaxCommentLength} characters.";
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        var userId = GetUserId();
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
        var userId = GetUserId();
        var comment = await _comments.GetByIdAsync(id);

        if (comment is null || comment.TaskId != taskId)
            return NotFound();

        if (comment.UserId != userId)
            return Forbid();

        await _comments.DeleteAsync(id);
        return RedirectToAction("Details", "Tasks", new { id = taskId });
    }
}
