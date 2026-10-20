using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mini_store.Controllers
{
[Authorize(Roles = "Admin,Staff")]
    public class SettingsController : Controller
    {
        public IActionResult Index() => View();
    }
}