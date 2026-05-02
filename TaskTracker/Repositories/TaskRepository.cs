using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly DbConnectionFactory    _db;
    private readonly ILogger<TaskRepository> _logger;

    private const string SelectColumns = @"
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
        t.CreatedAt, t.UpdatedAt, t.CreatedByUserId, t.AssignedToUserId,
        u1.Username AS CreatedByUsername, u2.Username AS AssignedToUsername";

    private const string FromJoins = @"
        FROM Tasks t
        LEFT JOIN Users u1 ON t.CreatedByUserId  = u1.Id
        LEFT JOIN Users u2 ON t.AssignedToUserId = u2.Id";

    public TaskRepository(DbConnectionFactory db, ILogger<TaskRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand($"SELECT {SelectColumns} {FromJoins} WHERE t.Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? Map(r) : null;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving task {TaskId}", id);
            throw;
        }
    }

    public async Task<(IEnumerable<TaskItem> Items, int Total)> GetAllAsync(
        int currentUserId,
        AppTaskStatus?   status   = null,
        AppTaskPriority? priority = null,
        int page         = 1,
        int pageSize     = 20,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (page - 1) * pageSize;

        try
        {
            var conditions = new List<string>
            {
                "(t.CreatedByUserId = @UserId OR t.AssignedToUserId = @UserId)"
            };

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand { Connection = conn };
            cmd.Parameters.AddWithValue("@UserId",   currentUserId);
            cmd.Parameters.AddWithValue("@Offset",   offset);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            if (status.HasValue)
            {
                conditions.Add("t.Status = @Status");
                cmd.Parameters.AddWithValue("@Status", (int)status.Value);
            }
            if (priority.HasValue)
            {
                conditions.Add("t.Priority = @Priority");
                cmd.Parameters.AddWithValue("@Priority", (int)priority.Value);
            }

            var where = "WHERE " + string.Join(" AND ", conditions);
            cmd.CommandText = $@"
                SELECT {SelectColumns}, COUNT(*) OVER() AS TotalCount
                {FromJoins}
                {where}
                ORDER BY t.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var list  = new List<TaskItem>();
            int total = 0;
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                if (total == 0) total = r.GetInt32(r.GetOrdinal("TotalCount"));
                list.Add(Map(r));
            }
            return (list, total);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error listing tasks for user {UserId}", currentUserId);
            throw;
        }
    }

    public async Task<int> CreateAsync(TaskItem task, CancellationToken ct = default)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(@"
                INSERT INTO Tasks
                    (Title, Description, Status, Priority, DueDate, CreatedAt, UpdatedAt, CreatedByUserId, AssignedToUserId)
                VALUES
                    (@Title, @Description, @Status, @Priority, @DueDate, @CreatedAt, @UpdatedAt, @CreatedByUserId, @AssignedToUserId);
                SELECT SCOPE_IDENTITY();", conn);

            AddTaskParams(cmd, task);
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to retrieve the created task ID.");
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error creating task '{Title}'", task.Title);
            throw;
        }
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(@"
                UPDATE Tasks SET
                    Title            = @Title,
                    Description      = @Description,
                    Status           = @Status,
                    Priority         = @Priority,
                    DueDate          = @DueDate,
                    UpdatedAt        = @UpdatedAt,
                    AssignedToUserId = @AssignedToUserId
                WHERE Id = @Id", conn);

            cmd.Parameters.AddWithValue("@Id", task.Id);
            AddTaskParams(cmd, task);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error updating task {TaskId}", task.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand("DELETE FROM Tasks WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error deleting task {TaskId}", id);
            throw;
        }
    }

    private static void AddTaskParams(SqlCommand cmd, TaskItem t)
    {
        cmd.Parameters.AddWithValue("@Title",            t.Title);
        cmd.Parameters.AddWithValue("@Description",      (object?)t.Description      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status",           (int)t.Status);
        cmd.Parameters.AddWithValue("@Priority",         (int)t.Priority);
        cmd.Parameters.AddWithValue("@DueDate",          (object?)t.DueDate          ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt",        t.CreatedAt);
        cmd.Parameters.AddWithValue("@UpdatedAt",        t.UpdatedAt);
        cmd.Parameters.AddWithValue("@CreatedByUserId",  t.CreatedByUserId);
        cmd.Parameters.AddWithValue("@AssignedToUserId", (object?)t.AssignedToUserId ?? DBNull.Value);
    }

    private static TaskItem Map(SqlDataReader r) => new()
    {
        Id                 = r.GetInt32(r.GetOrdinal("Id")),
        Title              = r.GetString(r.GetOrdinal("Title")),
        Description        = r.IsDBNull(r.GetOrdinal("Description"))        ? null : r.GetString(r.GetOrdinal("Description")),
        Status             = (AppTaskStatus)r.GetByte(r.GetOrdinal("Status")),
        Priority           = (AppTaskPriority)r.GetByte(r.GetOrdinal("Priority")),
        DueDate            = r.IsDBNull(r.GetOrdinal("DueDate"))            ? null : r.GetDateTime(r.GetOrdinal("DueDate")),
        CreatedAt          = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt          = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
        CreatedByUserId    = r.GetInt32(r.GetOrdinal("CreatedByUserId")),
        AssignedToUserId   = r.IsDBNull(r.GetOrdinal("AssignedToUserId"))   ? null : r.GetInt32(r.GetOrdinal("AssignedToUserId")),
        CreatedByUsername  = r.IsDBNull(r.GetOrdinal("CreatedByUsername"))  ? null : r.GetString(r.GetOrdinal("CreatedByUsername")),
        AssignedToUsername = r.IsDBNull(r.GetOrdinal("AssignedToUsername")) ? null : r.GetString(r.GetOrdinal("AssignedToUsername"))
    };
}
