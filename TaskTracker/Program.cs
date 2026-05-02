using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;
using TaskTracker.Data;
using TaskTracker.Repositories;
using TaskTracker.Repositories.Interfaces;
using TaskTracker.Services;
using TaskTracker.Services.Interfaces;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10; opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; opt.QueueLimit = 0;
    });
    o.RejectionStatusCode = 429;
});

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

app.UseMiddleware<TaskTracker.Middleware.CorrelationIdMiddleware>();
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"]        = "DENY";
    context.Response.Headers["X-XSS-Protection"]       = "1; mode=block";
    context.Response.Headers["Referrer-Policy"]        = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:";
    await next();
});

app.UseRateLimiter();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/health", async (DbConnectionFactory db) =>
{
    try { using var c = db.CreateConnection(); await c.OpenAsync(); return Results.Ok(new { status = "healthy", database = "ok", timestamp = DateTime.UtcNow }); }
    catch { return Results.Json(new { status = "degraded", database = "error", timestamp = DateTime.UtcNow }, statusCode: 503); }
}).AllowAnonymous();

    Log.Information("TaskTracker starting up");
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "TaskTracker terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }

public partial class Program { }
