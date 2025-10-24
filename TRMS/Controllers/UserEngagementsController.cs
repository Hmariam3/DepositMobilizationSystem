using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;

namespace TRMS.Controllers
{
    public class UserEngagementsController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // GET: UserEngagements
        public ActionResult Index()
        {
            string UserName = Session["UserName"].ToString();

            return View(db.UserEngagements.Where(e=>e.created_by== UserName).ToList());
        }

        // GET: UserEngagements/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserEngagement userEngagement = db.UserEngagements.Find(id);
            if (userEngagement == null)
            {
                return HttpNotFound();
            }
            return View(userEngagement);
        }

        // GET: UserEngagements/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserEngagements/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "id,Contacts,Promise,Deposited,TotalAmount")] UserEngagement userEngagement)
        {

          if(userEngagement.Contacts<0 || userEngagement.Deposited < 0 ||userEngagement.Promise < 0 || userEngagement.TotalAmount < 0)
            {
                TempData["errormessage"] = "The value must   Greater Than 0";
                return View(userEngagement);
            }
            if (ModelState.IsValid)
            {
                userEngagement.created_by = Session["UserName"].ToString();
                userEngagement.Created_Date = DateTime.Now;
                db.UserEngagements.Add(userEngagement);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userEngagement);
        }

        // GET: UserEngagements/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserEngagement userEngagement = db.UserEngagements.Find(id);
            if (userEngagement == null)
            {
                return HttpNotFound();
            }
            return View(userEngagement);
        }

        // POST: UserEngagements/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,Contacts,Promise,Deposited,TotalAmount")] UserEngagement userEngagement)
        {
            if (userEngagement.Contacts < 0 || userEngagement.Deposited < 0 || userEngagement.Promise < 0 || userEngagement.TotalAmount < 0)
            {
                TempData["errormessage"] = "The value must   Greater Than 0";
                return View(userEngagement);
            }
            if (ModelState.IsValid)
            {
                userEngagement.created_by = Session["UserName"].ToString();
                userEngagement.Created_Date = DateTime.Now;
                db.Entry(userEngagement).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userEngagement);
        }

        // GET: UserEngagements/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserEngagement userEngagement = db.UserEngagements.Find(id);
            if (userEngagement == null)
            {
                return HttpNotFound();
            }
            return View(userEngagement);
        }

        // POST: UserEngagements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserEngagement userEngagement = db.UserEngagements.Find(id);
            db.UserEngagements.Remove(userEngagement);
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
