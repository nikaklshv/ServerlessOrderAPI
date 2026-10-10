using Microsoft.AspNetCore.Mvc;

namespace ServerlessOrderApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Create");
        }

        // GET: /Home/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: /Home/Search
        public IActionResult Search()
        {
            return View();
        }
    }
}