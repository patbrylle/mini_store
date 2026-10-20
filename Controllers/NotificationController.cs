using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.ViewModels.Notification;

namespace mini_store.Controllers
{
[Authorize(Roles = "Admin,Staff")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
var model = notifications
    .Select(n => new NotificationViewModel
    {
        NotificationId = n.NotificationId,

        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        CreatedAt = n.CreatedAt,
        Link = n.Link,
        IsRead = n.IsRead,

        Icon = n.Type switch
        {
            "warning" => "bi-exclamation-triangle-fill",
            "danger" => "bi-x-circle-fill",
            "success" => "bi-check-circle-fill",
            "info" => "bi-info-circle-fill",
            _ => "bi-bell-fill"
        }
    })
    .ToList();

            return View(model);
        }
        [HttpGet]
public async Task<IActionResult> Open(int id)
{
    var notification = await _context.Notifications
        .FirstOrDefaultAsync(n => n.NotificationId == id);

    if (notification == null)
    {
        return NotFound();
    }

    notification.IsRead = true;

    await _context.SaveChangesAsync();

    if (!string.IsNullOrEmpty(notification.Link))
    {
        return Redirect(notification.Link);
    }

    return RedirectToAction(nameof(Index));
}

        // ==========================================
        // MARK NOTIFICATION AS READ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        n => n.NotificationId == id
                    );

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }

        // ==========================================
        // MARK ALL AS READ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var notifications =
                await _context.Notifications
                    .Where(n => !n.IsRead)
                    .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }
    }
}