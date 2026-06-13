using Microsoft.EntityFrameworkCore;

namespace FarmacorpPOS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FarmacorpDbContext _context;
    protected readonly DbSet<T> _set;

    public Repository(FarmacorpDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    public void Update(T entity) => _set.Update(entity);

    public void Delete(T entity) => _set.Remove(entity);
}
