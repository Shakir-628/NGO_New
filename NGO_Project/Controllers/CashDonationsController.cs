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
    public class CashDonationsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: CashDonations
        public ActionResult Index()
        {
            //var cashDonations = db.CashDonations.Include(c => c.Donor);
            return View();
        }

        // GET: CashDonations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CashDonation cashDonation = db.CashDonations.Find(id);
            if (cashDonation == null)
            {
                return HttpNotFound();
            }
            return View(cashDonation);
        }

        // GET: CashDonations/Create
        public ActionResult Create()
        {
            ViewBag.DonorId = new SelectList(db.Donors, "UserId", "FullName");
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName");
            return View();
        }

        // POST: CashDonations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CashDonationId,DonorId,UserId,Amount,Currency,DonationDate,Notes")] CashDonation cashDonation)
        {
            if (ModelState.IsValid)
            {
                db.CashDonations.Add(cashDonation);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.DonorId = new SelectList(db.Donors, "UserId", "FullName", cashDonation.DonorId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", cashDonation.UserId);
            return View(cashDonation);
        }

        // GET: CashDonations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CashDonation cashDonation = db.CashDonations.Find(id);
            if (cashDonation == null)
            {
                return HttpNotFound();
            }
            ViewBag.DonorId = new SelectList(db.Donors, "UserId", "FullName", cashDonation.DonorId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", cashDonation.UserId);
            return View(cashDonation);
        }

        // POST: CashDonations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CashDonationId,DonorId,UserId,Amount,Currency,DonationDate,Notes")] CashDonation cashDonation)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cashDonation).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.DonorId = new SelectList(db.Donors, "UserId", "FullName", cashDonation.DonorId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", cashDonation.UserId);
            return View(cashDonation);
        }

        // GET: CashDonations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CashDonation cashDonation = db.CashDonations.Find(id);
            if (cashDonation == null)
            {
                return HttpNotFound();
            }
            return View(cashDonation);
        }

        // POST: CashDonations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CashDonation cashDonation = db.CashDonations.Find(id);
            db.CashDonations.Remove(cashDonation);
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
