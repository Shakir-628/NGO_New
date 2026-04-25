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
    public class DonationItemsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: DonationItems
        public ActionResult Index()
        {
            try
            {
                var DonationItem = db.DonationItems.Include(d =>  d.Donation);
                return View(DonationItem.ToList());
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // GET: DonationItems/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonationItem donationItem = db.DonationItems.Find(id);
            if (donationItem == null)
            {
                return HttpNotFound();
            }
            return View(donationItem);
        }

        // GET: DonationItems/Create
        public ActionResult Create()
        {
            ViewBag.ItemId = new SelectList(db.InventoryItems, "ItemId", "ItemName");
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit");
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit");
            return View();
        }

        // POST: DonationItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DonationItemId,DonationId,ItemId,Quantity,Unit")] DonationItem donationItem)
        {
            if (ModelState.IsValid)
            {
                db.DonationItems.Add(donationItem);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ItemId = new SelectList(db.InventoryItems, "ItemId", "ItemName", donationItem.ItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            return View(donationItem);
        }

        // GET: DonationItems/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonationItem donationItem = db.DonationItems.Find(id);
            if (donationItem == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItemId = new SelectList(db.InventoryItems, "ItemId", "ItemName", donationItem.ItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            return View(donationItem);
        }

        // POST: DonationItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DonationItemId,DonationId,ItemId,Quantity,Unit")] DonationItem donationItem)
        {
            if (ModelState.IsValid)
            {
                db.Entry(donationItem).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ItemId = new SelectList(db.InventoryItems, "ItemId", "ItemName", donationItem.ItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            ViewBag.DonationItemId = new SelectList(db.DonationItems, "DonationItemId", "Unit", donationItem.DonationItemId);
            return View(donationItem);
        }

        // GET: DonationItems/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonationItem donationItem = db.DonationItems.Find(id);
            if (donationItem == null)
            {
                return HttpNotFound();
            }
            return View(donationItem);
        }

        // POST: DonationItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DonationItem donationItem = db.DonationItems.Find(id);
            db.DonationItems.Remove(donationItem);
            db.SaveChanges();
            return RedirectToAction("Index");
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
