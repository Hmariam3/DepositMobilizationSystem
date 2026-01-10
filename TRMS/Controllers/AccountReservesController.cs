using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TRMS.Models;
using TRMS.Security;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TRMS.Controllers
{
    public class AccountReservesController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        SoapServiceHelper soapServiceHelper = new SoapServiceHelper();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();

        // GET: AccountReserves
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var accounts = db.AccountReserves.Where(t => t.UserName == UserName).ToList();
                if (accounts == null)
                {
                    accounts = new List<AccountReserve>();
                }
                return View(accounts);
            }
 
        }

        // GET: AccountReserves/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountReserve accountReserve = db.AccountReserves.Find(id);
            if (accountReserve == null)
            {
                return HttpNotFound();
            }
            return View(accountReserve);
        }

        // GET: AccountReserves/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountReserves/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AccountReserve accountReserve, string submit)
        {
            accountReserve.AccountNumber = accountReserve.AccountNumber?.Trim();

            // Validate input model
            if (accountReserve == null || string.IsNullOrEmpty(accountReserve.AccountNumber))
            {
                TempData["ErrorMessage"] = accountReserve == null ? "Invalid transaction data." : "Account number is required.";
                return View(accountReserve);
            }


            // Validate input model
            if (accountReserve.AccountNumber.Length < 5)
            {
                TempData["ErrorMessage"] = "The Minimum Length for Account Number is 5";
                return View(accountReserve);
            }

            if (!string.IsNullOrEmpty(accountReserve.AccountNumber) && submit == "Validate")
            {
                try
                {
                    // Fetch and parse account balance
                    var balanceResponse = soapServiceHelper.GetAccountBalance(accountReserve.AccountNumber);
                    if (string.IsNullOrEmpty(balanceResponse))
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account balance.";
                        return View(accountReserve);
                    }

                    var balanceDoc = new XmlDocument();
                    balanceDoc.LoadXml(balanceResponse);

                    var nsManager = new XmlNamespaceManager(balanceDoc.NameTable);
                    nsManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                    nsManager.AddNamespace("ns2", "http://temenos.com/ACCTBALINFO");
                    nsManager.AddNamespace("ns4", "http://temenos.com/TWSTXNDETAIL");

                    // Check balance API status
                    var statusNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/Status/successIndicator", nsManager);
                    if (statusNode?.InnerText != "Success")
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account details or invalid account number.";
                        return View(accountReserve);
                    }

                    // Extract account details
                    var accountNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/ACCTBALINFOType/ns2:gACCTBALINFODetailType/ns2:mACCTBALINFODetailType", nsManager);
                    if (accountNode == null)
                    {
                        TempData["ErrorMessage"] = "Account details not found in the response.";
                        return View(accountReserve);
                    }

                    var accountNo = accountNode.SelectSingleNode("ns2:AcctNo", nsManager)?.InnerText ?? "Account Number Not Found";
                    var accountName = accountNode.SelectSingleNode("ns2:Name", nsManager)?.InnerText ?? "Account Name Not Found";
                    var workingBalance = accountNode.SelectSingleNode("ns2:WorkingBal", nsManager)?.InnerText.Replace(",", "") ?? "Working Balance Not Found";

                    // Store account details in ViewBag
                    ViewBag.AccountHolder = accountName;
                    ViewBag.AccountBalance = workingBalance;

                    return View(accountReserve);

                }
                catch (XmlException ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                    return View(accountReserve);
                }

            }

            //// Check for account number duplication
            //if (!string.IsNullOrEmpty(accountReserve.AccountNumber))
            //{
            //    var existingAccount = db.AccountReserves
            //        .Where(t => t.AccountNumber != null && t.AccountNumber == accountReserve.AccountNumber)
            //        .ToList();
            //    if (existingAccount.Any())
            //    {
            //        var createdBy = existingAccount.FirstOrDefault()?.UserName ?? string.Empty;
            //        var fullName = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.FullName ?? string.Empty;
            //        var phoneNumber = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.PhoneNumber ?? string.Empty;
            //        var emailAddress = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.MailAdress ?? string.Empty;
            //        var process = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.Process ?? string.Empty;
            //        var district = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.District ?? string.Empty;
            //        var branch = db.Users
            //            .Where(u => u.UserName == createdBy)
            //            .FirstOrDefault()?.Branch ?? string.Empty;

            //        TempData["ErrorMessage"] =
            //            $"<b>Registered By:</b> {fullName}<br/>" +
            //            $"<b>Phone:</b> {phoneNumber}<br/>" +
            //            $"<b>Email:</b> {emailAddress}<br/>" +
            //            $"<b>Process:</b> {process}<br/>" +
            //            $"<b>District/SubProcess:</b> {district}<br/>" +
            //            $"<b>Branch/Team:</b> {branch}";

            //        return View(accountReserve);
            //    }
            //}
            // Check for account duplication PER USER
            if (!string.IsNullOrEmpty(accountReserve.AccountNumber))
            {
                var currentUserName = Session["UserName"]?.ToString();

                var alreadyReservedByUser = db.AccountReserves
                    .Any(ar =>
                        ar.AccountNumber == accountReserve.AccountNumber &&
                        ar.UserName == currentUserName);

                if (alreadyReservedByUser)
                {
                    TempData["ErrorMessage"] = "This account is already reserved by you.";
                    return View(accountReserve);
                }
            }

            if (submit == "Reserve")
            {
                var userName = Session["UserName"]?.ToString();
                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "User");
                }
                if (accountReserve == null)
                {
                    TempData["ErrorMessage"] = "Data is missing.";
                    return View(accountReserve);
                }
                // Validate input model
                if (string.IsNullOrEmpty(accountReserve.AccountHolder) || accountReserve.AccountBalance == null)
                {
                    TempData["ErrorMessage"] = "Account Holder Name is Required. Please Validate it first";
                    return View(accountReserve);
                }
                accountReserve.UserName = userName;
                accountReserve.CreatedDate = DateTime.Now;

                db.AccountReserves.Add(accountReserve);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Account Reserved successfully!";
            }

            return View(accountReserve);
        }

        // GET: AccountReserves/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountReserve accountReserve = db.AccountReserves.Find(id);
            if (accountReserve == null)
            {
                return HttpNotFound();
            }
            return View(accountReserve);
        }

        // POST: AccountReserves/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ACC_ID,AccountNumber,AccountHolder,AccountBalance,UserName,CreatedDate")] AccountReserve accountReserve)
        {
            if (ModelState.IsValid)
            {
                db.Entry(accountReserve).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(accountReserve);
        }

        // GET: AccountReserves/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountReserve accountReserve = db.AccountReserves.Find(id);
            if (accountReserve == null)
            {
                return HttpNotFound();
            }
            return View(accountReserve);
        }

        // POST: AccountReserves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AccountReserve accountReserve = db.AccountReserves.Find(id);
            db.AccountReserves.Remove(accountReserve);
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
