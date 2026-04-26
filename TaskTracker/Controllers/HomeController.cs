using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Tasks");
        return View();
    }
}
