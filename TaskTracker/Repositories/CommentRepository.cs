using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<CommentRepository> _logger;

    public CommentRepository(DbConnectionFactory db, ILogger<CommentRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Comment>> GetByTaskIdAsync(int taskId)
    {
        try
        {
            var list = new List<Comment>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT c.Id, c.TaskId, c.UserId, c.Content, c.CreatedAt, u.Username
                FROM   Comments c
                JOIN   Users    u ON c.UserId = u.Id
                WHERE  c.TaskId = @TaskId
                ORDER  BY c.CreatedAt ASC", conn);
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(MapFull(r));
            return list;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving comments for task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT Id, TaskId, UserId, Content, CreatedAt FROM Comments WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? MapBase(r) : null;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving comment {CommentId}", id);
            throw;
        }
    }

    public async Task<int> CreateAsync(Comment comment)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
                INSERT INTO Comments (TaskId, UserId, Content, CreatedAt)
                VALUES (@TaskId, @UserId, @Content, @CreatedAt);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@TaskId",    comment.TaskId);
            cmd.Parameters.AddWithValue("@UserId",    comment.UserId);
            cmd.Parameters.AddWithValue("@Content",   comment.Content);
            cmd.Parameters.AddWithValue("@CreatedAt", comment.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to retrieve the created comment ID.");
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error creating comment for task {TaskId}", comment.TaskId);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM Comments WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error deleting comment {CommentId}", id);
            throw;
        }
    }

    private static Comment MapFull(SqlDataReader r) => new()
    {
        Id        = r.GetInt32(r.GetOrdinal("Id")),
        TaskId    = r.GetInt32(r.GetOrdinal("TaskId")),
        UserId    = r.GetInt32(r.GetOrdinal("UserId")),
        Content   = r.GetString(r.GetOrdinal("Content")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        Username  = r.GetString(r.GetOrdinal("Username"))
    };

    private static Comment MapBase(SqlDataReader r) => new()
    {
        Id        = r.GetInt32(r.GetOrdinal("Id")),
        TaskId    = r.GetInt32(r.GetOrdinal("TaskId")),
        UserId    = r.GetInt32(r.GetOrdinal("UserId")),
        Content   = r.GetString(r.GetOrdinal("Content")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
