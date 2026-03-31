using System.Linq.Expressions;

namespace NotificationService.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task SaveChangesAsync();
    }

    public interface INotificationRepository : IGenericRepository<NotificationService.Models.Notification>
    {
        Task<IEnumerable<NotificationService.Models.Notification>> GetPendingNotificationsAsync();
        Task<IEnumerable<NotificationService.Models.Notification>> GetRecentByRecipientAsync(string recipient, int count = 5);
    }
}
