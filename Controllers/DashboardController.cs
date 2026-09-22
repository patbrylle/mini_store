using Microsoft.AspNetCore.Mvc;
using mini_store.Data;

namespace mini_store.Controllers
{
public class DashboardController : Controller
{
private readonly ApplicationDbContext _context;


    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.TotalProducts = _context.Products.Count();
        ViewBag.TotalCategories = _context.Categories.Count();
        ViewBag.TotalCustomers = _context.Customers.Count();

        return View();
    }
}


}
