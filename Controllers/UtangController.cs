using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Services;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class UtangController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISalesService _sales;

        public UtangController(ApplicationDbContext db, ISalesService sales)
        {
            _db = db;
            _sales = sales;
        }

        // GET: /Utang  (read-only: no writes on a GET)
        public async Task<IActionResult> Index()
        {
            ViewBag.Form = await _sales.GetFormDataAsync();

            var utangs = await _db.Utangs.AsNoTracking()
                .Include(u => u.Customer)
                .Include(u => u.Sale)
                .OrderByDescending(u => u.Sale != null ? u.Sale.SaleDate : u.DateCreated)
                .ToListAsync();

            return View(utangs);
        }
    }
}