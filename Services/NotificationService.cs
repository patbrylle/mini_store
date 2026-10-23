using mini_store.Data;
using mini_store.Models;

namespace mini_store.Services
{
    public interface INotificationService
    {
        // adds to the context only; the caller saves (so it joins the caller's transaction)
        void Add(string title, string message, string type, string link);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        public NotificationService(ApplicationDbContext db) => _db = db;

        public void Add(string title, string message, string type, string link) =>
            _db.Notifications.Add(new Notification
            {
                Title = title, Message = message, Type = type,
                CreatedAt = DateTime.Now, IsRead = false, Link = link
            });
    }
}