using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Models;
using mini_store.Services;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISalesService _sales;

        public SalesController(ApplicationDbContext db, ISalesService sales)
        {
            _db = db;
            _sales = sales;
        }

        // GET: /Sales
        public async Task<IActionResult> Index()
        {
            ViewBag.Form = await _sales.GetFormDataAsync();

            var list = await _db.Sales.AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(list);
        }

        // GET: /Sales/Details/5  (modal body, loaded by AJAX)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _sales.GetTransactionAsync(id);
            return vm == null ? NotFound() : PartialView("_TransactionDetails", vm);
        }

        // GET: /Sales/Receipt/5?paymentId=3  (modal body, loaded by AJAX)
        [HttpGet]
        public async Task<IActionResult> Receipt(int id, int? paymentId)
        {
            var vm = await _sales.GetTransactionAsync(id);
            if (vm == null) return NotFound();

            ViewBag.PaymentId = paymentId;
            return PartialView("_Receipt", vm);
        }

        // POST: /Sales/Create  (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateSaleRequest request)
        {
            var result = await _sales.CreateSaleAsync(request, User.Identity?.Name);

            return result.Success
                ? Json(new { success = true, saleId = result.SaleId, paymentId = result.PaymentId })
                : BadRequest(new { success = false, error = result.Error });
        }

        // POST: /Sales/Pay  (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int utangId, decimal amount, string? method, string? referenceNo)
        {
            var result = await _sales.PayUtangAsync(utangId, amount, method, referenceNo, User.Identity?.Name);

            return result.Success
                ? Json(new { success = true, saleId = result.SaleId, paymentId = result.PaymentId })
                : BadRequest(new { success = false, error = result.Error });
        }

        // POST: /Sales/Delete/5
       // POST: /Sales/Delete/5
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete(int id, string? returnTo, bool restock)
{
    var result = await _sales.DeleteSaleAsync(id, restock);

    if (result.Success)
        TempData["Success"] = restock
            ? "Transaction deleted and stock restored."
            : "Transaction deleted. Stock was not changed.";
    else
        TempData["Error"] = result.Error;

    return string.Equals(returnTo, "Utang", StringComparison.OrdinalIgnoreCase)
        ? RedirectToAction("Index", "Utang")
        : RedirectToAction(nameof(Index));
}
    }
}