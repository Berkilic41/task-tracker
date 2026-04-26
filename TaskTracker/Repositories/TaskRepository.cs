using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly DbConnectionFactory _db;

    public TaskRepository(DbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
        t.CreatedAt, t.UpdatedAt, t.CreatedByUserId, t.AssignedToUserId,
        u1.Username AS CreatedByUsername, u2.Username AS AssignedToUsername";

    private const string FromJoins = @"
        FROM Tasks t
        LEFT JOIN Users u1 ON t.CreatedByUserId  = u1.Id
        LEFT JOIN Users u2 ON t.AssignedToUserId = u2.Id";

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand($"SELECT {SelectColumns} {FromJoins} WHERE t.Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync(
        AppTaskStatus? status = null, AppTaskPriority? priority = null)
    {
        var conditions = new List<string>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand { Connection = conn };

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

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        cmd.CommandText = $"SELECT {SelectColumns} {FromJoins} {where} ORDER BY t.CreatedAt DESC";

        var list = new List<TaskItem>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(Map(r));
        return list;
    }

    public async Task<int> CreateAsync(TaskItem task)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Tasks
                (Title, Description, Status, Priority, DueDate, CreatedAt, UpdatedAt, CreatedByUserId, AssignedToUserId)
            VALUES
                (@Title, @Description, @Status, @Priority, @DueDate, @CreatedAt, @UpdatedAt, @CreatedByUserId, @AssignedToUserId);
            SELECT SCOPE_IDENTITY();", conn);

        AddTaskParams(cmd, task);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
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
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Tasks WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
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
        Id                 = r.GetInt32(0),
        Title              = r.GetString(1),
        Description        = r.IsDBNull(2)  ? null : r.GetString(2),
        Status             = (AppTaskStatus)r.GetByte(3),
        Priority           = (AppTaskPriority)r.GetByte(4),
        DueDate            = r.IsDBNull(5)  ? null : r.GetDateTime(5),
        CreatedAt          = r.GetDateTime(6),
        UpdatedAt          = r.GetDateTime(7),
        CreatedByUserId    = r.GetInt32(8),
        AssignedToUserId   = r.IsDBNull(9)  ? null : r.GetInt32(9),
        CreatedByUsername  = r.IsDBNull(10) ? null : r.GetString(10),
        AssignedToUsername = r.IsDBNull(11) ? null : r.GetString(11)
    };
}
