using System.Linq.Expressions;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Eticaret.Service.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Service.Concrete;

public class Service<T> : IService<T> where T : class, IEntity, new()
{
    internal DatabaseContext _context;
    internal DbSet<T> _dBset;

    public Service(DatabaseContext context)
    {
        _context = context;
        _dBset = _context.Set<T>();
    }

    public void Add(T entity)
    {
        _dBset.Add(entity);
    }

    public async Task AddAsync(T entity)
    {
        await _dBset.AddAsync(entity);
    }


    public void Delete(T entity)
    {
        _dBset.Remove(entity);
    }

    public T Find(int id)
    {
        return _dBset.Find(id);
    }

    public async Task<T> FindAsync()
    {
         return await _dBset.FindAsync();
    }

    public T Get(Expression<Func<T, bool>> expression)
    {
        return _dBset.FirstOrDefault(expression);
    }

    public List<T> GetAll()
    {
        return _dBset.AsNoTracking().ToList();
    }

    public List<T> GetAll(Expression<Func<T, bool>> expression)
    {
         return _dBset.Where(expression).AsNoTracking().ToList();
    }

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> expression)
    {
         return await _dBset.Where(expression).AsNoTracking().ToListAsync();
    }

    public async Task<List<T>> GetAllAsync()
    {
         return await _dBset.AsNoTracking().ToListAsync();
    }

    public async Task<T> GetAsync(Expression<Func<T, bool>> expression)
    {
        return await _dBset.FirstOrDefaultAsync(expression);
    }

    public IQueryable<T> GetQueryable()
    {
        return _dBset;
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Update(T entity)
    {
        _dBset.Update(entity);
    }
}
