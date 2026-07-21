using Microsoft.AspNetCore.Mvc;

namespace OncallRota.Controllers
{
    public abstract class BaseController : Controller
    {
        protected int?   SessionEmployeeId   => HttpContext.Session.GetInt32("EmployeeId");
        protected int?   SessionTeamId       => HttpContext.Session.GetInt32("TeamId");
        protected string SessionRole         => HttpContext.Session.GetString("RoleName") ?? "";
        protected string SessionEmployeeName => HttpContext.Session.GetString("EmployeeName") ?? "";

        protected bool IsLoggedIn => SessionEmployeeId.HasValue;
        protected bool IsAdmin    => SessionRole == "Admin";

        protected IActionResult? RequireLogin()
            => IsLoggedIn ? null : RedirectToAction("Login", "Auth");

        protected IActionResult? RequireAdmin()
            => IsAdmin ? null : (IsLoggedIn ? Forbid() : RedirectToAction("Login", "Auth"));
    }
}