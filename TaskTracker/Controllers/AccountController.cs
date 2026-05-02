using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using TaskTracker.Models;
using TaskTracker.Models.Helpers;
using TaskTracker.Models.ViewModels;
using TaskTracker.Repositories.Interfaces;

namespace TaskTracker.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private const int  MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly IMemoryCache    _cache;

    public AccountController(IUserRepository users, IMemoryCache cache)
    {
        _users = users;
        _cache = cache;
    }

    private string LockoutKey(string username) => $"login:lockout:{username.ToLowerInvariant()}";
    private string AttemptsKey(string username) => $"login:attempts:{username.ToLowerInvariant()}";

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Tasks");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        // Check lockout
        if (_cache.TryGetValue(LockoutKey(model.Username), out _))
        {
            ModelState.AddModelError(string.Empty,
                "Too many failed attempts. Account is locked for 15 minutes.");
            return View(model);
        }

        var user = await _users.GetByUsernameAsync(model.Username);
        if (user is null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
        {
            var attempts = _cache.GetOrCreate(AttemptsKey(model.Username), e =>
            {
                e.AbsoluteExpirationRelativeToNow = LockoutDuration;
                return 0;
            }) + 1;

            _cache.Set(AttemptsKey(model.Username), attempts,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockoutDuration });

            if (attempts >= MaxFailedAttempts)
            {
                _cache.Set(LockoutKey(model.Username), true,
                    new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockoutDuration });
                ModelState.AddModelError(string.Empty,
                    "Too many failed attempts. Account is locked for 15 minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
            }
            return View(model);
        }

        // Successful login — clear lockout counters
        _cache.Remove(AttemptsKey(model.Username));
        _cache.Remove(LockoutKey(model.Username));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.Username),
            new(ClaimTypes.Email,          user.Email)
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties { IsPersistent = model.RememberMe };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Tasks");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Tasks");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _users.GetByUsernameAsync(model.Username) is not null)
        {
            ModelState.AddModelError(nameof(model.Username), "Username is already taken.");
            return View(model);
        }
        if (await _users.GetByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        await _users.CreateAsync(new AppUser
        {
            Username     = model.Username,
            Email        = model.Email,
            PasswordHash = PasswordHelper.HashPassword(model.Password),
            CreatedAt    = DateTime.UtcNow
        });

        TempData["Success"] = "Account created! You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
