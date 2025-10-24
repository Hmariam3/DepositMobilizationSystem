using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class PostionsController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // GET: Postions
        [AuthorizeRoles("Admin")]
        public ActionResult Index()
        {
            return View(db.Postions.ToList());
        }

        // GET: Postions/Details/5
        [AuthorizeRoles("Admin")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Postion postion = db.Postions.Find(id);
            if (postion == null)
            {
                return HttpNotFound();
            }
            return View(postion);
        }

        // GET: Postions/Create
        [AuthorizeRoles("Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Postions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AuthorizeRoles("Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Postion1,LimitAmout,Remark")] Postion postion)
        {

            if (postion == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(postion);
            }

            // Validate Transaction Reference Number
            if (string.IsNullOrWhiteSpace(postion.LimitAmout.ToString()))
            {
                TempData["errorRes"] = "Please Enter Limit Amount.";
                return View(postion);
            }

            // Validate Transaction Amount
            if (postion.LimitAmout <= 0)
            {
                TempData["errorRes"] = "Limit Amount must be greater than zero.";
                return View(postion);
            }
            if (ModelState.IsValid)
            {
                db.Postions.Add(postion);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(postion);
        }
        [AuthorizeRoles("Admin")]
        // GET: Postions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Postion postion = db.Postions.Find(id);
            if (postion == null)
            {
                return HttpNotFound();
            }
            return View(postion);
        }

        // POST: Postions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Admin")]
        public ActionResult Edit([Bind(Include = "Id,Postion1,LimitAmout,Remark")] Postion postion)
        {
            if (ModelState.IsValid)
            {
                db.Entry(postion).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(postion);
        }

        // GET: Postions/Delete/5
        [AuthorizeRoles("Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Postion postion = db.Postions.Find(id);
            if (postion == null)
            {
                return HttpNotFound();
            }
            return View(postion);
        }

        // POST: Postions/Delete/5
        [AuthorizeRoles("Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Postion postion = db.Postions.Find(id);
            db.Postions.Remove(postion);
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
