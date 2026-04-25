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
    public class DistributionOrdersController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: DistributionOrders
        public ActionResult Index()
        {
            try
            {
                var vm = new Models.InventoryDocumentViewModel
                {
                    InventoryItems = db.InventoryItems.Include("Category").ToList(),
                    Documents = db.Documents.ToList()
                };

                return View(vm);

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        public JsonResult SaveDisbursement(Document doc)
        {
            try
            {
                doc.GeneratedDate = DateTime.Now;
                db.Documents.Add(doc);
                db.SaveChanges();

                return Json(new { success = true, message = "Donor Info disbursement saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: DistributionOrders/Details/5

        public ActionResult Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                DistributionOrder distributionOrder = db.DistributionOrders.Find(id);
                if (distributionOrder == null)
                {
                    return HttpNotFound();
                }
                return View(distributionOrder);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // GET: DistributionOrders/Create
        public ActionResult Create()
        {
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName");
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation");
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation");
            return View();
        }

        // POST: DistributionOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DistributionOrderId,UserId,DistributionLocation,OrderDate,Status,Notes")] DistributionOrder distributionOrder)
        {
            if (ModelState.IsValid)
            {
                db.DistributionOrders.Add(distributionOrder);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", distributionOrder.UserId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            return View(distributionOrder);
        }

        // GET: DistributionOrders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DistributionOrder distributionOrder = db.DistributionOrders.Find(id);
            if (distributionOrder == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", distributionOrder.UserId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            return View(distributionOrder);
        }

        // POST: DistributionOrders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DistributionOrderId,UserId,DistributionLocation,OrderDate,Status,Notes")] DistributionOrder distributionOrder)
        {
            if (ModelState.IsValid)
            {
                db.Entry(distributionOrder).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", distributionOrder.UserId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            ViewBag.DistributionOrderId = new SelectList(db.DistributionOrders, "DistributionOrderId", "DistributionLocation", distributionOrder.DistributionOrderId);
            return View(distributionOrder);
        }

        // GET: DistributionOrders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DistributionOrder distributionOrder = db.DistributionOrders.Find(id);
            if (distributionOrder == null)
            {
                return HttpNotFound();
            }
            return View(distributionOrder);
        }

        // POST: DistributionOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DistributionOrder distributionOrder = db.DistributionOrders.Find(id);
            db.DistributionOrders.Remove(distributionOrder);
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
