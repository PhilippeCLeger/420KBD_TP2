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
        UsersDBEntities DB = new UsersDBEntities();

        // GET: Photos
        public ActionResult Index()
        {
            SetLocalPhotosSerialNumber();
            InitSortPhotos();
            return View();
        }
        [UserAccess]
        public ActionResult Create()
        {
            ViewBag.PhotoVisibilities = GetPhotoVisibilities();
            Photo photo = new Photo();
            return View(photo);
        }

        [HttpPost]
        public ActionResult Create(Photo photo)
        {
            //photo.Id = -1;
            //photo.UserId = 1;
            //photo.Ratings = 0;
            //photo.CreationDate
            //photo.VisibilityId = (int)photo.VisibilityId;
            if (ModelState.IsValid)
            {
                photo = DB.Add_Photo(photo);
                RenewPhotosSerialNumber();
                return RedirectToAction($"Details/{photo.Id}");
            }
            else
            {
                var ms = ModelState;
            }
            ViewBag.PhotoVisibilities = GetPhotoVisibilities();
            return View(photo);
        }

        public ActionResult Edit(int id)
        {
            ViewBag.PhotoVisibilities = GetPhotoVisibilities();
            return GetPhotoView<ActionResult>(id, (photo) => View(photo), () => RedirectToAction("Index"));
            //Photo photo = DB.Photos.Find(id);
            //if(photo != null)
            //{
            //    return View(photo);
            //}
            //return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Edit(Photo photo)
        {
            if (ModelState.IsValid)
            {
                DB.Update_Photo(photo);
                RenewPhotosSerialNumber();
                return RedirectToAction($"Details/{photo.Id}");
            }
            ViewBag.PhotoVisibilities = GetPhotoVisibilities();
            return View(photo);
        }

        public ActionResult Details(int id)
        {
            return GetPhotoView<ActionResult>(id, (photo) => View(photo), () => RedirectToAction("Index"));
            //Photo photo = DB.Photos.Find(id);
            //if(photo != null)
            //{
            //    return View(photo);
            //}
            //return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            // Delete la photo et ses ratings...
            DB.Remove_Photo(id);
            RenewPhotosSerialNumber();
            return RedirectToAction("Index");
        }


        public ActionResult UpdateCurrentUserRating(int photoId, int rating, string comment)
        {
            var user = OnlineUsers.GetSessionUser();
            var photoRating = DB.PhotoRatings.Where((r) => r.UserId == user.Id && r.PhotoId == photoId).FirstOrDefault();
            if (photoRating == null)
            {
                photoRating = new PhotoRating();
                photoRating.UserId = user.Id;
                photoRating.PhotoId = photoId;
            }
            photoRating.Comment = comment;
            photoRating.Rating = rating;
            photoRating.CreationDate = DateTime.Now;
            DB.AddPhotoRating(photoRating);
            return null;
        }

        public void InitSortRatings()
        {
            if(Session["RatingFieldToSort"] == null)
                Session["RatingFieldToSort"] = "dates";
            if (Session["RatingFieldSortDir"] == null)
                Session["RatingFieldSortDir"] = true;
        }
        public void InitSortPhotos()
        {
            if (Session["PhotoFieldToSort"] == null)
                Session["PhotoFieldToSort"] = "dates";
            if (Session["PhotoFieldSortDir"] == null)
                Session["PhotoFieldSortDir"] = true;
        }

        public PartialViewResult GetPhotoDetails(int photoId)
        {
            InitSortRatings();
            return GetPhotoView<PartialViewResult>(photoId, (photo) => PartialView(photo), () => null);
            //Photo photo = DB.Photos.Find(photoId);
            //if(photo != null)
            //{
            //    return PartialView(photo);
            //}
            //return null;
        }

        private SelectList GetPhotoVisibilities() =>
            new SelectList(BuildPhotoVisibilitiesItems(), "Id", "Name");

        private IEnumerable<SelectListItem> BuildPhotoVisibilitiesItems()
        {
            var lst = new List<SelectListItem>();
            foreach (var v in DB.PhotoVisibilities)
                lst.Add(BuildItem(v.Name, v.Id.ToString()));
            return lst;
        }

        private SelectListItem BuildItem(string text, string value)
        {
            var item = new SelectListItem();
            item.Value = value;
            item.Text = text;
            return item;
        }

        private T GetPhotoView<T>(int photoId, Func<Photo, T> getView, Func<T> getAlternateView)
        {
            Photo photo = DB.Photos.Find(photoId);
            if (photo != null)
            {
                photo.PhotoRatings = DB.PhotoRatings.Where((r) => r.PhotoId == photo.Id).ToArray();
                return getView(photo);
            }
            return getAlternateView();
        }

        public ActionResult SortRatingsBy(string fieldToSort)
        {
            return View();
        }

        public ActionResult SortPhotosBy(string fieldToSort)
        {
            Session["PhotoFieldSortDir"] = (string)Session["PhotoFieldToSort"] == fieldToSort;
            Session["PhotoFieldToSort"] = fieldToSort;

            return View(DB.Photos);
        }

        public PartialViewResult PhotoForm(Photo photo)
        {
            
            return PartialView(photo);
        }

        // -------------------------------------------------------- serial number --------------------------------------------------

        // je ne suis pas sur si nous avons déja cette méthode
        //public PartialViewResult GetImages(bool forceRefresh = false)
        //{
        //    if (forceRefresh || !IsImagesUpToDate())
        //    {
        //        SetLocalImagesSerialNumber();
        //        return PartialView(DB.Photos.OrderByDescending(i => i.CreationDate));
        //    }
        //    return null;
        //}


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DB.Dispose();
            }
            base.Dispose(disposing);
        }

        public void RenewPhotosSerialNumber()
        {
            HttpRuntime.Cache["imagesSerialNumber"] = Guid.NewGuid().ToString();
        }

        public string GetPhotosSerialNumber()
        {
            if (HttpRuntime.Cache["imagesSerialNumber"] == null)
            {
                RenewPhotosSerialNumber();
            }
            return (string)HttpRuntime.Cache["imagesSerialNumber"];
        }

        public void SetLocalPhotosSerialNumber()
        {
            Session["imagesSerialNumber"] = GetPhotosSerialNumber();
        }

        public bool IsPhotoUpToDate()
        {
            return ((string)Session["imagesSerialNumber"] == (string)HttpRuntime.Cache["imagesSerialNumber"]);
        }
    }
}