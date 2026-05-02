using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Models.ViewModels;
using TaskTracker.Repositories.Interfaces;
using TaskTracker.Services.Interfaces;

namespace TaskTracker.Controllers;

[Authorize]
public class TasksController : Controller
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize     = 100;

    private readonly ITaskService       _taskService;
    private readonly IUserRepository    _users;
    private readonly ICommentRepository _comments;

    public TasksController(ITaskService taskService, IUserRepository users, ICommentRepository comments)
    {
        _taskService = taskService;
        _users       = users;
        _comments    = comments;
    }

    private int GetUserId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        return userId;
    }

    // GET /Tasks  ?status=&priority=&page=&pageSize=
    public async Task<IActionResult> Index(int? status, int? priority, int page = 1, int pageSize = DefaultPageSize)
    {
        var userId       = GetUserId();
        pageSize         = Math.Clamp(pageSize, 1, MaxPageSize);
        AppTaskStatus?   statusEnum   = status.HasValue   ? (AppTaskStatus)status.Value     : null;
        AppTaskPriority? priorityEnum = priority.HasValue ? (AppTaskPriority)priority.Value : null;

        var (tasks, total) = await _taskService.GetAllForUserAsync(userId, statusEnum, priorityEnum, page, pageSize);

        ViewBag.StatusFilter   = status;
        ViewBag.PriorityFilter = priority;
        ViewBag.Page           = page;
        ViewBag.PageSize       = pageSize;
        ViewBag.TotalCount     = total;
        ViewBag.TotalPages     = (int)Math.Ceiling(total / (double)pageSize);
        return View(tasks);
    }

    // GET /Tasks/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var task = await _taskService.GetOwnedByIdAsync(id, GetUserId(), requireCreator: false);
            task.Comments = (await _comments.GetByTaskIdAsync(id)).ToList();
            return View(task);
        }
        catch (KeyNotFoundException)        { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // GET /Tasks/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Users = await _users.GetAllAsync();
        return View(new TaskFormViewModel());
    }

    // POST /Tasks/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _users.GetAllAsync();
            return View(model);
        }

        await _taskService.CreateAsync(GetUserId(), model);
        TempData["Success"] = "Task created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Tasks/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var task = await _taskService.GetOwnedByIdAsync(id, GetUserId(), requireCreator: true);
            ViewBag.Users = await _users.GetAllAsync();
            return View(new TaskFormViewModel
            {
                Title            = task.Title,
                Description      = task.Description,
                Status           = task.Status,
                Priority         = task.Priority,
                DueDate          = task.DueDate,
                AssignedToUserId = task.AssignedToUserId
            });
        }
        catch (KeyNotFoundException)        { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // POST /Tasks/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _users.GetAllAsync();
            return View(model);
        }

        try
        {
            await _taskService.UpdateAsync(id, GetUserId(), model);
            TempData["Success"] = "Task updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException)        { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // GET /Tasks/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var task = await _taskService.GetOwnedByIdAsync(id, GetUserId(), requireCreator: true);
            return View(task);
        }
        catch (KeyNotFoundException)        { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // POST /Tasks/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _taskService.DeleteAsync(id, GetUserId());
            TempData["Success"] = "Task deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)        { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
