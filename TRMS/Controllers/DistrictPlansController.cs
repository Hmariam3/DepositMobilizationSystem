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
    public class DistrictPlansController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // GET: DistrictPlans
        public ActionResult Index()
        {
            string role = Session["UserRole"].ToString();
            if (role == "District")
            {
                string district = Session["UserHomeBranch"].ToString();
                return View(db.DistrictPlans.Where(d => d.DistrictName.Trim() == district.Trim()).ToList());
            }
            else if(role == "Admin")
            {
               
                return View(db.DistrictPlans.ToList());
            }
            return View();
        }



        public ActionResult BranchInDistrict()
        {
           string district = Session["UserHomeBranch"].ToString();
           string role = Session["UserRole"].ToString();
            var users = new List<User>();
            if (role == "District")
            {
                users=db.Users.Where(u => u.Process != null && u.Process.Trim() == district.Trim()).ToList();
                return View(users);
            }
            else if(role == "Admin")
            {
                users = db.Users.ToList();
                return View(users);
            }
            return View(users);
        }
        public ActionResult AddBranchTarget(int ? brid)
        {
            if (brid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User branch = db.Users.Find(brid);
            BranchTarget branchTarget = new BranchTarget();
            if (branch == null)
            {
                return HttpNotFound();
            }


            else
            {
                var district = db.DistrictPlans.Where(d => d.DistrictName.Trim() == branch.Process.Trim()).ToList();
                if (district.Count() < 1)
                {
                    TempData["errorRes"] = "Please set District Target  First!";
                    return View(branchTarget);
                }
                else
                {
                    int distID = db.DistrictPlans.Where(d => d.DistrictName.Trim() == branch.Process.Trim()).FirstOrDefault().DId;
                    branchTarget.DID = distID;
                    branchTarget.District = branch.Process;
                    branchTarget.BranchName = branch.Branch;
                }
                
            }
            return View(branchTarget);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBranchTarget(BranchTarget branchTarget)
        {
            var districtPlan = db.DistrictPlans.Where(d => d.DistrictName.Trim() == branchTarget.District.Trim()).ToList();
            if (districtPlan.Count()< 1)
            {
                TempData["errorRes"] = "Please set District Target  First!";
                return View(branchTarget);
            }


            if (branchTarget == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(branchTarget);
            }


            if (branchTarget.District == null)
            {
                TempData["errorRes"] = "District Not Null!";
                return View(branchTarget);
            }
            var planExist = db.BranchTargets.Where(t => t.BranchName.Trim() == branchTarget.BranchName.Trim()).ToList();
            if (planExist.Count() >= 1)
            {

                TempData["errorRes"] = "Target already maintained";
                return View(branchTarget);
            }

            if (ModelState.IsValid)
            {
                branchTarget.CreatedBy = Session["UserName"].ToString();
                branchTarget.CreatedDate = DateTime.Now;
                db.BranchTargets.Add(branchTarget);
                db.SaveChanges();
                TempData["successRes"] = "Saved successfully!";
                ModelState.Clear();
                branchTarget = new BranchTarget(); // Reinitialize model
            }

            return RedirectToAction("branchTargetList");
        }
        public ActionResult branchTargetList()
        {
            string district = Session["UserHomeBranch"].ToString();
            return View(db.BranchTargets.Where(b=>b.District.Trim()== district.Trim()).ToList());
        }
        // GET: DistrictPlans/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DistrictPlan districtPlan = db.DistrictPlans.Find(id);
            if (districtPlan == null)
            {
                return HttpNotFound();
            }
            return View(districtPlan);
        }

        // GET: DistrictPlans/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DistrictPlans/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DistrictPlan districtPlan)
        {
            if (districtPlan == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(districtPlan);
            }
            if (districtPlan.DistrictName == null)
            {
                TempData["errorRes"] = "District Not Null!";
                return View(districtPlan);
            }
            var planExist = db.DistrictPlans.Where(t => t.DistrictName.Trim() == districtPlan.DistrictName.Trim()).ToList();
            if (planExist.Count() >= 1)
            {

                TempData["errorRes"] = "Target already maintained";
                return View(districtPlan);
            }

            if (ModelState.IsValid)
            {
                districtPlan.CreatedBy = Session["UserName"].ToString();
                districtPlan.CreatedDate = DateTime.Now;
                db.DistrictPlans.Add(districtPlan);
                db.SaveChanges();
                TempData["successRes"] = "Saved successfully!";

                ModelState.Clear();
                districtPlan = new DistrictPlan(); // Reinitialize model
            }

            return View(districtPlan);
        }

        // GET: DistrictPlans/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DistrictPlan districtPlan = db.DistrictPlans.Find(id);
            if (districtPlan == null)
            {
                return HttpNotFound();
            }
            return View(districtPlan);
        }

        // POST: DistrictPlans/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DId,Deposit,FCY,merchant,CreatedBy,CreatedDate")] DistrictPlan districtPlan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(districtPlan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(districtPlan);
        }

        // GET: DistrictPlans/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DistrictPlan districtPlan = db.DistrictPlans.Find(id);
            if (districtPlan == null)
            {
                return HttpNotFound();
            }
            return View(districtPlan);
        }

        // POST: DistrictPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DistrictPlan districtPlan = db.DistrictPlans.Find(id);
            db.DistrictPlans.Remove(districtPlan);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult DeleteBranchtarget(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BranchTarget branchTarget = db.BranchTargets.Find(id);
            if (branchTarget == null)
            {
                return HttpNotFound();
            }
            return View(branchTarget);
        }
        [HttpPost, ActionName("DeleteBranchtarget")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBranchtargetConfirmed(int id)
        {
            BranchTarget branchTarget = db.BranchTargets.Find(id);
            db.BranchTargets.Remove(branchTarget);
            db.SaveChanges();
            return RedirectToAction("branchTargetList");
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
