using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;

namespace mini_store.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationBellViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var unreadCount = await _context.Notifications
                .CountAsync(n => !n.IsRead);

            return View(unreadCount);
        }
    }
}