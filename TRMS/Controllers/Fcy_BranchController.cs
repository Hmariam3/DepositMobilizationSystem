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
    public class Fcy_BranchController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // GET: Fcy_Branch
        public ActionResult Index()
        {

            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            string role = Session["UserRole"].ToString();
            if (role == "Maker")
            {
                string UserName = Session["UserName"].ToString();
                var transactions = db.Fcy_Branch.Where(t => t.CreatedBy == UserName).ToList();
                if (transactions == null)
                {
                    transactions = new List<Fcy_Branch>();
                }
                return View(transactions);

            }
            else if (role == "Admin")
            {

                var transactions = db.Fcy_Branch.ToList();
                if (transactions == null)
                {
                    transactions = new List<Fcy_Branch>();
                }
                return View(transactions);

            }
            return View();



        }

        // GET: Fcy_Branch/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Fcy_Branch fcy_Branch = db.Fcy_Branch.Find(id);
            if (fcy_Branch == null)
            {
                return HttpNotFound();
            }
            return View(fcy_Branch);
        }

        // GET: Fcy_Branch/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Fcy_Branch/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create( Fcy_Branch fcy_Branch)
        {
            if (fcy_Branch == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(fcy_Branch);
            }
            if (fcy_Branch.FcyTargetAmount <= 0 || fcy_Branch.FcyTargetAmount > 500000000)
            {
                TempData["errorRes"] = "Fcy Amount must be greater than zero and less than or equal to five hundred million.";
                return View(fcy_Branch);
            }
            fcy_Branch.Branch = Session["UserHomeBranch"].ToString();
            var fcyexist= db.Fcy_Branch.Where(t => t.Branch.Trim() == fcy_Branch.Branch.Trim()).ToList();
            if (fcyexist.Count() >= 1)
            {
               
                TempData["errorRes"] = "FCY already maintained";
                return View(fcy_Branch);
            }
            if (ModelState.IsValid)
            {
                fcy_Branch.CreatedBy = Session["UserName"].ToString();
                fcy_Branch.District = Session["District"].ToString();
               
                fcy_Branch.CreatedDate = DateTime.Now;
                db.Fcy_Branch.Add(fcy_Branch);
                db.SaveChanges();

                TempData["successRes"] = "Saved successfully!";

                ModelState.Clear();
                fcy_Branch = new Fcy_Branch(); // Reinitialize model
                return View();
            }

            return View(fcy_Branch);
        }

        // GET: Fcy_Branch/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Fcy_Branch fcy_Branch = db.Fcy_Branch.Find(id);
            if (fcy_Branch == null)
            {
                return HttpNotFound();
            }
            return View(fcy_Branch);
        }

        // POST: Fcy_Branch/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,FcyTargetAmount,Branch,CreatedBy,CreatedDate")] Fcy_Branch fcy_Branch)
        {
            if (ModelState.IsValid)
            {
                db.Entry(fcy_Branch).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(fcy_Branch);
        }

        // GET: Fcy_Branch/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Fcy_Branch fcy_Branch = db.Fcy_Branch.Find(id);
            if (fcy_Branch == null)
            {
                return HttpNotFound();
            }
            return View(fcy_Branch);
        }

        // POST: Fcy_Branch/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Fcy_Branch fcy_Branch = db.Fcy_Branch.Find(id);
            db.Fcy_Branch.Remove(fcy_Branch);
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
