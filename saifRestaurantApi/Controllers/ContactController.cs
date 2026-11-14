using Microsoft.AspNetCore.Mvc;

namespace saifRestaurantApi.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
