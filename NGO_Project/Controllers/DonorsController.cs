using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NGO_Project.Models;

namespace NGO_Project.Controllers
{
    public class DonorsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: Donors/Home
        public ActionResult Index()
        {
            int currentNGOId = Convert.ToInt32(Session["UserId"]);

            var donationEntries = (from d in db.Donations
                                   join u in db.Users on d.DonorId equals u.UserId
                                   where d.UserId == currentNGOId
                                   orderby d.DonationDate descending
                                   select new DonorCRMViewModel
                                   {
                                       DonationId = d.DonationId,
                                       DonorId = d.DonorId,
                                       DonationDate = d.DonationDate,
                                       Status = d.Status,
                                       Notes = d.Notes,
                                       DonorName = u.FirstName + " " + u.LastName,
                                       DonorEmail = u.Email,
                                       DonorPhone = u.PhoneNumber,
                                       Items = d.DonationItems.Select(di => new DonationItemViewModel
                                       {
                                           ItemName = di.ItemMaster.ItemName,
                                           Quantity = di.Quantity,
                                           Unit = di.Unit
                                       }).ToList()
                                   }).ToList();

            return View(donationEntries);
        }

        // GET: Donors/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User donor = db.Users.Find(id);
            if (donor == null)
            {
                return HttpNotFound();
            }
            return View(donor);
        }

        // GET: Donors/Invoice/5
        public ActionResult Invoice(int? id, int? donationId)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var donor = db.Users
                .FirstOrDefault(u => u.UserId == id);

            if (donor == null) return HttpNotFound();

            // Fetch donations for this user (donor)
            var donations = db.Donations
                .Include(dn => dn.DonationItems.Select(di => di.ItemMaster))
                .Where(dn => dn.DonorId == donor.UserId)
                .ToList();

            ViewBag.Donations = donations;
            ViewBag.SpecificDonationId = donationId;
            return View(donor);
        }

        // GET: Donors/Create
        public ActionResult Create()
        {
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.InventoryId, Name = i.ItemMaster.ItemName }), "Id", "Name");
            return View();
        }

        // POST: Donors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserId,FirstName,LastName,Email,PhoneNumber,Type")] User donor)
        {
            if (ModelState.IsValid)
            {
                donor.Created_Date = DateTime.Now;
                donor.Updated_Date = DateTime.Now;
                db.Users.Add(donor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.InventoryId, Name = i.ItemMaster.ItemName }), "Id", "Name");
            return View(donor);
        }

        [HttpPost]
        public JsonResult SaveDonor(User model, List<int> inventoryItemIds, string FullName, string Email, string PhoneNumber)
        {
            try
            {
                // 1. Identify or Create the User (The "Donor" in Users table)
                var donorUser = db.Users.FirstOrDefault(u => u.Email == Email && u.Email != "");
                
                if (donorUser == null)
                {
                    // Create new user for this donor
                    var names = (FullName ?? "Anonymous").Split(new[] { ' ' }, 2);
                    var firstName = names[0];
                    var lastName = names.Length > 1 ? names[1] : "-";
                    
                    var donorTypeId = db.UserTypes.FirstOrDefault(t => t.Type == "Donor")?.TypeId;
                    
                    donorUser = new User
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = Email,
                        PhoneNumber = PhoneNumber,
                        Username = Email ?? ("donor_" + Guid.NewGuid().ToString().Substring(0, 8)),
                        Type = donorTypeId,
                        Created_Date = DateTime.Now,
                        Updated_Date = DateTime.Now
                    };
                    
                    db.Users.Add(donorUser);
                    db.SaveChanges(); // Get UserId
                }

                // 2. We now use donorUser.UserId directly as DonorId in the Donation record.
                // No separate Donor record is needed as per the new schema.

                // 3. Create Donation History Record
                var itemIdsToProcess = inventoryItemIds ?? new List<int>();
                Donation donation = null;
                
                if (itemIdsToProcess.Any())
                {
                    donation = new Donation
                    {
                        DonorId = donorUser.UserId,
                        UserId = Convert.ToInt32(Session["UserId"]), // Recorded by current NGO user
                        DonationDate = DateTime.Now,
                        Status = "Completed",
                        Notes = "Batch Donation"
                    };
                    db.Donations.Add(donation);
                    db.SaveChanges();

                    foreach (var itemId in itemIdsToProcess)
                    {
                        var inventoryRecord = db.InventoryItems.Include(i => i.ItemMaster)
                                                .FirstOrDefault(i => i.InventoryId == itemId);
                        
                        if (inventoryRecord != null)
                        {
                            var donationItem = new DonationItem
                            {
                                DonationId = donation.DonationId,
                                ItemId = inventoryRecord.ItemId,
                                Quantity = Convert.ToDecimal(inventoryRecord.Quantity),
                                Unit = inventoryRecord.ItemMaster?.Unit ?? "pcs",
                                CreationDate = DateTime.Now
                            };
                            db.DonationItems.Add(donationItem);
                        }
                    }
                    db.SaveChanges();
                }

                return Json(new { success = true, donorId = donorUser.UserId, donationId = donation?.DonationId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message + (ex.InnerException != null ? " Inner: " + ex.InnerException.Message : "") });
            }
        }

        [HttpGet]
        public JsonResult CheckExistingDonor(string email)
        {
            // Check Users table ONLY
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.Email != "");
            if (user != null)
            {
                return Json(new
                {
                    success = true,
                    exists = true,
                    fullName = (user.FirstName + " " + user.LastName).Trim(),
                    phoneNumber = user.PhoneNumber
                }, JsonRequestBehavior.AllowGet);
            }

            // If information does not exist in Users table, fetch the default Anonymous user record
            var anonymousUser = db.Users.FirstOrDefault(u => u.Username == "Anonymous");
            if (anonymousUser != null)
            {
                return Json(new
                {
                    success = true,
                    exists = true,
                    fullName = anonymousUser.FirstName.Trim(),
                    phoneNumber = anonymousUser.PhoneNumber,
                    email = anonymousUser.Email // Returning this to potentially update the UI
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = "Anonymous record not found." }, JsonRequestBehavior.AllowGet);
        }

        // GET: Donors/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User donor = db.Users.Find(id);
            if (donor == null)
            {
                return HttpNotFound();
            }
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.InventoryId, Name = i.ItemMaster.ItemName }), "Id", "Name");
            return View(donor);
        }

        // POST: Donors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User donor)
        {
            if (ModelState.IsValid)
            {
                db.Entry(donor).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.InventoryId, Name = i.ItemMaster.ItemName }), "Id", "Name");
            return View(donor);
        }

        // GET: Donors/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User donor = db.Users.Find(id);
            if (donor == null)
            {
                return HttpNotFound();
            }
            return View(donor);
        }

        // POST: Donors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User donor = db.Users.Find(id);
            db.Users.Remove(donor);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult GetDonatedItems(int id, int? donationId = null)
        {
            // Fetch donations for this donor
            var donationsQuery = db.Donations
                .Where(d => d.DonorId == id);

            // If a specific donationId is provided, filter for only that record
            if (donationId.HasValue)
            {
                donationsQuery = donationsQuery.Where(d => d.DonationId == donationId.Value);
            }

            var donations = donationsQuery
                .OrderByDescending(d => d.DonationDate)
                .Select(d => new
                {
                    DonationId = d.DonationId,
                    Date = d.DonationDate,
                    Items = d.DonationItems.Select(di => new
                    {
                        ItemName = di.ItemMaster.ItemName,
                        Quantity = di.Quantity,
                        Unit = di.Unit
                    }).ToList()
                }).ToList()
                .Select(d => new {
                    d.DonationId,
                    Date = d.Date.HasValue ? d.Date.Value.ToString("MMM dd, yyyy hh:mm tt") : "N/A",
                    d.Items
                }).ToList();

            if (donations.Count > 0)
            {
                return Json(new { success = true, donations = donations, type = "grouped" }, JsonRequestBehavior.AllowGet);
            }

            // Fallback for legacy data (Check first donation item if grouped not found)
            var donor = db.Users.FirstOrDefault(u => u.UserId == id);
            var firstDonation = db.Donations.Include(dn => dn.DonationItems.Select(di => di.ItemMaster))
                                   .FirstOrDefault(dn => dn.DonorId == id);
            var firstItem = firstDonation?.DonationItems.FirstOrDefault();
            if (donor != null && firstItem != null)
            {
                return Json(new 
                { 
                    success = true, 
                    summary = new {
                        ItemName = firstItem.ItemMaster.ItemName,
                        Quantity = firstItem.Quantity,
                        Unit = firstItem.Unit,
                        Date = donor.Created_Date.HasValue ? donor.Created_Date.Value.ToString("MMM dd, yyyy") : "N/A"
                    }, 
                    type = "summary" 
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, donations = new List<object>(), type = "none" }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}