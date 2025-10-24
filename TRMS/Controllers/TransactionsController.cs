using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class TransactionsController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // GET: Transactions
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var transactions = db.Transactions.Where(t => t.UserName == UserName).ToList();
                if (transactions == null)
                {
                    transactions = new List<Transaction>();
                }
                return View(transactions);
            }
        }

        // GET: Transactions/Details/5
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }

            // Construct the file path
            if (!string.IsNullOrEmpty(transaction.Slip))
            {
                string filePath = Path.Combine(Server.MapPath("~/Uploads/"), transaction.Slip);
                if (System.IO.File.Exists(filePath))
                {
                    ViewBag.SlipPath = Url.Content("~/Uploads/" + transaction.Slip);
                }
                else
                {
                    ViewBag.SlipPath = null; // Handle missing files gracefully
                }
            }

            return View(transaction);
        }

        // GET: Transactions/Create
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        public ActionResult Create(Transaction transaction, HttpPostedFileBase postedFile)
        {

            if (transaction == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(transaction);
            }
            if (transaction.Particpant_Number < 0)
            {
                TempData["errorRes"] = "Please enter greater than 0!";
                return View(transaction);
            }
            if (transaction.ReferenceNumber != null)
            {
                transaction.ReferenceNumber = transaction.ReferenceNumber.Trim();
            }
                // check it avilabe with similar 
            var checktype = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber).ToList().FirstOrDefault();
            if(checktype!=null)
            {
                if(checktype.Collected_Type.Trim()!=transaction.Collected_Type)
                {
                    TempData["errorRes"] = "Reference Number Is Registed As " + " "+ checktype.Collected_Type.Trim() +" "+ " Please Select" +" "+ checktype.Collected_Type.Trim();
                    return View(transaction);
                }
            }

            var indivisualrefernce = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type == "Individual".Trim()).ToList();
            if (indivisualrefernce.Count()>=1)
            {
                // Ensure transaction is not null before accessing ReferenceNumber
                string createdBy = db.Transactions
                    .Where(u => u.ReferenceNumber == transaction.ReferenceNumber)
                    .FirstOrDefault()?.Created_By ?? string.Empty;  // Default to empty string if null

                string phoneNumber = db.Users
                    .Where(u => u.UserName == createdBy)
                    .FirstOrDefault()?.PhoneNumber ?? string.Empty;  // Default to empty string if null

                TempData["errorRes"] = "Transaction reference duplication!" +"Registerd BY "+ createdBy +"  "+ "And  Phone Number is "+ phoneNumber;
                return View(transaction);
            }

            //for joint the same user
            string userName = Session["UserName"].ToString();
            var jointreferncebyThesame = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.UserName== userName && t.Collected_Type == "Joint".Trim()).ToList();
            if (jointreferncebyThesame.Count() >= 1)
            {
                // Ensure transaction is not null before accessing ReferenceNumber
                string createdBy = db.Transactions
                    .Where(u => u.ReferenceNumber == transaction.ReferenceNumber)
                    .FirstOrDefault()?.Created_By ?? string.Empty;  // Default to empty string if null

                string phoneNumber = db.Users
                    .Where(u => u.UserName == createdBy)
                    .FirstOrDefault()?.PhoneNumber ?? string.Empty;  // Default to empty string if null

                TempData["errorRes"] = "Transaction reference duplication!" + "Registerd BY " + createdBy + "  " + "And  Phone Number is " + phoneNumber;
                return View(transaction);
            }


            if (transaction.Collected_Type == "Individual".Trim())
            {
                transaction.Particpant_Number = 0;
            }



            // Validate Transaction Reference Number
            if (string.IsNullOrWhiteSpace(transaction.ReferenceNumber))
            {
                TempData["errorRes"] = "Please Enter Transaction Reference.";
                return View(transaction);
            }
            var refernceforJoint = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint".Trim()).FirstOrDefault();

            if (refernceforJoint!=null)
            {
              int ParticipantNumber = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint" && t.Particpant_Number != 0).FirstOrDefault()?.Particpant_Number ?? 0;
                if (ParticipantNumber == 0)
                {

                    // Ensure transaction is not null before accessing ReferenceNumber
                    string createdBy = db.Transactions
                        .Where(u => u.ReferenceNumber == transaction.ReferenceNumber)
                        .FirstOrDefault()?.Created_By ?? string.Empty;  // Default to empty string if null

                    string phoneNumber = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.PhoneNumber ?? string.Empty;  // Default to empty string if null

                    TempData["errorRes"] = "The transaction reference has reached the maximum joint limit. " + " Registerd BY " + createdBy + "  " + "And  Phone Number is " + phoneNumber;
                    return View(transaction);
                }
                else
                {
                    var jointlimit = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint" && t.Particpant_Number != 0).FirstOrDefault();
                    if (jointlimit.Particpant_Number > 0)
                    {
                        
                        jointlimit.Particpant_Number = jointlimit.Particpant_Number - 1;
                        db.Entry(jointlimit).State = EntityState.Modified;
                        db.SaveChanges();
                        transaction.Particpant_Number = 0;
                    }
                }
                
            }
            // Validate Transaction Amount
            // Validate Transaction Amount
            if (transaction.TransactionAmount <= 0 || transaction.TransactionAmount > 2000000000)
            {
                TempData["errorRes"] = "Transaction Amount must be greater than zero and less than or equal to 2 Billion.";
                return View(transaction);
            }

            string filePath = string.Empty;
            string fileName = string.Empty;

            // Validate File Upload
            if (postedFile != null)
            {
                string path = Server.MapPath("~/Uploads/");

                // Ensure Directory Exists
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Validate File Extension
                string extension = Path.GetExtension(postedFile.FileName);
                string[] allowedExtensions = { ".jpg", ".png", ".pdf", ".docx" };

                if (!allowedExtensions.Contains(extension.ToLower()))
                {
                    TempData["errorRes"] = "Invalid file type! Allowed: .jpg, .png, .pdf, .docx";
                    return View(transaction);
                }

               

                // Generate Unique File Name
                fileName = $"{transaction.ReferenceNumber}_{Path.GetFileName(postedFile.FileName)}";
                filePath = Path.Combine(path, fileName);

                // Save File
                try
                {
                    postedFile.SaveAs(filePath);
                }
                catch (Exception ex)
                {
                    TempData["errorRes"] = "File upload failed: " + ex.Message;
                    return View(transaction);
                }
            }

            // Validate Session Values
            if (Session["UserName"] == null || Session["UserHomeBranch"] == null)
            {
                TempData["errorRes"] = "Session expired! Please log in again.";
                return RedirectToAction("Login"); // Redirect to login if session is invalid
            }

            // Save Transaction if Model is Valid
            try
            {
                if (ModelState.IsValid)
                {
                    transaction.Created_By = Session["UserName"].ToString();

                    transaction.Slip = fileName;
                    transaction.UserName = Session["UserName"].ToString();
                    transaction.UserBranch = Session["UserHomeBranch"].ToString();
                    var user = db.Users.FirstOrDefault(u => u.UserName == transaction.UserName);
                    transaction.Process = user?.Process ?? string.Empty; // Assign empty string if user or Process is null

                    var Jointcheck = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint".Trim()).FirstOrDefault();
                    if (Jointcheck == null)
                    {
                        transaction.Total_Particpant = transaction.Particpant_Number;
                    }
                    db.Transactions.Add(transaction);
                    db.SaveChanges();

                    //if (Jointcheck == null)
                    //{
                    //    if (transaction.Collected_Type.Trim() == "Joint".Trim())
                    //    {
                    //        var jointlimit = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint" && t.Particpant_Number != 0).FirstOrDefault();
                    //        jointlimit.Particpant_Number = jointlimit.Particpant_Number - 1;
                    //        db.Entry(jointlimit).State = EntityState.Modified;
                    //        db.SaveChanges();
                    //    }
                    //}

                    TempData["successRes"] = "Transaction saved successfully!";

                    ModelState.Clear();
                    transaction = new Transaction(); // Reinitialize model
                    return View();
                }
            }
            catch (DbEntityValidationException ex)
            {
                List<string> errorMessages = new List<string>();

                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }

                // Store errors in TempData
                TempData["errorRes"] = string.Join("<br/>", errorMessages);
            }

            return View();
        }

        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // GET: Transactions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Transaction transaction, HttpPostedFileBase postedFile)
        {


            if (transaction == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(transaction);
            }

            // Validate Transaction Reference Number
            if (string.IsNullOrWhiteSpace(transaction.ReferenceNumber))
            {
                TempData["errorRes"] = "Please Enter Transaction Reference.";
                return View(transaction);
            }

            // Validate Transaction Amount
            if (transaction.TransactionAmount <= 0)
            {
                TempData["errorRes"] = "Transaction Amount must be greater than zero.";
                return View(transaction);
            }

            string filePath = string.Empty;
            string fileName = string.Empty;

            // Validate File Upload
            if (postedFile != null)
            {
                string path = Server.MapPath("~/Uploads/");

                // Ensure Directory Exists
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Validate File Extension
                string extension = Path.GetExtension(postedFile.FileName);
                string[] allowedExtensions = { ".jpg", ".png", ".pdf", ".docx" };

                if (!allowedExtensions.Contains(extension.ToLower()))
                {
                    TempData["errorRes"] = "Invalid file type! Allowed: .jpg, .png, .pdf, .docx";
                    return View(transaction);
                }



                // Generate Unique File Name
                fileName = $"{transaction.ReferenceNumber}_{Path.GetFileName(postedFile.FileName)}";
                filePath = Path.Combine(path, fileName);
                // Check if file exists, delete it
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }


                // Save File
                try
                {
                    postedFile.SaveAs(filePath);
                }
                catch (Exception ex)
                {
                    TempData["errorRes"] = "File upload failed: " + ex.Message;
                    return View(transaction);
                }
            }

            // Validate Session Values
            if (Session["UserName"] == null || Session["UserHomeBranch"] == null)
            {
                TempData["errorRes"] = "Session expired! Please log in again.";
                return RedirectToAction("Login"); // Redirect to login if session is invalid
            }

            // Save Transaction if Model is Valid
         
                if (ModelState.IsValid)
            {
                Transaction transactionNew = db.Transactions.Where(t => t.Id == transaction.Id).FirstOrDefault();
                transactionNew.Slip = fileName;
                transactionNew.UserName = Session["UserName"].ToString();
                transactionNew.UserBranch = Session["UserHomeBranch"].ToString();
                transactionNew.TransactionAmount = transaction.TransactionAmount;
                transactionNew.CreditAccount = transaction.CreditAccount;
                transactionNew.Channal = transaction.Channal;
                transactionNew.TransactionType = transaction.TransactionType;
                transactionNew.TransctionBranch= transaction.TransctionBranch;
                transactionNew.Remark = transaction.Remark;
                db.Entry(transactionNew).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Transaction transaction = db.Transactions.Find(id);
            db.Transactions.Remove(transaction);
            db.SaveChanges();
            // Generate Unique File Name
            string filePath = string.Empty;
            string fileName = string.Empty;

            if (transaction.Slip != null || transaction.Slip != "")
            {
                fileName = transaction.Slip;

                string path = Server.MapPath("~/Uploads/");
                filePath = Path.Combine(path, fileName);
                // Check if file exists, delete it
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            //update the joint users 
            var jointlimits = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint").ToList();
            if (jointlimits.Count() >=1)
            {
                var jointlimit = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint" && t.Particpant_Number != 0).FirstOrDefault();
                if (jointlimit != null)
                {
                    if (jointlimit.Particpant_Number > 0)
                    {

                        jointlimit.Particpant_Number = jointlimit.Particpant_Number + 1;
                        db.Entry(jointlimit).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                   
                }
                else
                {
                    var jointlimit1 = db.Transactions.Where(t => t.ReferenceNumber == transaction.ReferenceNumber && t.Collected_Type.Trim() == "Joint" && t.Total_Particpant != null).FirstOrDefault();
                    if (jointlimit1.Particpant_Number ==0)
                    {
                        jointlimit1.Particpant_Number = jointlimit1.Particpant_Number + 1;
                        db.Entry(jointlimit1).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                }
            }

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
