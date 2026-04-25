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
    public class RequestedItemsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: RequestedItems
        public ActionResult Index()
        {

            var requestedItems = db.RequestedItems
    .Include(r => r.AidRequest).ToList();
            return View(requestedItems);
        }

        // GET: RequestedItems/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RequestedItem requestedItem = db.RequestedItems.Find(id);
            if (requestedItem == null)
            {
                return HttpNotFound();
            }
            return View(requestedItem);
        }

        // GET: RequestedItems/Create
        public ActionResult Create()
        {
            ViewBag.RequestId = new SelectList(db.AidRequests, "RequestId", "RequestTitle");
            ViewBag.RequestedItemId = new SelectList(db.AidRequests, "RequestId", "RequestTitle");
            return View();
        }

        // POST: RequestedItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RequestedItemId,RequestId,ItemName,Quantity,Unit")] RequestedItem requestedItem)
        {
            if (ModelState.IsValid)
            {
                db.RequestedItems.Add(requestedItem);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RequestId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestId);
            ViewBag.RequestedItemId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestedItemId);
            return View(requestedItem);
        }

        // GET: RequestedItems/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RequestedItem requestedItem = db.RequestedItems.Find(id);
            if (requestedItem == null)
            {
                return HttpNotFound();
            }
            ViewBag.RequestId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestId);
            ViewBag.RequestedItemId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestedItemId);
            return View(requestedItem);
        }

        // POST: RequestedItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestedItemId,RequestId,ItemName,Quantity,Unit")] RequestedItem requestedItem)
        {
            if (ModelState.IsValid)
            {
                db.Entry(requestedItem).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RequestId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestId);
            ViewBag.RequestedItemId = new SelectList(db.AidRequests, "RequestId", "RequestTitle", requestedItem.RequestedItemId);
            return View(requestedItem);
        }

        // GET: RequestedItems/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RequestedItem requestedItem = db.RequestedItems.Find(id);
            if (requestedItem == null)
            {
                return HttpNotFound();
            }
            return View(requestedItem);
        }

        // POST: RequestedItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            RequestedItem requestedItem = db.RequestedItems.Find(id);
            db.RequestedItems.Remove(requestedItem);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public JsonResult UpdateIsPosted(int id)
        {
            try
            {
                var itemRequest = db.RequestedItems.FirstOrDefault(r => r.RequestedItemId == id);
                if (itemRequest == null)
                    return Json(new { success = false, message = "Requested item not found." });

                var airRequest = db.AidRequests.FirstOrDefault(r => r.RequestId == itemRequest.RequestId);
                if (airRequest == null)
                    return Json(new { success = false, message = "Aid request not found." });

                // Update IsPosted first
                airRequest.IsPosted = 1;
                db.Entry(airRequest).State = EntityState.Modified;
                db.SaveChanges();

                // Update itemRequest count
                if (itemRequest.ItemRequestCount == 0 || itemRequest.ItemRequestCount == null)
                {
                    itemRequest.ItemRequestCount = 1;
                }
                else
                {
                    itemRequest.ItemRequestCount += 1;
                }
                db.Entry(itemRequest).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, message = "Updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
