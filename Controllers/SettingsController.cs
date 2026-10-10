using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mini_store.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        public IActionResult Index() => View();
    }
}