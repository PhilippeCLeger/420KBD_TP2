using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace UsersManager.Models
{
    public static class UsersDBDAL
    {
        private static DbContextTransaction Transaction
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return (DbContextTransaction)HttpContext.Current.Session["Transaction"];
                }
                return null;
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session["Transaction"] = value;
                }
            }
        }
        private static void BeginTransaction(UsersDBEntities DB)
        {
            if (Transaction != null)
                throw new Exception("Transaction en cours! Impossible d'en démarrer une nouvelle!");
            Transaction = DB.Database.BeginTransaction();
        }
        private static void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction.Dispose();
                Transaction = null;
            }
            else
                throw new Exception("Aucune transaction en cours! Impossible de mettre à jour la base de données!");
        }


        #region Users
        public static bool EmailAvailable(this UsersDBEntities DB, string email, int excludedId = 0)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user == null)
                return true;
            else
                if (user.Id != excludedId)
                return user.Email.ToLower() != email.ToLower();
            return true;
        }

        public static bool EmailExist(this UsersDBEntities DB, string email)
        {
            return DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault() != null;
        }

        public static bool EmailBlocked(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Blocked;
            return true;
        }

        public static bool EmailVerified(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Verified;
            return false;
        }

        public static User GetUser(this UsersDBEntities DB, LoginCredential loginCredential)
        {
            User user = DB.Users.Where(u => (u.Email.ToLower() == loginCredential.Email.ToLower()) &&
                                            (u.Password == loginCredential.Password))
                                .FirstOrDefault();
            return user;
        }

        public static User Add_User(this UsersDBEntities DB, User user)
        {
            user.SaveAvatar();
            user = DB.Users.Add(user);
            DB.SaveChanges();
            OnlineUsers.RenewSerialNumber();
            return user;
        }

        public static User Update_User(this UsersDBEntities DB, User user)
        {
            user.SaveAvatar();
            DB.Entry(user).State = EntityState.Modified;
            DB.SaveChanges();
            DB.Entry(user).Reference(u => u.Gender).Load();
            DB.Entry(user).Reference(u => u.UserType).Load();
            OnlineUsers.RenewSerialNumber();
            return user;
        }

        public static bool RemoveUser(this UsersDBEntities DB, int userId)
        {
            User userToDelete = DB.Users.Find(userId);
            if (userToDelete != null)
            {
                BeginTransaction(DB);
                OnlineUsers.RemoveUser(userToDelete.Id);
                userToDelete.RemoveAvatar();
                DB.Users.Remove(userToDelete);
                DB.SaveChanges();
                DB.DeleteFriendShips(userId);
                Commit();
                return true;
            }
            return false;
        }

        public static User FindUser(this UsersDBEntities DB, int id)
        {
            User user = DB.Users.Find(id);
            if (user != null)
            {
                user.ConfirmEmail = user.Email;
                user.ConfirmPassword = user.Password;
                DB.Entry(user).Reference(u => u.Gender).Load();
                DB.Entry(user).Reference(u => u.UserType).Load();
            }
            return user;
        }

        public static IEnumerable<User> SortedUsers(this UsersDBEntities DB)
        {
            return DB.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        }

        public static bool Verify_User(this UsersDBEntities DB, int userId, int code)
        {
            User user = DB.FindUser(userId);
            if (user != null)
            {
                UnverifiedEmail unverifiedEmail = DB.UnverifiedEmails.Where(u => u.UserId == userId).FirstOrDefault();
                if (unverifiedEmail != null)
                {
                    if (unverifiedEmail.VerificationCode == code)
                    {
                        BeginTransaction(DB);
                        user.Email = user.ConfirmEmail = unverifiedEmail.Email;
                        user.Verified = true;
                        DB.Entry(user).State = EntityState.Modified;
                        DB.UnverifiedEmails.Remove(unverifiedEmail);
                        DB.SaveChanges();
                        Commit();
                        OnlineUsers.RenewSerialNumber();
                        return true;
                    }
                }
            }
            return false;
        }

        public static UnverifiedEmail Add_UnverifiedEmail(this UsersDBEntities DB, int userId, string email)
        {

            UnverifiedEmail unverifiedEmail = new UnverifiedEmail() { UserId = userId, Email = email, VerificationCode = DateTime.Now.Millisecond };
            unverifiedEmail = DB.UnverifiedEmails.Add(unverifiedEmail);
            DB.SaveChanges();
            return unverifiedEmail;
        }

        public static bool HaveUnverifiedEmail(this UsersDBEntities DB, int userId, int code)
        {
            return DB.UnverifiedEmails.Where(u => (u.UserId == userId && u.VerificationCode == code)).FirstOrDefault() != null;
        }

        public static ResetPasswordCommand Add_ResetPasswordCommand(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email == email).FirstOrDefault();
            if (user != null)
            {
                ResetPasswordCommand resetPasswordCommand = new ResetPasswordCommand() { UserId = user.Id, VerificationCode = DateTime.Now.Millisecond };
                resetPasswordCommand = DB.ResetPasswordCommands.Add(resetPasswordCommand);
                DB.SaveChanges();
                return resetPasswordCommand;
            }
            return null;
        }

        public static ResetPasswordCommand Find_ResetPasswordCommand(this UsersDBEntities DB, int userid, int verificationCode)
        {
            return DB.ResetPasswordCommands.Where(r => (r.UserId == userid && r.VerificationCode == verificationCode)).FirstOrDefault();
        }

        public static bool ResetPassword(this UsersDBEntities DB, int userId, string password)
        {
            User user = DB.FindUser(userId);
            if (user != null)
            {
                user.Password = user.ConfirmPassword = password;
                ResetPasswordCommand resetPasswordCommand = DB.ResetPasswordCommands.Where(r => r.UserId == userId).FirstOrDefault();
                if (resetPasswordCommand != null)
                {
                    BeginTransaction(DB);
                    DB.Entry(user).State = EntityState.Modified;
                    DB.ResetPasswordCommands.Remove(resetPasswordCommand);
                    DB.SaveChanges();
                    Commit();
                    return true;
                }
            }
            return false;
        }
        #endregion


        #region LoginJournal
        public static Login AddLogin(this UsersDBEntities DB, int userId)
        {
            Login login = new Login();
            login.LoginDate = login.LogoutDate = DateTime.Now;
            login.UserId = userId;
            login = DB.Logins.Add(login);
            DB.SaveChanges();
            return login;
        }

        public static bool UpdateLogout(this UsersDBEntities DB, int loginId)
        {
            Login login = DB.Logins.Find(loginId);
            if (login != null)
            {
                login.LogoutDate = DateTime.Now;
                DB.Entry(login).State = EntityState.Modified;
                DB.SaveChanges();
                return true;
            }
            return false;
        }

        public static bool UpdateLogoutByUserId(this UsersDBEntities DB, int userId)
        {
            Login login = DB.Logins.Where(l => l.UserId == userId).OrderByDescending(l => l.LoginDate).FirstOrDefault();
            if (login != null)
            {
                login.LogoutDate = DateTime.Now;
                DB.Entry(login).State = EntityState.Modified;
                DB.SaveChanges();
                return true;
            }
            return false;
        }

        public static bool DeleteLoginsJournalDay(this UsersDBEntities DB, DateTime day)
        {
            DateTime dayAfter = day.AddDays(1);
            DB.Logins.RemoveRange(DB.Logins.Where(l => l.LoginDate >= day && l.LoginDate < dayAfter));
            DB.SaveChanges();
            OnlineUsers.RenewSerialNumber();
            return true;
        }
        #endregion


        #region Friendship
        public static FriendShip Add_FiendShipRequest(this UsersDBEntities DB, int userId, int targetUserId)
        {
            User user = DB.Users.Find(userId);
            User targetUser = DB.FindUser(targetUserId);
            if (user != null && targetUser != null)
            {
                BeginTransaction(DB);
                DB.Remove_FiendShipRequest(userId, targetUserId);
                FriendShip friendShip = new FriendShip();
                friendShip.UserId = user.Id;
                friendShip.TargetUserId = targetUser.Id;
                friendShip.CreationDate = DateTime.Now;
                friendShip.Accepted = false;
                friendShip.Declined = false;
                friendShip = DB.FriendShips.Add(friendShip);
                DB.SaveChanges();
                Commit();
                return friendShip;
            }
            return null;
        }
        public static bool Remove_FiendShipRequest(this UsersDBEntities DB, int userId, int targetUserId)
        {
            User user = DB.Users.Find(userId);
            User targetUser = DB.FindUser(targetUserId);
            if (user != null && targetUser != null)
            {
                DB.FriendShips.RemoveRange(DB.FriendShips.Where(f => f.UserId == userId && f.TargetUserId == targetUserId));
                DB.FriendShips.RemoveRange(DB.FriendShips.Where(f => f.UserId == targetUserId && f.TargetUserId == userId));
                DB.SaveChanges();
            }
            return true;
        }
        public static bool Accept_FriendShip(this UsersDBEntities DB, int userId, int targetUserId)
        {
            FriendShip friendShip = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            if (friendShip != null)
            {
                friendShip.Accepted = true;
                DB.Entry(friendShip).State = EntityState.Modified;
                DB.SaveChanges();
                return true;
            }
            return false;
        }
        public static bool Decline_FriendShip(this UsersDBEntities DB, int userId, int targetUserId)
        {
            FriendShip friendShip = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            if (friendShip != null)
            {
                friendShip.Declined = true;
                DB.Entry(friendShip).State = EntityState.Modified;
                DB.SaveChanges();
                return true;
            }
            return false;
        }
        public static bool AreFriends(this UsersDBEntities DB, int userId, int targetUserId)
        {
            User targetUser = DB.Users.Find(targetUserId);
            if (targetUser != null)
            {
                if (targetUser.Blocked)
                    return false;
            }
            else
                return false;
            User user = DB.Users.Find(userId);
            if (user != null)
            {
                if (user.Blocked)
                    return false;
            }
            else
                return false;

            FriendShip friendShip = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            if (friendShip != null)
            {
                return friendShip.Accepted;
            }
            friendShip = DB.FriendShips.Where(f => (f.UserId == targetUserId && f.TargetUserId == userId)).FirstOrDefault();
            if (friendShip != null)
            {
                return friendShip.Accepted;
            }
            return false;
        }
        public static bool FriendShipDeclined(this UsersDBEntities DB, int userId, int targetUserId)
        {
            FriendShip friendShip = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            if (friendShip != null)
            {
                return friendShip.Declined;
            }
            friendShip = DB.FriendShips.Where(f => (f.UserId == targetUserId && f.TargetUserId == userId)).FirstOrDefault();
            if (friendShip != null)
            {
                return friendShip.Declined;
            }
            return false;
        }
        public static bool NotFriends(this UsersDBEntities DB, int userId, int targetUserId)
        {
            FriendShip friendShipOfUser = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            FriendShip friendShipOfTargetUser = DB.FriendShips.Where(f => (f.UserId == targetUserId && f.TargetUserId == userId)).FirstOrDefault();
            return (friendShipOfUser == null && friendShipOfTargetUser == null);
        }

        private static int FriendShipStatus(this UsersDBEntities DB, int userId, int targetUserId)
        {
            FriendShip friendShipOfUser = DB.FriendShips.Where(f => (f.UserId == userId && f.TargetUserId == targetUserId)).FirstOrDefault();
            FriendShip friendShipOfTargetUser = DB.FriendShips.Where(f => (f.UserId == targetUserId && f.TargetUserId == userId)).FirstOrDefault();
            if (friendShipOfUser != null)
            {
                if (friendShipOfUser.Accepted)
                    return 1; // friend
                if (friendShipOfUser.Declined)
                    return 2; // targetUser declined
                return 3; // request friendship pending
            }
            if (friendShipOfTargetUser != null)
            {
                if (friendShipOfTargetUser.Accepted)
                    return 1; // friend
                if (friendShipOfTargetUser.Declined)
                    return 4; // user declined
                return 5; // request friendship offer
            }
            return 0; // not friend
        }
        public static List<FriendShipState> FriendShipsStatus(this UsersDBEntities DB, int userId)
        {
            List<FriendShipState> friendShipsStatus = new List<FriendShipState>();
            foreach (User targetUser in DB.SortedUsers())
            {
                if (targetUser.Id != userId)
                {
                    friendShipsStatus.Add(new FriendShipState(targetUser, DB.FriendShipStatus(userId, targetUser.Id)));
                }
            }
            return friendShipsStatus;
        }

        public static bool DeleteFriendShips(this UsersDBEntities DB, int userId)
        {
            DB.FriendShips.RemoveRange(DB.FriendShips.Where(f => f.UserId == userId));
            DB.FriendShips.RemoveRange(DB.FriendShips.Where(f => f.TargetUserId == userId));
            DB.SaveChanges();
            return true;
        }

        #endregion


        #region Photos
        public static Photo Add_Photo(this UsersDBEntities DB, Photo Photo)
        {
            Photo.CreationDate = DateTime.Now;
            Photo.Save();
            Photo = DB.Photos.Add(Photo);
            DB.SaveChanges();
            return Photo;
        }
        public static List<Photo> VisiblePhotos(this UsersDBEntities DB, int userId)
        {
            List<Photo> photos = new List<Photo>();
            User user = DB.Users.Find(userId);
            if (user != null)
            {
                if (user.Blocked)
                    return photos;
            }
            else
                return photos;
            foreach (Photo photo in DB.Photos)
            {
                if (!photo.User.Blocked)
                {
                    if (photo.UserId == userId) /* private or owner */
                        photos.Add(photo);
                    else
                        switch (photo.VisibilityId)
                        {
                            case 1: /* public */
                                photos.Add(photo);
                                break;
                            case 2: /* friends */
                                if (DB.AreFriends(userId, photo.UserId))
                                    photos.Add(photo);
                                break;
                            default: break;
                        }
                }
            }
            return photos;
        }

        public static List<Photo> SearchPhotosByKeywords(List<Photo> photos, string keywords)
        {
            List<Photo> filteredPhotos = new List<Photo>();
            string[] keywordsArray = keywords.ToLower().Split(' ');
            foreach (var photo in photos)
            {
                string photoText = (photo.Title + " " + photo.Description + photo.User.GetFullName()).ToLower();
                bool containsAllTags = true;
                foreach (var keyword in keywordsArray)
                {
                    if (!photoText.Contains(keyword))
                    {
                        containsAllTags = false;
                        break;
                    }
                }
                if (containsAllTags)
                    filteredPhotos.Add(photo);
            }
            return filteredPhotos;
        }
        public static bool Update_Photo(this UsersDBEntities DB, Photo Photo)
        {
            Photo.Save();
            DB.Entry(Photo).State = EntityState.Modified;
            DB.SaveChanges();
            return true;
        }

        public static bool CompilePhotoRating(this UsersDBEntities DB, Photo photo)
        {
            int ratingsCount = 0;
            double ratingsTotal = 0;
            foreach (PhotoRating photoRating in photo.PhotoRatings)
            {
                if (!photoRating.User.Blocked)
                {
                    ratingsCount++;
                    ratingsTotal += photoRating.Rating;
                }
            }
            if (ratingsCount > 0)
            {
                photo.Ratings = ratingsTotal / ratingsCount;
                photo.RatingsCount = ratingsCount;
            }
            else
            {
                photo.Ratings = 0;
                photo.RatingsCount = 0;
            }
            DB.Entry(photo).State = EntityState.Modified;
            DB.SaveChanges();
            return true;
        }
        public static bool Update_Photo_Ratings(this UsersDBEntities DB)
        {
            BeginTransaction(DB);
            foreach (Photo photo in DB.Photos)
            {
                DB.CompilePhotoRating(photo);
            }
            Commit();
            return true;
        }


        public static bool Remove_Photo(this UsersDBEntities DB, int PhotoId)
        {
            Photo PhotoToDelete = DB.Photos.Find(PhotoId);
            if (PhotoToDelete != null)
            {
                BeginTransaction(DB);
                DB.PhotoRatings.RemoveRange(DB.PhotoRatings.Where(pr => pr.PhotoId == PhotoId));
                PhotoToDelete.Remove();
                DB.Photos.Remove(PhotoToDelete);
                DB.SaveChanges();
                Commit();
                return true;
            }
            return false;
        }
        public static PhotoRating AddPhotoRating(this UsersDBEntities DB, PhotoRating photoRating)
        {
            PhotoRating existingPhotoRating = DB.PhotoRatings.Where(pr => pr.PhotoId == photoRating.PhotoId && pr.UserId == photoRating.UserId).FirstOrDefault();
            if (existingPhotoRating != null)
            {
                existingPhotoRating.Rating = photoRating.Rating;
                existingPhotoRating.Comment = photoRating.Comment;
                existingPhotoRating.CreationDate = DateTime.Now;
                DB.Entry(existingPhotoRating).State = EntityState.Modified;
            }
            else
            {
                photoRating = DB.PhotoRatings.Add(photoRating);
            }
            DB.SaveChanges();
            return photoRating;
        }

        #endregion
    }
}