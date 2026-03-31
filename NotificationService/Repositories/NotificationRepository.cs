using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Interfaces;
using System.Linq.Expressions;

namespace NotificationService.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly NotificationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(NotificationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class NotificationRepository : GenericRepository<Models.Notification>, INotificationRepository
    {
        public NotificationRepository(NotificationDbContext context) : base(context) { }

        public async Task<IEnumerable<Models.Notification>> GetPendingNotificationsAsync()
        {
            return await _dbSet.Where(n => n.Status == "Pending").ToListAsync();
        }

        public async Task<IEnumerable<Models.Notification>> GetRecentByRecipientAsync(string recipient, int count = 5)
        {
            return await _dbSet
                .Where(n => n.Recipient == recipient)
                .OrderByDescending(n => n.SentAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
