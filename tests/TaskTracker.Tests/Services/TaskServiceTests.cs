using Moq;
using TaskTracker.Models;
using TaskTracker.Models.ViewModels;
using TaskTracker.Repositories.Interfaces;
using TaskTracker.Services;
using Xunit;

namespace TaskTracker.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repo;
    private readonly TaskService           _service;

    public TaskServiceTests()
    {
        _repo    = new Mock<ITaskRepository>();
        _service = new TaskService(_repo.Object);
    }

    private static TaskItem MakeTask(int id, int creatorId, int? assigneeId = null) => new()
    {
        Id = id, Title = "Task", CreatedByUserId = creatorId, AssignedToUserId = assigneeId,
        Status = AppTaskStatus.Todo, Priority = AppTaskPriority.Medium,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    // ─── GetOwnedByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnedByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetOwnedByIdAsync(1, 100));
        Assert.Equal("Task 1 not found.", ex.Message);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_NotCreatorOrAssignee_ThrowsUnauthorized()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50, assigneeId: 75));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetOwnedByIdAsync(1, userId: 99));
        Assert.Equal("You do not have access to this task.", ex.Message);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_CreatorAccess_RequireCreatorFalse_Succeeds()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50, assigneeId: 75));

        var result = await _service.GetOwnedByIdAsync(1, userId: 50, requireCreator: false);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_AssigneeAccess_RequireCreatorFalse_Succeeds()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50, assigneeId: 75));

        var result = await _service.GetOwnedByIdAsync(1, userId: 75, requireCreator: false);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_CreatorAccess_RequireCreatorTrue_Succeeds()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50));

        var result = await _service.GetOwnedByIdAsync(1, userId: 50, requireCreator: true);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetOwnedByIdAsync_AssigneeOnly_RequireCreatorTrue_ThrowsUnauthorized()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50, assigneeId: 75));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetOwnedByIdAsync(1, userId: 75, requireCreator: true));
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AllFields_MappedCorrectly()
    {
        var model = new TaskFormViewModel
        {
            Title = "New", Description = "Desc", Status = AppTaskStatus.InProgress,
            Priority = AppTaskPriority.High, DueDate = new DateTime(2027, 1, 1),
            AssignedToUserId = 200
        };
        _repo.Setup(r => r.CreateAsync(It.IsAny<TaskItem>())).ReturnsAsync(42);

        var result = await _service.CreateAsync(userId: 100, model);

        Assert.Equal(42, result);
        _repo.Verify(r => r.CreateAsync(It.Is<TaskItem>(t =>
            t.Title == "New" &&
            t.Description == "Desc" &&
            t.Status == AppTaskStatus.InProgress &&
            t.Priority == AppTaskPriority.High &&
            t.DueDate == new DateTime(2027, 1, 1) &&
            t.AssignedToUserId == 200 &&
            t.CreatedByUserId == 100
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_TimestampsSetToUtcNow()
    {
        var model = new TaskFormViewModel { Title = "T", Status = AppTaskStatus.Todo, Priority = AppTaskPriority.Low };
        TaskItem? captured = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<TaskItem>()))
             .Callback<TaskItem>(t => captured = t)
             .ReturnsAsync(1);

        var before = DateTime.UtcNow;
        await _service.CreateAsync(1, model);
        var after = DateTime.UtcNow;

        Assert.NotNull(captured);
        Assert.InRange(captured!.CreatedAt, before, after);
        Assert.InRange(captured.UpdatedAt, before, after);
        Assert.Equal(captured.CreatedAt, captured.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_NullDescription_AllowsNull()
    {
        var model = new TaskFormViewModel { Title = "T", Description = null, Status = AppTaskStatus.Todo, Priority = AppTaskPriority.Low };
        _repo.Setup(r => r.CreateAsync(It.IsAny<TaskItem>())).ReturnsAsync(1);

        await _service.CreateAsync(1, model);

        _repo.Verify(r => r.CreateAsync(It.Is<TaskItem>(t => t.Description == null)), Times.Once);
    }

    // ─── UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(1, userId: 100, new TaskFormViewModel()));
    }

    [Fact]
    public async Task UpdateAsync_NotCreator_ThrowsUnauthorized()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateAsync(1, userId: 99, new TaskFormViewModel { Title = "X" }));
    }

    [Fact]
    public async Task UpdateAsync_Creator_UpdatesFieldsAndTimestamp()
    {
        var task = MakeTask(1, creatorId: 100);
        task.UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(task);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        await _service.UpdateAsync(1, userId: 100, new TaskFormViewModel
        {
            Title = "Updated", Description = "D", Status = AppTaskStatus.Done,
            Priority = AppTaskPriority.High, DueDate = new DateTime(2027, 6, 1),
            AssignedToUserId = 300
        });

        Assert.Equal("Updated", task.Title);
        Assert.Equal("D", task.Description);
        Assert.Equal(AppTaskStatus.Done, task.Status);
        Assert.Equal(AppTaskPriority.High, task.Priority);
        Assert.Equal(300, task.AssignedToUserId);
        Assert.True(task.UpdatedAt >= before);
        _repo.Verify(r => r.UpdateAsync(task), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CreatedByUserIdPreserved()
    {
        var task = MakeTask(1, creatorId: 100);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(task);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(1, userId: 100, new TaskFormViewModel { Title = "X" });

        Assert.Equal(100, task.CreatedByUserId);
    }

    // ─── DeleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1, 100));
    }

    [Fact]
    public async Task DeleteAsync_NotCreator_ThrowsUnauthorized()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 50));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteAsync(1, userId: 99));
    }

    [Fact]
    public async Task DeleteAsync_Creator_CallsRepositoryDelete()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, creatorId: 100));
        _repo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, userId: 100);

        _repo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_OwnershipCheckedBeforeDeletion()
    {
        var order = new List<string>();
        _repo.Setup(r => r.GetByIdAsync(1))
             .Callback(() => order.Add("get"))
             .ReturnsAsync(MakeTask(1, creatorId: 100));
        _repo.Setup(r => r.DeleteAsync(1))
             .Callback(() => order.Add("delete"))
             .Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, userId: 100);

        Assert.Equal(["get", "delete"], order);
    }

    // ─── GetAllForUserAsync (paginated) ─────────────────────────────────────

    [Fact]
    public async Task GetAllForUserAsync_NoFilters_DelegatesToRepo()
    {
        var tasks = new List<TaskItem> { MakeTask(1, 100), MakeTask(2, 100) };
        _repo.Setup(r => r.GetAllAsync(100, null, null, 1, 20))
             .ReturnsAsync((tasks, 2));

        var (items, total) = await _service.GetAllForUserAsync(100);

        Assert.Equal(2, items.Count());
        Assert.Equal(2, total);
        _repo.Verify(r => r.GetAllAsync(100, null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllForUserAsync_WithStatusFilter_DelegatesToRepo()
    {
        _repo.Setup(r => r.GetAllAsync(100, AppTaskStatus.Done, null, 1, 20))
             .ReturnsAsync((Enumerable.Empty<TaskItem>(), 0));

        var (items, total) = await _service.GetAllForUserAsync(100, status: AppTaskStatus.Done);

        Assert.Empty(items);
        Assert.Equal(0, total);
        _repo.Verify(r => r.GetAllAsync(100, AppTaskStatus.Done, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllForUserAsync_WithBothFilters_DelegatesToRepo()
    {
        _repo.Setup(r => r.GetAllAsync(100, AppTaskStatus.InProgress, AppTaskPriority.High, 1, 20))
             .ReturnsAsync((Enumerable.Empty<TaskItem>(), 0));

        await _service.GetAllForUserAsync(100, AppTaskStatus.InProgress, AppTaskPriority.High);

        _repo.Verify(r => r.GetAllAsync(100, AppTaskStatus.InProgress, AppTaskPriority.High, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllForUserAsync_CustomPageParams_PassedToRepo()
    {
        _repo.Setup(r => r.GetAllAsync(100, null, null, 3, 50))
             .ReturnsAsync((Enumerable.Empty<TaskItem>(), 120));

        var (_, total) = await _service.GetAllForUserAsync(100, page: 3, pageSize: 50);

        Assert.Equal(120, total);
        _repo.Verify(r => r.GetAllAsync(100, null, null, 3, 50), Times.Once);
    }

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsTask()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeTask(1, 100));

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((TaskItem?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }
}
