using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class AccountMappingsController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        SoapServiceHelper soapServiceHelper = new SoapServiceHelper();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();
        // GET: AccountMappings
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var accounts = db.AccountMappings.Where(t => t.UserName == UserName).ToList();
                if (accounts == null)
                {
                    accounts = new List<AccountMapping>();
                }
                return View(accounts);
            }
        }

        // GET: AccountMappings/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountMapping accountMapping = db.AccountMappings.Find(id);
            if (accountMapping == null)
            {
                return HttpNotFound();
            }
            return View(accountMapping);
        }

        // GET: AccountMappings/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountMappings/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AccountMapping accountMapping, string submit)
        {
            accountMapping.AccountNumber = accountMapping.AccountNumber?.Trim();
            // Validate input model
            if (accountMapping == null || string.IsNullOrEmpty(accountMapping.AccountNumber))
            {
                TempData["ErrorMessage"] = accountMapping == null ? "Invalid transaction data." : "Account number is required.";
                return View(accountMapping);
            }

            // Validate input model
            if (accountMapping.AccountNumber.Length < 5)
            {
                TempData["ErrorMessage"] = "The Minimum Length for Account Number is 5";
                return View(accountMapping);
            }

            if (!string.IsNullOrEmpty(accountMapping.AccountNumber) && submit == "Validate")
            {
                try
                {
                    // Fetch and parse account balance
                    var balanceResponse = soapServiceHelper.GetAccountBalance(accountMapping.AccountNumber);
                    if (string.IsNullOrEmpty(balanceResponse))
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account balance.";
                        return View(accountMapping);
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
                        return View(accountMapping);
                    }

                    // Extract account details
                    var accountNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/ACCTBALINFOType/ns2:gACCTBALINFODetailType/ns2:mACCTBALINFODetailType", nsManager);
                    if (accountNode == null)
                    {
                        TempData["ErrorMessage"] = "Account details not found in the response.";
                        return View(accountMapping);
                    }

                    var accountNo = accountNode.SelectSingleNode("ns2:AcctNo", nsManager)?.InnerText ?? "Account Number Not Found";
                    var accountName = accountNode.SelectSingleNode("ns2:Name", nsManager)?.InnerText ?? "Account Name Not Found";
                    var workingBalance = accountNode.SelectSingleNode("ns2:WorkingBal", nsManager)?.InnerText.Replace(",", "") ?? "Working Balance Not Found";


                    //ViewBag.AccountHolder = "Hailemariam Kebede";
                    //ViewBag.CurrentBalance = 5000;
                    // Store account details in ViewBag
                    ViewBag.AccountHolder = accountName;
                    ViewBag.CurrentBalance = workingBalance;

                    // Fetch previous balance (string JSON)
                    string json = callbyReference.GetBeginningBalance(accountMapping.AccountNumber);

                    decimal Beginning = 0;
                    bool hasValidOpeningBalance = false;

                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            JObject obj = JObject.Parse(json);

                            if (obj["lcyClosingBalance"] != null &&
                                decimal.TryParse(obj["lcyClosingBalance"].ToString(), out decimal parsedValue))
                            {
                                Beginning = parsedValue;
                                hasValidOpeningBalance = true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"Closing balance missing or invalid for account {accountMapping.AccountNumber}");
                            }
                        }
                        catch (JsonException ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"JSON parse error for account {accountMapping.AccountNumber}: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"GetBeginningBalance returned null/empty for account {accountMapping.AccountNumber}");
                    }


                    // Now decide what to show in UI
                    if (hasValidOpeningBalance)
                    {
                        ViewBag.BeginningBalance = Beginning;
                        ViewBag.BeginningBalanceStatus = "success"; // optional - for UI coloring
                    }
                    else
                    {
                        ViewBag.BeginningBalance = null;                  // or "-" or string.Empty
                        ViewBag.BeginningBalanceStatus = "error";         // optional
                        ViewBag.BeginningBalanceError = "Unable to load Beginning balance"; // optional message
                    }


                    return View(accountMapping);

                }
                catch (XmlException ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                    return View(accountMapping);
                }

            }

            //// Check for account number duplication
            if (!string.IsNullOrEmpty(accountMapping.AccountNumber))
            {
                var existingAccount = db.AccountMappings
                    .Where(t => t.AccountNumber != null && t.AccountNumber == accountMapping.AccountNumber)
                    .ToList();
                if (existingAccount.Any())
                {
                    var createdBy = existingAccount.FirstOrDefault()?.UserName ?? string.Empty;
                    var fullName = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.FullName ?? string.Empty;
                    var phoneNumber = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.PhoneNumber ?? string.Empty;
                    var emailAddress = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.MailAdress ?? string.Empty;
                    var process = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.Process ?? string.Empty;
                    var district = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.District ?? string.Empty;
                    var branch = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.Branch ?? string.Empty;

                    TempData["ErrorMessage"] =
                        $"<b>Registered By:</b> {fullName}<br/>" +
                        $"<b>Phone:</b> {phoneNumber}<br/>" +
                        $"<b>Email:</b> {emailAddress}<br/>" +
                        $"<b>District:</b> {district}<br/>" +
                        $"<b>Branch:</b> {branch}";

                    return View(accountMapping);
                }
            }

            // Check for account duplication PER USER
            //if (!string.IsNullOrEmpty(accountMapping.AccountNumber))
            //{
            //    var currentUserName = Session["UserName"]?.ToString();

            //    var alreadyMappedByUser = db.AccountMappings
            //        .Any(am =>
            //            am.AccountNumber == accountMapping.AccountNumber &&
            //            am.UserName == currentUserName);

            //    if (alreadyMappedByUser)
            //    {
            //        TempData["ErrorMessage"] = "This account is already registered by you.";
            //        return View(accountMapping);
            //    }
            //}


            if (submit == "Map")
            {
                var userName = Session["UserName"]?.ToString();
                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "User");
                }
                if (accountMapping == null)
                {
                    TempData["ErrorMessage"] = "Data is missing.";
                    return View(accountMapping);
                }

                accountMapping.UserName = userName;
                accountMapping.District = user.District;
                accountMapping.Branch = user.Branch;


                // Validate input model
                if (string.IsNullOrEmpty(accountMapping.AccountHolder) || accountMapping.CurrentBalance == null || accountMapping.BegginingBalance == null || string.IsNullOrEmpty(accountMapping.Branch) || string.IsNullOrEmpty(accountMapping.District))
                {
                    TempData["ErrorMessage"] = "Account Holder Name is Required. Please Validate it first";
                    return View(accountMapping);
                }

                accountMapping.CreatedDate = DateTime.Now;

                db.AccountMappings.Add(accountMapping);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Account Mapped successfully!";
            }

            return View(accountMapping);
        }

        // GET: AccountMappings/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountMapping accountMapping = db.AccountMappings.Find(id);
            if (accountMapping == null)
            {
                return HttpNotFound();
            }
            return View(accountMapping);
        }

        // POST: AccountMappings/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Acc_ID,AccountNumber,AccountHolder,BegginingBalance,CurrentBalance,District,Branch,UserName,CreatedDate")] AccountMapping accountMapping)
        {
            if (ModelState.IsValid)
            {
                db.Entry(accountMapping).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(accountMapping);
        }

        // GET: AccountMappings/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AccountMapping accountMapping = db.AccountMappings.Find(id);
            if (accountMapping == null)
            {
                return HttpNotFound();
            }
            return View(accountMapping);
        }

        // POST: AccountMappings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AccountMapping accountMapping = db.AccountMappings.Find(id);
            db.AccountMappings.Remove(accountMapping);
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
