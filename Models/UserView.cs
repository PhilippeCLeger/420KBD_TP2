using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UsersManager.Models
{
    [MetadataType(typeof(UserView))]
    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            this.Logins = new HashSet<Login>();
            CreationDate = DateTime.Now;
            UserTypeId = 3; // User
            Verified = false;
            Blocked = false;
        }

        private static ImageGUIDReference AvatarReference = 
            new ImageGUIDReference(@"/ImagesData/Avatars/", @"no_avatar.png", false);

        public String GetAvatarURL()
        {
            return AvatarReference.GetURL(Avatar, false);
        }
        public void SaveAvatar()
        {
            Avatar = AvatarReference.SaveImage(AvatarImageData, Avatar);
        }
        public void RemoveAvatar()
        {
            AvatarReference.Remove(Avatar);
        }

        public string ConfirmEmail { get; set; }
        public string ConfirmPassword { get; set; }

        [Display(Name = "Avatar")]
        public string AvatarImageData { get; set; }
        public bool IsAdmin
        {
            get { return UserTypeId == 1 /*Admin*/; }
        }
        public string GetFullName(bool showGender = false)
        {
            if (showGender)
            {
                if (Gender.Name != "Neutre")
                    return Gender.Name + " " + LastName;
            }
            return FirstName + " " + LastName;
        }
     }

    public class UserView
    {
        [Display(Name = "Prenom"), Required(ErrorMessage = "Obligatoire")]
        public string FirstName { get; set; }

        [Display(Name = "Nom"), Required(ErrorMessage = "Obligatoire")]
        public string LastName { get; set; }

        [Display(Name = "Avatar")]
        public string Avatar { get; set; }

        [Display(Name = "Genre")]
        public int GenderId { get; set; }

        [Display(Name = "Courriel"), EmailAddress(ErrorMessage = "Invalide"), Required(ErrorMessage = "Obligatoire")]
        [System.Web.Mvc.Remote("EmailAvailable", "Accounts", HttpMethod = "POST", AdditionalFields = "Id", ErrorMessage = "Ce courriel n'est pas disponible.")]
        public string Email { get; set; }

        [Display(Name = "Confirmation")]
        [Compare("Email", ErrorMessage = "Le courriel et celui de confirmation ne correspondent pas.")]
        public string ConfirmEmail { get; set; }

        [Display(Name = "Mot de passe"), Required(ErrorMessage = "Obligatoire")]
        [StringLength(50, ErrorMessage = "Le mot de passe doit comporter au moins {2} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public String Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmation")]
        [Compare("Password", ErrorMessage = "Le mot de passe et celui de confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Genre")]
        public virtual Gender Gender { get; set; }

        [Display(Name = "Date de création")]
        [DataType(DataType.Date)]
        public DateTime CreationDate { get; set; }
    }
}