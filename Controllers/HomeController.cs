using System.Web.Mvc;
using UsersManager.Models;

namespace UsersManager.Controllers
{
    public class HomeController : Controller
    {
        //UsersDBEntities DB = new UsersDBEntities();

        public ActionResult Index()
        {
            //User user = DB.Users.Find(1);
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}