using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Models.ViewModels;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ITaskRepository    _tasks;
    private readonly IUserRepository    _users;
    private readonly ICommentRepository _comments;

    public TasksController(
        ITaskRepository    tasks,
        IUserRepository    users,
        ICommentRepository comments)
    {
        _tasks    = tasks;
        _users    = users;
        _comments = comments;
    }

    // GET /Tasks  ?status=&priority=
    public async Task<IActionResult> Index(int? status, int? priority)
    {
        AppTaskStatus?   statusEnum   = status.HasValue   ? (AppTaskStatus)status.Value     : null;
        AppTaskPriority? priorityEnum = priority.HasValue ? (AppTaskPriority)priority.Value : null;

        var tasks = await _tasks.GetAllAsync(statusEnum, priorityEnum);

        ViewBag.StatusFilter   = status;
        ViewBag.PriorityFilter = priority;
        return View(tasks);
    }

    // GET /Tasks/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) return NotFound();

        task.Comments = (await _comments.GetByTaskIdAsync(id)).ToList();
        return View(task);
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

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var now    = DateTime.UtcNow;

        await _tasks.CreateAsync(new TaskItem
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

        TempData["Success"] = "Task created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Tasks/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) return NotFound();

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

    // POST /Tasks/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _users.GetAllAsync();
            return View(model);
        }

        var task = await _tasks.GetByIdAsync(id);
        if (task is null) return NotFound();

        task.Title            = model.Title;
        task.Description      = model.Description;
        task.Status           = model.Status;
        task.Priority         = model.Priority;
        task.DueDate          = model.DueDate;
        task.AssignedToUserId = model.AssignedToUserId;
        task.UpdatedAt        = DateTime.UtcNow;

        await _tasks.UpdateAsync(task);
        TempData["Success"] = "Task updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Tasks/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) return NotFound();
        return View(task);
    }

    // POST /Tasks/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _tasks.DeleteAsync(id);
        TempData["Success"] = "Task deleted.";
        return RedirectToAction(nameof(Index));
    }
}
