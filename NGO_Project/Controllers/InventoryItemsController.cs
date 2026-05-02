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
    public class InventoryItemsController : Controller
    {
        private NGOEntities db = new NGOEntities();

        // GET: InventoryItems
        public ActionResult Index()
        {
            var currentUserId = Convert.ToInt32(Session["UserId"]);

            var viewModel = new InventoryDashboardViewModel
            {
                NewItem = new InventoryItem(),

                // Group identical items and sum their quantity for the current user
                InventoryItems = db.InventoryItems
                                  .Where(i => i.CreatedBy == currentUserId)
                                  .GroupBy(i => i.ItemId)
                                  .ToList()
                                  .Select(g => new InventoryItem
                                  {
                                      InventoryId = g.First().InventoryId,
                                      ItemId = g.Key,
                                      Quantity = g.Sum(x => x.Quantity),
                                      ExpirationDate = g.Max(x => x.ExpirationDate),
                                      LastUpdated = g.Max(x => x.LastUpdated) ?? DateTime.Now,
                                      QualityCheckStatus = g.First().QualityCheckStatus,
                                      ItemMaster = g.First().ItemMaster
                                  })
                                  .OrderByDescending(i => i.InventoryId)
                                  .ToList(),
            };

            ViewBag.CategoriesList = db.Categories
                 .Where(x => x.CategoryName != null && x.CreatedBy == currentUserId)
                 .OrderBy(c => c.CategoryName)
                 .ToList();


            // Populate dropdowns
            ViewBag.Units = new SelectList(
                db.Categories.Where(x => x.CategoryName != null && x.CreatedBy == currentUserId)
                             .Select(c => c.Unit)
                             .Distinct()
                             .ToList()
            );

            ViewBag.ItemMasters = new SelectList(
                db.ItemMasters.Where(x=>x.CreatedBy == currentUserId).OrderBy(x => x.ItemName),
                "ItemId", "ItemName"
            );

            ViewBag.ItemMastersList = db.ItemMasters.Where(x=> x.CreatedBy == currentUserId)
                .OrderBy(x => x.ItemName)
                .ToList();

            ViewBag.Categories = new SelectList(
                db.Categories.Where(x => x.CategoryName != null && x.CreatedBy == currentUserId),
                "CategoryId", "CategoryName"
            );

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult CreateBatch(List<InventoryItem> items)
        {
            if (items == null || !items.Any())
                return Json(new { success = false, message = "No items to add." });

            var savedIds = new List<int>();
            string lastCategory = "Donation";

            foreach (var item in items)
            {
                item.LastUpdated = DateTime.Now;
                var currentUserId = Convert.ToInt32(Session["UserId"]);
                item.CreatedBy = currentUserId;

                var existingBatch = db.InventoryItems.FirstOrDefault(i => i.ItemId == item.ItemId && i.CreatedBy == currentUserId);
                if (existingBatch != null)
                {
                    existingBatch.Quantity += item.Quantity;
                    existingBatch.ExpirationDate = item.ExpirationDate ?? existingBatch.ExpirationDate;
                    existingBatch.QualityCheckStatus = null; // Reset to Pending on new donation
                    existingBatch.LastUpdated = DateTime.Now;
                    db.Entry(existingBatch).State = EntityState.Modified;
                    db.SaveChanges();
                    savedIds.Add(existingBatch.InventoryId);
                    
                    var itemInfo = db.ItemMasters.Include(im => im.Category).FirstOrDefault(im => im.ItemId == existingBatch.ItemId);
                    lastCategory = itemInfo?.Category?.CategoryName ?? "Donation";
                }
                else
                {
                    db.InventoryItems.Add(item);
                    db.SaveChanges();
                    savedIds.Add(item.InventoryId);
                    
                    var itemInfo = db.ItemMasters.Include(im => im.Category).FirstOrDefault(im => im.ItemId == item.ItemId);
                    lastCategory = itemInfo?.Category?.CategoryName ?? "Donation";
                }
            }

            return Json(new { success = true, itemIds = savedIds, categoryName = lastCategory });
        }


        // POST: InventoryItems/Create (for AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InventoryDashboardViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null && !string.IsNullOrEmpty(Session["UserId"].ToString()))
                {
                    var currentUserId = Convert.ToInt32(Session["UserId"]);
                    viewModel.NewItem.CreatedBy = currentUserId;

                    // Intercept duplicate creation and sum quantity dynamically
                    var existingBatch = db.InventoryItems.FirstOrDefault(i => i.ItemId == viewModel.NewItem.ItemId && i.CreatedBy == currentUserId);
                    if (existingBatch != null)
                    {
                        existingBatch.Quantity += viewModel.NewItem.Quantity;
                        existingBatch.ExpirationDate = viewModel.NewItem.ExpirationDate ?? existingBatch.ExpirationDate;
                        existingBatch.LastUpdated = DateTime.Now;
                        db.Entry(existingBatch).State = EntityState.Modified;

                        // Sync InventoryId for UI response
                        viewModel.NewItem.InventoryId = existingBatch.InventoryId;
                    }
                    else
                    {
                        viewModel.NewItem.CreatedBy = Convert.ToInt32(Session["UserId"]);
                        viewModel.NewItem.QualityCheckStatus = true;
                        db.InventoryItems.Add(viewModel.NewItem);
                    }

                    db.SaveChanges();

                    // ✅ Get Category name for modal
                    var categoryName = db.ItemMasters
                                         .Where(i => i.ItemId == viewModel.NewItem.ItemId)
                                         .Select(i => i.Category.CategoryName)
                                         .FirstOrDefault();

                    ViewBag.CategoriesList = db.Categories
                         .Where(x => x.CategoryName != null && x.CreatedBy == currentUserId)
                         .OrderBy(c => c.CategoryName)
                         .ToList();

                    return Json(new
                    {
                        success = true,
                        itemId = viewModel.NewItem.InventoryId,
                        categoryName = categoryName
                    });
                }
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            return Json(new { success = false, errors = string.Join(", ", errors) });
        }


        [HttpPost]
        public JsonResult EditCategory(int id, string categoryName, string unit)
        {
            var category = db.Categories.Find(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            category.CategoryName = categoryName;
            category.Unit = unit;
            db.SaveChanges();
            return Json(new { success = true });
        }



        [HttpPost]
        public JsonResult DeleteCategory(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            db.Categories.Remove(category);
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetInventoryItemAPI(int id)
        {
            var item = db.InventoryItems.Find(id);
            if (item != null)
            {
                var categoryName = db.ItemMasters
                                     .Where(i => i.ItemId == item.ItemId)
                                     .Select(i => i.Category.CategoryName)
                                     .FirstOrDefault();

                return Json(new 
                { 
                    InventoryId = item.InventoryId, 
                    item.ItemId, 
                    item.Quantity, 
                    ExpirationDate = item.ExpirationDate?.ToString("yyyy-MM-dd"), 
                    item.QualityCheckStatus,
                    CategoryName = categoryName,
                    Unit = item.ItemMaster?.Unit
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        // GET: InventoryItems/GetInventoryItems (Action for AJAX to refresh the table)
        public ActionResult GetInventoryItems()
        {
            var inventoryItems = db.InventoryItems
                                   .GroupBy(i => i.ItemId)
                                   .ToList()
                                   .Select(g => new InventoryItem
                                   {
                                       InventoryId = g.First().InventoryId,
                                       ItemId = g.Key,
                                       Quantity = g.Sum(x => x.Quantity),
                                       ExpirationDate = g.Max(x => x.ExpirationDate),
                                       LastUpdated = g.Max(x => x.LastUpdated) ?? DateTime.Now,
                                       QualityCheckStatus = g.First().QualityCheckStatus,
                                       ItemMaster = g.First().ItemMaster
                                   })
                                   .OrderByDescending(i => i.InventoryId)
                                   .ToList();

            return PartialView("_InventoryTableRows", inventoryItems);
        }

        // GET: InventoryItems/GetSummaryData (Action for AJAX to refresh summary cards)
        public ActionResult GetSummaryData()
        {
            var currentUserId = Convert.ToInt32(Session["UserId"]);
            var inventoryItems = db.InventoryItems.Where(i => i.CreatedBy == currentUserId).ToList();
            var total = inventoryItems.Count();
            var available = inventoryItems.Count(i => i.Quantity > 5);
            var lowStock = inventoryItems.Count(i => i.Quantity <= 5 && i.Quantity > 0);

            // Return JSON data for the summary cards
            return Json(new { total = total, available = available, lowStock = lowStock }, JsonRequestBehavior.AllowGet);
        }

        // The other CRUD actions (Details, Edit, Delete) are included for completeness.

        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            InventoryItem inventoryItem = db.InventoryItems.Find(id);
            if (inventoryItem == null) return HttpNotFound();

            ViewBag.ItemId = new SelectList(db.ItemMasters, "ItemId", "ItemName", inventoryItem.ItemId);
            return View(inventoryItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "InventoryId,ItemId,Quantity,Unit,QualityCheckStatus,ExpirationDate,LastUpdated,CreatedBy")] InventoryItem inventoryItem)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null && !string.IsNullOrEmpty(Session["UserId"].ToString()))
                {
                    var currentUserId = Convert.ToInt32(Session["UserId"]);
                    
                    // Check if another record already exists for this ItemId and NGO
                    var existingItem = db.InventoryItems.FirstOrDefault(i => 
                        i.ItemId == inventoryItem.ItemId && 
                        i.CreatedBy == currentUserId && 
                        i.InventoryId != inventoryItem.InventoryId);

                    if (existingItem != null)
                    {
                        // Merge current changes into the existing record
                        existingItem.Quantity += inventoryItem.Quantity;
                        existingItem.ExpirationDate = inventoryItem.ExpirationDate ?? existingItem.ExpirationDate;
                        existingItem.QualityCheckStatus = inventoryItem.QualityCheckStatus;
                        existingItem.LastUpdated = DateTime.Now;
                        
                        db.Entry(existingItem).State = EntityState.Modified;
                        
                        // Remove the redundant record being edited
                        var itemToRemove = db.InventoryItems.Find(inventoryItem.InventoryId);
                        if (itemToRemove != null) db.InventoryItems.Remove(itemToRemove);
                    }
                    else
                    {
                        inventoryItem.LastUpdated = DateTime.Now;
                        inventoryItem.CreatedBy = currentUserId;
                        db.Entry(inventoryItem).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.ItemId = new SelectList(db.ItemMasters, "ItemId", "ItemName", inventoryItem.ItemId);
            return View(inventoryItem);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            InventoryItem inventoryItem = db.InventoryItems.Find(id);
            if (inventoryItem == null) return HttpNotFound();
            return View(inventoryItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            InventoryItem inventoryItem = db.InventoryItems.Find(id);
            db.InventoryItems.Remove(inventoryItem);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult UpdateQualityStatus(int id, bool? status)
        {
            var item = db.InventoryItems.Find(id);
            if (item == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            try
            {
                item.QualityCheckStatus = status;
                item.LastUpdated = DateTime.Now;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteInventoryItemAPI(int id)
        {
            var item = db.InventoryItems.Find(id);
            if (item == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            try
            {
                db.InventoryItems.Remove(item);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to delete due to constraints." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory([Bind(Include = "CategoryName, Unit, CreatedBy")] Category viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = Convert.ToInt32(Session["UserId"]);
                if (Session["UserId"] != null && !string.IsNullOrEmpty(Session["UserId"].ToString()))
                {
                    var existingCategories = db.Categories.FirstOrDefault(x =>
                        x.CategoryName == viewModel.CategoryName && x.CreatedBy == currentUserId);
                    viewModel.CreatedBy = currentUserId;
                    if (existingCategories == null)
                    {
                        db.Categories.Add(viewModel);
                        db.SaveChanges();
                        // pass units as distinct list from Category table
                        ViewBag.Units = new SelectList(db.Categories.Where(x => x.CategoryName != null && x.CreatedBy == currentUserId)
                                                       .Select(c => c.Unit)
                                                       .Distinct()
                                                       .ToList());
                        ViewBag.Categories = new SelectList(db.Categories.Where(x => x.CategoryName != null && x.CreatedBy == currentUserId), "CategoryId", "CategoryName");
                        return Json(new { success = true, message = "Category created successfully!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Category already exists." });
                    }
                }
            }

            return Json(new { success = false, message = "Invalid data submitted or user not logged in." });
        }
        public ActionResult GetUnitByCategory(int categoryId)
        {
            var unit = db.Categories
                         .Where(c => c.CategoryId == categoryId)
                         .Select(c => new
                         {
                             UnitName = c.Unit
                         })
                         .FirstOrDefault();

            return Json(unit, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetItemDetails(int itemId)
        {
            var itemDetails = db.ItemMasters
                                .Where(i => i.ItemId == itemId)
                                .Select(i => new
                                {
                                    CategoryName = i.Category.CategoryName,
                                    UnitName = i.Unit
                                })
                                .FirstOrDefault();

            return Json(itemDetails, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateItemMaster(ItemMaster itemMaster)
        {
            if (Session["UserId"] == null || string.IsNullOrEmpty(Session["UserId"].ToString()))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            try 
            {
                itemMaster.CreatedBy = Convert.ToInt32(Session["UserId"]);
                itemMaster.LastUpdated = DateTime.Now;
                itemMaster.Status = true;

                db.ItemMasters.Add(itemMaster);
                db.SaveChanges();

                return Json(new { success = true, message = "Item master saved successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while saving the item." });
            }
        }

        [HttpPost]
        public ActionResult DeleteItemMaster(int id)
        {
            var item = db.ItemMasters.Find(id);
            if (item == null)
            {
                return Json(new { success = false, message = "Item not found." });
            }

            try
            {
                db.ItemMasters.Remove(item);
                db.SaveChanges();
                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Cannot delete this item because it might be referenced elsewhere." });
            }
        }

        public ActionResult GetItemsByCategory(int categoryId)
        {
            var items = db.ItemMasters
                .Where(x => x.CategoryId == categoryId)
                .Select(x => new { Id = x.ItemId, ItemName = x.ItemName })
                .ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
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