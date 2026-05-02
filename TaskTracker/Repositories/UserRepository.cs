using Microsoft.Data.SqlClient;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<UserRepository> _logger;

    private const string UserColumns = "Id, Username, Email, PasswordHash, CreatedAt";

    public UserRepository(DbConnectionFactory db, ILogger<UserRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AppUser?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"SELECT {UserColumns} FROM Users WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving user {UserId}", id);
            throw;
        }
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"SELECT {UserColumns} FROM Users WHERE Username = @Username", conn);
            cmd.Parameters.AddWithValue("@Username", username);
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving user by username '{Username}'", username);
            throw;
        }
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        try
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"SELECT {UserColumns} FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving user by email");
            throw;
        }
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        try
        {
            var list = new List<AppUser>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"SELECT {UserColumns} FROM Users ORDER BY Username", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(Map(r));
            return list;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error listing users");
            throw;
        }
    }

    public async Task<int> CreateAsync(AppUser user)
    {
        try
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

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to retrieve the created user ID.");
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error creating user '{Username}'", user.Username);
            throw;
        }
    }

    private static AppUser Map(SqlDataReader r) => new()
    {
        Id           = r.GetInt32(r.GetOrdinal("Id")),
        Username     = r.GetString(r.GetOrdinal("Username")),
        Email        = r.GetString(r.GetOrdinal("Email")),
        PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
        CreatedAt    = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
