using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UsersManager.Models;

namespace UsersManager.Controllers
{
    public class ApplicationController : Controller
    {
        [UserAccess]
        public ActionResult Index()
        {
            return View();
        }
    }
}