using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGO_Project.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashboard

        private NGOEntities db = new NGOEntities();
        public ActionResult Index()
        {
            if (Session["UserId"] == null) 
            {

                return RedirectToAction("Login","Users");
            }
            return View();
        }

        public ActionResult NGO()
        {
            if (Session["UserId"] == null)
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));
                Response.Cache.SetNoStore();
                return RedirectToAction("Login", "Users");             
            }
            return View();
        }
        public ActionResult Donor()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));
            Response.Cache.SetNoStore();

            var aidRequestsWithUsers = (from ar in db.AidRequests
                                        join u in db.Users on ar.UserId equals u.UserId
                                        where  ar.IsPosted == 1
                                        orderby ar.PostDate descending
                                        select new
                                        {
                                            AidRequest = ar,
                                            User = u
                                        })
                                        .ToList<dynamic>().Select(x =>
                                        {
                                            dynamic obj = new System.Dynamic.ExpandoObject();
                                            obj.AidRequest = x.AidRequest;
                                            obj.User = x.User;
                                            return obj;
                                        }).ToList();  // important to cast for IEnumerable<dynamic>

            return View(aidRequestsWithUsers);
        }
    }
}