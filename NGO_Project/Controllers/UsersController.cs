using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using NGO_Project;
using NGO_Project.Libs;

namespace NGO_Project.Controllers
{
    public class UsersController: Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: Users
        public ActionResult Index()
        {
            return View(db.Users.ToList());
        }

        // GET: Users/Login
        public ActionResult Login()
        {
            ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type");
            return View();
        }

        // POST: Users/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user, string Type)
        {
            ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type");

            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("Missinguser", "Username and Password are required.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(Type))
            {
                ModelState.AddModelError("Type", "Please select a user type.");
                return View();
            }

            var userType = db.UserTypes.FirstOrDefault(x => x.TypeId.ToString() == Type);
            if (userType == null)
            {
                ModelState.AddModelError("Type", "Invalid user type selected.");
                return View();
            }

            var encryptedPassword = Encryption.Encrypt(user.Password);
            var existingUser = db.Users.FirstOrDefault(x =>
                x.Username == user.Username &&
                x.Password == encryptedPassword &&
                x.Type == userType.TypeId
            );

           
            if (existingUser == null)
            {
                ModelState.AddModelError("InvalidUser", "Invalid username, password, or user type.");
                return View();
            }

            // Store session variables
            Session["UserId"] = existingUser.UserId;
            Session["Username"] = existingUser.Username;
            Session["UserType"] = existingUser.Type;
            Session["FullName"] = $"{existingUser.FirstName} {existingUser.LastName}";

            return RedirectToAction("Dashboard");
        }

        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            int typeId = Convert.ToInt32(Session["UserType"]);
            var userType = db.UserTypes.FirstOrDefault(u => u.TypeId == typeId)?.Type;

            if (userType == "Donor")
                return RedirectToAction("Donor", "Dashboard");

            if (userType == "NGO")
                return RedirectToAction("Index", "InventoryItems");

            return RedirectToAction("login");
        }

        public ActionResult NGODashboard()
        {
            if (!IsUserType("NGO"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(); // Should point to Views/Users/NGODashboard.cshtml
        }

        private bool IsUserType(string type)
        {
            int? userTypeId = Session["UserType"] as int?;
            if (userTypeId == null)
                return false;

            var userType = db.UserTypes.FirstOrDefault(t => t.TypeId == userTypeId)?.Type;
            return userType == type;
        }

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // GET: Users/Registration
        public ActionResult Registration()
        {
            ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Include = "Title,FirstName,LastName,Username,Email,PhoneNumber,Address,City,CNIC,Type,Password")] User user)
        {
            ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type", user.Type);

            if (db.Users.Any(x => x.Username == user.Username))
                ModelState.AddModelError("Username", "Username already exists.");
            if (db.Users.Any(x => x.Email == user.Email))
                ModelState.AddModelError("Email", "Email already exists.");

            if (!new EmailAddressAttribute().IsValid(user.Email))
                ModelState.AddModelError("Email", "Invalid email address format.");

            if (!ModelState.IsValid)
                return View(user);

            user.Password = Encryption.Encrypt(user.Password);
            user.Created_Date = DateTime.Now;
            user.Updated_Date = DateTime.Now;

            // ✅ Force default value for LastName if null
            if (string.IsNullOrWhiteSpace(user.LastName))
                user.LastName = "-";

            db.Users.Add(user);
            db.SaveChanges();

            // ✅ Send confirmation email
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(user.Email);
                mail.From = new MailAddress("danyal.alam2025@gmail.com", "Goodwill NGO");
                mail.Subject = "Welcome to Goodwill System";
                mail.Body = $"Dear {user.Title} {user.FirstName},\n\nThank you for registering at the Goodwill Contribution System.\n\nRegards,\nGoodwill NGO Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential("danyal.alam2025@gmail.com", "glodjyygpmqrjhtq")
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "User registered, but email sending failed: " + ex.Message);
            }

            TempData["SuccessMessage"] = "Account created successfully!";
            return RedirectToAction("Login");
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type", user.Type);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserId,Title,FirstName,LastName,Username,Email,PhoneNumber,Address,City,CNIC,Type,Password,Created_Date,Updated_Date")] User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.UserTypelist = new SelectList(db.UserTypes, "TypeId", "Type", user.Type);
                return View(user);
            }

            user.Password = Encryption.Encrypt(user.Password);
            user.Updated_Date = DateTime.Now;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Users/Logout
        public ActionResult Logout()
        {
            // Clear individual session keys (optional clarity)
            Session["UserId"] = null;
            Session["Username"] = null;
            Session["UserType"] = null;
            Session["FullName"] = null;

            // Clear and abandon session
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
