using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly DbConnectionFactory _db;

    public CommentRepository(DbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Comment>> GetByTaskIdAsync(int taskId)
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
            list.Add(new Comment
            {
                Id        = r.GetInt32(0),
                TaskId    = r.GetInt32(1),
                UserId    = r.GetInt32(2),
                Content   = r.GetString(3),
                CreatedAt = r.GetDateTime(4),
                Username  = r.GetString(5)
            });
        return list;
    }

    public async Task<int> CreateAsync(Comment comment)
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
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Comments WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
