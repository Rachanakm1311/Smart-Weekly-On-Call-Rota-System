using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;
using OncallRota.Services;

namespace OncallRota.Controllers
{
    public class EmployeesController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment  _env;

        public EmployeesController(ApplicationDbContext db, IWebHostEnvironment env)
        { _db = db; _env = env; }

        // ── helpers ──────────────────────────────────────────────────────────
        private async Task PopulateDropdowns(Employee? emp = null)
        {
            ViewBag.Teams = new SelectList(await _db.Teams.Where(t => t.Status == "Active").OrderBy(t => t.TeamName).ToListAsync(), "TeamId", "TeamName", emp?.TeamId);
            ViewBag.Roles = new SelectList(await _db.Roles.Where(r => r.Status == "Active").OrderBy(r => r.RoleName).ToListAsync(), "RoleId", "RoleName", emp?.RoleId);
        }

        /// <summary>Saves uploaded photo to wwwroot/images/profiles and returns the root-relative path.</summary>
        private async Task<string?> SavePhotoAsync(IFormFile? photo, string? existingPath)
        {
            if (photo == null || photo.Length == 0) return existingPath;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return existingPath;   // invalid type — keep old

            var profilesDir = Path.Combine(_env.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(profilesDir);

            // Delete old file (not the default placeholder)
            if (!string.IsNullOrEmpty(existingPath) && !existingPath.Contains("default"))
            {
                var old = Path.Combine(_env.WebRootPath, existingPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(profilesDir, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return $"/images/profiles/{fileName}";
        }

        // ── CRUD ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var q = _db.Employees.Include(e => e.Team).Include(e => e.Role).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.EmployeeName.Contains(search) || (e.Email != null && e.Email.Contains(search)));
            ViewBag.Search = search;
            return View(await q.OrderBy(e => e.EmployeeName).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var g = RequireAdmin(); if (g != null) return g;
            await PopulateDropdowns(); return View(new Employee());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee emp, IFormFile? photo)
        {
            var g = RequireAdmin(); if (g != null) return g;
            // Derive EmployeeName from the two explicit fields
            emp.EmployeeName = $"{emp.FirstName.Trim()} {emp.LastName.Trim()}";
            ModelState.Remove(nameof(Employee.EmployeeName));
            if (!ModelState.IsValid) { await PopulateDropdowns(emp); return View(emp); }
            if (await _db.Employees.AnyAsync(e => e.Email == emp.Email))
            { ModelState.AddModelError("Email", "Email already exists."); await PopulateDropdowns(emp); return View(emp); }
            emp.Password       = AuthService.HashPassword(emp.Password);
            emp.ProfilePicture = await SavePhotoAsync(photo, null);
            _db.Employees.Add(emp); await _db.SaveChangesAsync();
            TempData["Success"] = "Employee created."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            await PopulateDropdowns(emp); return View(emp);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee emp, IFormFile? photo, bool removePhoto = false)
        {
            var g = RequireAdmin(); if (g != null) return g;
            // Derive EmployeeName from the two explicit fields
            emp.EmployeeName = $"{emp.FirstName.Trim()} {emp.LastName.Trim()}";
            // Password is optional on edit (blank = keep existing); clear any binding error for it
            ModelState.Remove(nameof(Employee.EmployeeName));
            ModelState.Remove(nameof(Employee.Password));
            if (!ModelState.IsValid) { await PopulateDropdowns(emp); return View(emp); }
            if (await _db.Employees.AnyAsync(e => e.Email == emp.Email && e.EmployeeId != emp.EmployeeId))
            { ModelState.AddModelError("Email", "Email already exists."); await PopulateDropdowns(emp); return View(emp); }

            var existing = await _db.Employees.AsNoTracking().FirstAsync(e => e.EmployeeId == emp.EmployeeId);

            // Keep existing password when the field is left blank
            if (string.IsNullOrWhiteSpace(emp.Password))
                emp.Password = existing.Password;
            // Re-hash only when password actually changed and is not already BCrypt
            else if (emp.Password != existing.Password && !emp.Password.StartsWith("$2"))
                emp.Password = AuthService.HashPassword(emp.Password);

            if (removePhoto)
            {
                // Delete the existing file if it's a custom upload
                if (!string.IsNullOrEmpty(existing.ProfilePicture) && !existing.ProfilePicture.Contains("default"))
                {
                    var file = Path.Combine(_env.WebRootPath,
                        existing.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
                }
                emp.ProfilePicture = null;
            }
            else
            {
                emp.ProfilePicture = await SavePhotoAsync(photo, existing.ProfilePicture);
            }

            _db.Employees.Update(emp); await _db.SaveChangesAsync();
            TempData["Success"] = "Employee updated."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var emp = await _db.Employees.FindAsync(id);
            if (emp != null)
            {
                // Clean up uploaded file
                if (!string.IsNullOrEmpty(emp.ProfilePicture) && !emp.ProfilePicture.Contains("default"))
                {
                    var file = Path.Combine(_env.WebRootPath,
                        emp.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
                }
                _db.Employees.Remove(emp); await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Employee deleted."; return RedirectToAction(nameof(Index));
        }
    }
}
