using OPA_Pay.Models;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync();
    Task<List<Notification>> GetByUserIdAsync(string userId);
    Task<Notification?> GetByIdAsync(int id);
    Task AddAsync(Notification n);
    Task SaveAsync();
}