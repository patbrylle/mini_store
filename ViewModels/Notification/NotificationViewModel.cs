namespace mini_store.ViewModels.Notification
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string Link { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}
