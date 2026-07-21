using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;

namespace OncallRota.Controllers
{
    public class RolesController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public RolesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var q = _db.Roles.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(r => r.RoleName.Contains(search));
            ViewBag.Search = search;
            return View(await q.OrderBy(r => r.RoleName).ToListAsync());
        }

        public IActionResult Create() { var g = RequireAdmin(); if (g != null) return g; return View(new Role()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(role);
            if (await _db.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            { ModelState.AddModelError("RoleName","Role name already exists."); return View(role); }
            _db.Roles.Add(role); await _db.SaveChangesAsync();
            TempData["Success"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var role = await _db.Roles.FindAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(role);
            if (await _db.Roles.AnyAsync(r => r.RoleName == role.RoleName && r.RoleId != role.RoleId))
            { ModelState.AddModelError("RoleName","Role name already exists."); return View(role); }
            _db.Roles.Update(role); await _db.SaveChangesAsync();
            TempData["Success"] = "Role updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var role = await _db.Roles.FindAsync(id);
            if (role != null) { _db.Roles.Remove(role); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Role deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
