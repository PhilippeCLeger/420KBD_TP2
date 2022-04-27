using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UsersManager.Models
{
    [MetadataType(typeof(PhotoView))]
    public partial class Photo
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Photo()
        {
            //this.PhotoRatings = new HashSet<PhotoRating>();
            Ratings = 0;
            RatingsCount = 0;
            CreationDate = DateTime.Now;
            UserId = OnlineUsers.CurrentUserId;
            VisibilityId = 1;
        }

        public string Data { get; set; }
        private static ImageGUIDReference PhotoReference = new ImageGUIDReference(@"/ImagesData/Photos/", @"No_Photo.png", true);

        public string GetUrl(bool thumbnail = false)
        {
            return PhotoReference.GetURL(GUID, thumbnail);
        }

        public void Save()
        {
            GUID = PhotoReference.SaveImage(Data, GUID);
        }

        public void Remove()
        {
            PhotoReference.Remove(GUID);
        }
    }


    public class PhotoView
    {
        [Display(Name = "Titre"), Required(ErrorMessage = "Obligatoire"), MaxLength(49, ErrorMessage = "Ne doit pas excéder 50 caractères")]
        public string Title { get; set; }

        [Display(Name = "Description"), Required(ErrorMessage = "Obligatoire")]
        public string Description { get; set; }

        [Display(Name = "Photo")]
        public string GUID { get; set; }

        [Display(Name = "Évaluations"), Range(0, 5, ErrorMessage = "invalide")]
        public double Ratings { get; set; }
    }
}