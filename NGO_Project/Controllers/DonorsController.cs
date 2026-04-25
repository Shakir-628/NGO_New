using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NGO_Project;

namespace NGO_Project.Controllers
{
    public class DonorsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: Donors/Home
        public ActionResult Index()
        {
            int currentNGOId = Convert.ToInt32(Session["UserId"]);

            var donorDetails = db.Donors
                .Include(d => d.InventoryItem.ItemMaster)
                .Where(d => d.NGOId == currentNGOId)
                .OrderByDescending(d => d.CreatedDate)
                .ToList();

            return View(donorDetails);
        }

        // GET: Donors/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Donor donor = db.Donors.Find(id);
            if (donor == null)
            {
                return HttpNotFound();
            }
            return View(donor);
        }

        // GET: Donors/Create
        public ActionResult Create()
        {
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.Id, Name = i.ItemMaster.ItemName }), "Id", "Name");
            return View();
        }

        // POST: Donors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DonorId,FullName,Email,PhoneNumber,InventoryItemId")] Donor donor)
        {
            if (ModelState.IsValid)
            {
                donor.NGOId = Convert.ToInt32(Session["UserId"]);
                donor.CreatedDate = DateTime.Now;
                db.Donors.Add(donor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.Id, Name = i.ItemMaster.ItemName }), "Id", "Name", donor.InventoryItemId);
            return View(donor);
        }

        [HttpPost]
        public JsonResult SaveDonor(Donor model)
        {
            if (ModelState.IsValid)
            {
                // 1. Find or Create Donor (Upsert)
                var donor = db.Donors.FirstOrDefault(d => d.Email == model.Email);
                if (donor == null)
                {
                    // If new donor, save into CRM (DB)
                    donor = model;
                    if (donor.NGOId == 0 && Session["UserId"] != null)
                    {
                        donor.NGOId = Convert.ToInt32(Session["UserId"]);
                    }
                    donor.CreatedDate = DateTime.Now;
                    db.Donors.Add(donor);
                    db.SaveChanges();
                }
                else
                {
                    // Existing donor: Update info if changed
                    donor.FullName = model.FullName;
                    donor.PhoneNumber = model.PhoneNumber;
                    donor.InventoryItemId = model.InventoryItemId; // Update primary link
                    db.Entry(donor).State = EntityState.Modified;
                    db.SaveChanges();
                }

                // 2. Create Donation History Record (to allow multiple items tracking)
                if (model.InventoryItemId.HasValue && model.InventoryItemId.Value > 0)
                {
                    var inventoryRecord = db.InventoryItems.Include(i => i.ItemMaster)
                                            .FirstOrDefault(i => i.Id == model.InventoryItemId.Value);
                    
                    if (inventoryRecord != null)
                    {
                        var donation = new Donation
                        {
                            DonorId = donor.DonorId,
                            UserId = Convert.ToInt32(Session["UserId"]),
                            DonationDate = DateTime.Now,
                            Status = "Completed",
                            Notes = "Added via Inventory Workflow"
                        };
                        db.Donations.Add(donation);
                        db.SaveChanges();

                        var donationItem = new DonationItem
                        {
                            DonationId = donation.DonationId,
                            ItemId = inventoryRecord.ItemId,
                            Quantity = inventoryRecord.Quantity,
                            Unit = inventoryRecord.ItemMaster?.Unit ?? "pcs",
                            CreationDate = DateTime.Now
                        };
                        db.DonationItems.Add(donationItem);
                        db.SaveChanges();
                    }
                }

                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid data." });
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
            Donor donor = db.Donors.Find(id);
            if (donor == null)
            {
                return HttpNotFound();
            }
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.Id, Name = i.ItemMaster.ItemName }), "Id", "Name", donor.InventoryItemId);
            return View(donor);
        }

        // POST: Donors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DonorId,NGOId,FullName,Email,PhoneNumber,InventoryItemId,CreatedDate")] Donor donor)
        {
            if (ModelState.IsValid)
            {
                db.Entry(donor).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.InventoryItemId = new SelectList(db.InventoryItems.Select(i => new { Id = i.Id, Name = i.ItemMaster.ItemName }), "Id", "Name", donor.InventoryItemId);
            return View(donor);
        }

        // GET: Donors/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Donor donor = db.Donors.Find(id);
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
            Donor donor = db.Donors.Find(id);
            db.Donors.Remove(donor);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult GetDonatedItems(int id)
        {
            var detailedItems = db.DonationItems
                .Where(di => di.Donation.DonorId == id)
                .Select(di => new
                {
                    ItemName = di.ItemMaster.ItemName,
                    Quantity = di.Quantity,
                    Unit = di.Unit,
                    Date = di.CreationDate
                }).ToList()
                .Select(di => new {
                    di.ItemName,
                    di.Quantity,
                    di.Unit,
                    Date = di.Date.HasValue ? di.Date.Value.ToString("MMM dd, yyyy") : "N/A"
                }).ToList();

            if (detailedItems.Count > 0)
            {
                return Json(new { success = true, items = detailedItems, type = "detailed" }, JsonRequestBehavior.AllowGet);
            }

            // Fallback to Donor summary item
            var donor = db.Donors.Include(d => d.InventoryItem.ItemMaster).FirstOrDefault(d => d.DonorId == id);
            if (donor != null && donor.InventoryItem?.ItemMaster != null)
            {
                return Json(new 
                { 
                    success = true, 
                    summary = new {
                        ItemName = donor.InventoryItem.ItemMaster.ItemName,
                        Quantity = donor.InventoryItem.Quantity,
                        Unit = donor.InventoryItem.ItemMaster.Unit,
                        Date = donor.CreatedDate.HasValue ? donor.CreatedDate.Value.ToString("MMM dd, yyyy") : "N/A"
                    }, 
                    type = "summary" 
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, items = new List<object>(), type = "none" }, JsonRequestBehavior.AllowGet);
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