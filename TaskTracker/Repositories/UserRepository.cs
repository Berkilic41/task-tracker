using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _db;

    public UserRepository(DbConnectionFactory db) => _db = db;

    public async Task<AppUser?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users WHERE Username = @Username", conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users WHERE Email = @Email", conn);
        cmd.Parameters.AddWithValue("@Email", email);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        var list = new List<AppUser>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users ORDER BY Username", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(Map(r));
        return list;
    }

    public async Task<int> CreateAsync(AppUser user)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Users (Username, Email, PasswordHash, CreatedAt)
            VALUES (@Username, @Email, @PasswordHash, @CreatedAt);
            SELECT SCOPE_IDENTITY();", conn);
        cmd.Parameters.AddWithValue("@Username",     user.Username);
        cmd.Parameters.AddWithValue("@Email",        user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@CreatedAt",    user.CreatedAt);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static AppUser Map(SqlDataReader r) => new()
    {
        Id           = r.GetInt32(0),
        Username     = r.GetString(1),
        Email        = r.GetString(2),
        PasswordHash = r.GetString(3),
        CreatedAt    = r.GetDateTime(4)
    };
}
