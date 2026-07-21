using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;
using OncallRota.Models;

namespace OncallRota.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        public AuthService(ApplicationDbContext db) => _db = db;

        public async Task<Employee?> ValidateLoginAsync(string email, string password)
        {
            var emp = await _db.Employees
                               .Include(e => e.Role)
                               .Include(e => e.Team)
                               .FirstOrDefaultAsync(e =>
                                   e.Email == email &&
                                   e.Status == "Active");
            if (emp == null) return null;

            // Support both BCrypt hashes (starts with $2) and plain-text (legacy/seed)
            bool valid = emp.Password.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(password, emp.Password)
                : emp.Password == password;

            return valid ? emp : null;
        }

        /// <summary>Hash a plain-text password using BCrypt (work factor 12).</summary>
        public static string HashPassword(string plain) =>
            BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 12);
    }
}
