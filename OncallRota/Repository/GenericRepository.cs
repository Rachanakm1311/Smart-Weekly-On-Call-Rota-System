using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;

namespace OncallRota.Repository
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _db;
        protected readonly DbSet<T> _set;
        public GenericRepository(ApplicationDbContext db) { _db = db; _set = db.Set<T>(); }

        public async Task<IEnumerable<T>> GetAllAsync()        => await _set.ToListAsync();
        public async Task<T?> GetByIdAsync(int id)             => await _set.FindAsync(id);
        public async Task<T> CreateAsync(T entity)             { _set.Add(entity); await _db.SaveChangesAsync(); return entity; }
        public async Task<T> UpdateAsync(T entity)             { _set.Update(entity); await _db.SaveChangesAsync(); return entity; }
        public async Task DeleteAsync(int id)
        {
            var e = await _set.FindAsync(id);
            if (e != null) { _set.Remove(e); await _db.SaveChangesAsync(); }
        }
    }
}
