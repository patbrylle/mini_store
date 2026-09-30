using System.ComponentModel.DataAnnotations;

namespace mini_store.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public string Link { get; set; } = string.Empty;

        public string? UserId { get; set; }
    }
}