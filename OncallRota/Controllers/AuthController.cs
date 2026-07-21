using Microsoft.AspNetCore.Mvc;
using OncallRota.Interfaces;
using OncallRota.ViewModels;

namespace OncallRota.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpGet] public IActionResult Login() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var emp = await _auth.ValidateLoginAsync(vm.Email, vm.Password);
            if (emp == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }
            HttpContext.Session.SetInt32("EmployeeId",      emp.EmployeeId);
            HttpContext.Session.SetString("EmployeeName",   emp.EmployeeName);
            HttpContext.Session.SetString("RoleName",       emp.Role?.RoleName ?? "Employee");
            HttpContext.Session.SetInt32("TeamId",          emp.TeamId);
            HttpContext.Session.SetString("TeamName",       emp.Team?.TeamName ?? "");
            HttpContext.Session.SetString("ProfilePicture", emp.ProfilePicture ?? "");

            return emp.Role?.RoleName == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Employee");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
