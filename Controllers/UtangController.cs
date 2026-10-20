
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using Microsoft.AspNetCore.Authorization;


namespace mini_store.Controllers
{
[Authorize(Roles = "Admin,Staff")]
    public class UtangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UtangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Utang
        // =========================================================

        public async Task<IActionResult> Index()
        {
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            ViewBag.Products = await _context.Products
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var utangs = await _context.Utangs
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                    .ThenInclude(s => s!.SaleItems)
                        .ThenInclude(si => si.Product)
                .OrderByDescending(u =>
                    u.Sale != null
                        ? u.Sale.SaleDate
                        : u.DateCreated)
                .ToListAsync();
                await CreateOverdueNotifications(utangs);

            return View(utangs);
        }
        private async Task CreateOverdueNotifications(
    List<mini_store.Models.Utang> utangs)
{
    var today = DateTime.Today;

    foreach (var utang in utangs)
    {
        if (!utang.DueDate.HasValue)
            continue;

        if (utang.Balance <= 0)
            continue;

        if (utang.DueDate.Value.Date >= today)
            continue;

        var customerName =
            utang.Customer?.CustomerName ?? "Customer";

        var link =
            Url.Action("Index", "Utang") ?? "#";

        var alreadyExists = await _context.Notifications
            .AnyAsync(n =>
                n.Title == "Overdue Utang" &&
                n.Link == link &&
                n.Message.Contains($"#{utang.UtangId}"));

        if (alreadyExists)
            continue;

        _context.Notifications.Add(
            new mini_store.Models.Notification
            {
                Title = "Overdue Utang",
                Message =
                    $"{customerName}'s utang #{utang.UtangId} " +
                    $"is overdue. Remaining balance: " +
                    $"₱{utang.Balance:N2}.",

                Type = "danger",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Link = link
            });
    }

    await _context.SaveChangesAsync();
}


        // =========================================================
        // POST: /Utang/Pay
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
            int id,
            decimal paymentAmount)
        {
            // Find the utang record
           var utang = await _context.Utangs
    .Include(u => u.Customer)
    .Include(u => u.Sale)
    .FirstOrDefaultAsync(u => u.UtangId == id);

            if (utang == null)
            {
                return NotFound();
            }


            // =====================================================
            // VALIDATE PAYMENT
            // =====================================================

            if (paymentAmount <= 0)
            {
                TempData["UtangError"] =
                    "Please enter a valid payment amount.";

                return RedirectToAction(nameof(Index));
            }


            // =====================================================
            // PREVENT OVERPAYMENT
            // =====================================================

            if (paymentAmount > utang.Balance)
            {
                TempData["UtangError"] =
                    "Payment cannot be greater than the remaining balance.";

                return RedirectToAction(nameof(Index));
            }


            // =====================================================
            // UPDATE PAYMENT
            // =====================================================

            utang.AmountPaid += paymentAmount;


            // Recalculate remaining balance
            utang.Balance =
                Math.Max(
                    0,
                    utang.Amount - utang.AmountPaid
                );


            // =====================================================
            // UPDATE UTANG STATUS
            // =====================================================

            if (utang.Balance <= 0)
            {
                utang.Balance = 0;
                utang.Status = "Paid";
            }
            else if (utang.AmountPaid > 0)
            {
                utang.Status = "Partial";
            }
            else
            {
                utang.Status = "Unpaid";
            }


            // =====================================================
            // UPDATE RELATED SALE
            // =====================================================

            if (utang.Sale != null)
            {
                utang.Sale.AmountPaid =
                    utang.AmountPaid;

                if (utang.Balance <= 0)
                {
                    utang.Sale.Status = "Paid";
                }
                else if (utang.AmountPaid > 0)
                {
                    utang.Sale.Status = "Partial";
                }
                else
                {
                    utang.Sale.Status = "Unpaid";
                }
            }


            // =====================================================
            // SAVE
            // =====================================================

          await _context.SaveChangesAsync();

var customerName =
    utang.Customer?.CustomerName ?? "Customer";

var paymentNotification = new mini_store.Models.Notification
{
    Title = "Utang Payment Received",
    Message =
        $"{customerName} paid ₱{paymentAmount:N2}. " +
        $"Remaining balance: ₱{utang.Balance:N2}.",
    Type = utang.Balance <= 0
        ? "success"
        : "info",
    CreatedAt = DateTime.Now,
    IsRead = false,
    Link = Url.Action(
        "Index",
        "Utang"
    ) ?? "#"
};

_context.Notifications.Add(paymentNotification);

await _context.SaveChangesAsync();

TempData["UtangSuccess"] =
    "Payment recorded successfully.";


            return RedirectToAction(nameof(Index));
        }
        // =========================================================
// POST: /Utang/Delete/5
// =========================================================
[Authorize(Roles = "Admin")]

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var utang = await _context.Utangs
        .Include(u => u.Sale)
            .ThenInclude(s => s!.SaleItems)
        .FirstOrDefaultAsync(u => u.UtangId == id);

    if (utang == null)
    {
        return NotFound();
    }

    // Only allow deletion when the utang is fully paid
    if (utang.Balance > 0 || utang.Status != "Paid")
    {
        TempData["ErrorMessage"] =
            "This utang cannot be deleted because it is not fully paid.";

        return RedirectToAction(nameof(Index));
    }

    using var transaction =
        await _context.Database.BeginTransactionAsync();

    try
    {
        // Remove SaleItems first
        if (utang.Sale?.SaleItems != null)
        {
            _context.SaleItems.RemoveRange(
                utang.Sale.SaleItems
            );
        }

        // Remove the Sale
        if (utang.Sale != null)
        {
            _context.Sales.Remove(utang.Sale);
        }

        // Remove the Utang record
        _context.Utangs.Remove(utang);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
            "Paid utang record deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();

        TempData["ErrorMessage"] =
            "Unable to delete the utang record: " + ex.Message;

        return RedirectToAction(nameof(Index));
    }
}
    }
}
