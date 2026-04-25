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
    public class AidRequestsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: AidRequests
        public ActionResult Index()
        {
            // Fetch all active aid requests and pass them to the view.
            var aidRequests = db.AidRequests.Include(a => a.Category)
    .Where(r => r.IsActive == true)
    .OrderByDescending(r => r.RequestId) // or r.CreatedDate
    .ToList();

            ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null), "CategoryId", "CategoryName");
            return View(aidRequests);
        }

        // GET: AidRequests/Create
        public ActionResult Create()
        {
            // Assuming your User entity has FirstName
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName");
            ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null), "CategoryId", "CategoryName");
            return View();
        }

        // POST: AidRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
                [Bind(Include = "RequestId,UserId,RequestTitle,Description,CategoryId,UrgencyLevel,Location")] AidRequest aidRequest,
                [Bind(Include = "ItemName,Quantity,Unit,ItemRequestCount")] RequestedItem requestedItem
 )
        {

            // 1️⃣ Save AidRequest first
            aidRequest.PostDate = DateTime.Now;
            aidRequest.IsActive = true;
            aidRequest.UserId = Convert.ToInt16(Session["UserId"]);
            aidRequest.IsPosted = 1;
            db.AidRequests.Add(aidRequest);
            db.SaveChanges(); // This will generate RequestId

            // 2️⃣ Save RequestedItem linked to the AidRequest
            requestedItem.RequestId = aidRequest.RequestId; // FK link

            // (Optional defaults to avoid nulls if form doesn’t send data)
            if (string.IsNullOrWhiteSpace(requestedItem.ItemName))
                requestedItem.ItemName = aidRequest.RequestTitle;
            if (requestedItem.Quantity <= 0)
                requestedItem.Quantity = 1;
            if (string.IsNullOrWhiteSpace(requestedItem.Unit))
                requestedItem.Unit = "pcs";
            requestedItem.ItemRequestCount = 1; // Default value

            db.RequestedItems.Add(requestedItem);
            db.SaveChanges();

            ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null), "CategoryId", "CategoryName");
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", aidRequest.UserId);

            return RedirectToAction("Index");
        }

        // GET: AidRequests/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AidRequest aidRequest = db.AidRequests.Find(id);
            if (aidRequest == null)
            {
                return HttpNotFound();
            }

            ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null), "CategoryId", "CategoryName", aidRequest.CategoryId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", aidRequest.UserId);
            return View(aidRequest);
        }

        // POST: AidRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestId,UserId,RequestTitle,Description,CategoryId,UrgencyLevel,Location,PostDate,IsActive")] AidRequest aidRequest)
        {
            if (ModelState.IsValid)
            {
                db.Entry(aidRequest).State = EntityState.Modified;
                aidRequest.PostDate = DateTime.Now;
                aidRequest.IsActive = true;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null), "CategoryId", "CategoryName", aidRequest.CategoryId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", aidRequest.UserId);
            return View(aidRequest);
        }


        // GET: AidRequests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AidRequest aidRequest = db.AidRequests.Find(id);
            if (aidRequest == null)
            {
                return HttpNotFound();
            }
            return View(aidRequest);
        }

        // POST: AidRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AidRequest aidRequest = db.AidRequests.Find(id);
            db.AidRequests.Remove(aidRequest);
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