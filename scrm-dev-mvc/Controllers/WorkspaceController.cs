using Microsoft.AspNetCore.Mvc;

namespace scrm_dev_mvc.Controllers
{
    public class WorkspaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
