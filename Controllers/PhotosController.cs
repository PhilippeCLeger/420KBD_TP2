using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UsersManager.Models;

namespace UsersManager.Controllers
{
    public class PhotosController : Controller
    {
        // GET: Photos
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Photo photo)
        {
            return View();
        }

        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Edit(Photo photo)
        {
            return View();
        }

        public ActionResult Details(int id)
        {
            return View();
        }

        public PartialViewResult GetPhotoDetails(int photoId)
        {
            return PartialView();
        }
    }
}