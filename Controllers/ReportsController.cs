using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mini_store.Data;
using mini_store.Services;

namespace mini_store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private const string StoreName = "Mini Store";

        private readonly IReportService _reports;
        private readonly ApplicationDbContext _db;

        public ReportsController(IReportService reports, ApplicationDbContext db)
        {
            _reports = reports;
            _db = db;
        }

        // GET: Reports?type=sales&range=month&status=Paid ...
        [HttpGet]
        public async Task<IActionResult> Index(ReportFilter f)
        {
            var info = ReportCatalog.Normalize(f);
            var vm = new ReportsPageVm { Filter = f, Info = info };

            if (info.Key == "overview") vm.Overview = await _reports.OverviewAsync();
            else vm.Table = await _reports.BuildAsync(f);

            if (info.HasCategory)
            {
                vm.Categories = (await _db.Categories.AsNoTracking()
                        .OrderBy(c => c.CategoryName)
                        .Select(c => new { c.CategoryId, c.CategoryName })
                        .ToListAsync())
                    .Select(c => (Id: c.CategoryId, Name: c.CategoryName))
                    .ToList();
            }

            if (info.HasCustomer)
            {
                vm.Customers = (await _db.Customers.AsNoTracking()
                        .OrderBy(c => c.CustomerName)
                        .Select(c => new { c.CustomerId, c.CustomerName })
                        .ToListAsync())
                    .Select(c => (Id: c.CustomerId, Name: c.CustomerName))
                    .ToList();
            }

            return View(vm);
        }

        // GET: Reports/Export?type=sales&range=month&format=pdf|excel
        [HttpGet]
        public async Task<IActionResult> Export(ReportFilter f, string format = "excel")
        {
            var info = ReportCatalog.Normalize(f);
            if (info.Key == "overview") return RedirectToAction(nameof(Index));

            var table = await _reports.BuildAsync(f);
            var by = User.Identity?.Name ?? "";
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");

            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(ReportExporter.Pdf(table, StoreName, by),
                    "application/pdf", $"{info.Key}-report-{stamp}.pdf");
            }

            return File(ReportExporter.Excel(table, StoreName, by),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{info.Key}-report-{stamp}.xlsx");
        }
    }
}