using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class DepositFCiesController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
       callAccountwithRefrenece callbyRefrence = new callAccountwithRefrenece();
        // GET: DepositFCies
        public ActionResult Index()
        {
            var depositFCies = db.DepositFCies.Include(d => d.User1);
            return View(depositFCies.ToList());
        }

        // GET: DepositFCies/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositFCY depositFCY = db.DepositFCies.Find(id);
            if (depositFCY == null)
            {
                return HttpNotFound();
            }
            return View(depositFCY);
        }

        // GET: DepositFCies/Create
        public ActionResult Create()
        {
         
            return View();
        }

        // POST: DepositFCies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DepositFCY depositFCY, string submit)
        {
            if (depositFCY == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(depositFCY);
            }
            if (depositFCY.RefernceNumber == null)
            {
                TempData["errorRes"] = "Please Refernce  Number!";
                return View(depositFCY);
            }
            var  arefreneceExist = db.DepositFCies.Where(t => t.RefernceNumber.Trim() == depositFCY.RefernceNumber.Trim()).ToList();
            if (arefreneceExist.Count() >= 1)
            {
                // Ensure transaction is not null before accessing ReferenceNumber
                string createdBy = db.DepositFCies
                    .Where(u => u.RefernceNumber == depositFCY.RefernceNumber)
                    .FirstOrDefault()?.User ?? string.Empty;  // Default to empty string if null

                string phoneNumber = db.Users
                    .Where(u => u.UserName == createdBy)
                    .FirstOrDefault()?.PhoneNumber ?? string.Empty;  // Default to empty string if null

                TempData["ErrorMessage"] = "Refernce Number  duplication!" + "Registerd BY " + createdBy + "  " + "And  Phone Number is " + phoneNumber;
                return View(depositFCY);
            }
         
            if (submit.Equals("Validate"))
            {

                // Fetch and parse transaction details by reference
                var transactionResponse = callbyRefrence.GetAccountinformationByrefrence(depositFCY.RefernceNumber);
                if (string.IsNullOrEmpty(transactionResponse))
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Failed to retrieve transaction details or invalid reference number.";
                    return View(depositFCY);
                }


                var transactionDoc = new XmlDocument();
                transactionDoc.LoadXml(transactionResponse);

                var transactionNsManager = new XmlNamespaceManager(transactionDoc.NameTable);
                transactionNsManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                transactionNsManager.AddNamespace("ns3", "http://temenos.com/FTTTTXNDETAIL");
                transactionNsManager.AddNamespace("ns4", "http://temenos.com/TWSTXNDETAIL");

                // ✅ Check transaction API status
                var transactionStatusNode = transactionDoc.SelectSingleNode("//S:Body/ns4:CBOTXNDETAILResponse/Status/successIndicator", transactionNsManager);
                if (transactionStatusNode?.InnerText != "Success")
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Transaction reference not found or invalid.";
                    return View(depositFCY);
                }

                // ✅ Select all transaction detail nodes
                var transactionNodes = transactionDoc.SelectNodes("//S:Body/ns4:CBOTXNDETAILResponse/FTTTTXNDETAILType/ns3:gFTTTTXNDETAILDetailType/ns3:mFTTTTXNDETAILDetailType", transactionNsManager);

                if (transactionNodes == null || transactionNodes.Count == 0)
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "No transaction details found in the response.";
                    return View(depositFCY);
                }

                // ✅ Extract all transaction details
                var transactionDetails = new List<dynamic>();
                string debitAccount = "";
                string creditAccount = "";
                string debitAmount = "";
                string creditAmount = "";
                string txnRef = "";
                string currency = "";
                string txnDate = "";

                foreach (XmlNode txnNode in transactionNodes)
                {
                    var refValue = txnNode.SelectSingleNode("ns3:TXNREF", transactionNsManager)?.InnerText ?? "";
                    var marker = txnNode.SelectSingleNode("ns3:DRCRMARKER", transactionNsManager)?.InnerText ?? "";
                    var account = txnNode.SelectSingleNode("ns3:Account", transactionNsManager)?.InnerText ?? "";
                    var amount = txnNode.SelectSingleNode("ns3:Amount", transactionNsManager)?.InnerText ?? "";
                    var curr = txnNode.SelectSingleNode("ns3:Currency", transactionNsManager)?.InnerText ?? "";
                    var date = txnNode.SelectSingleNode("ns3:TXNDATE", transactionNsManager)?.InnerText ?? "";

                    transactionDetails.Add(new
                    {
                        TXNREF = refValue,
                        DRCRMARKER = marker,
                        Account = account,
                        Amount = amount,
                        Currency = curr,
                        TXNDATE = date
                    });

                    // Identify debit and credit accounts
                    if (marker == "DEBIT")
                    {
                        debitAccount = account;
                        debitAmount = amount;
                    }
                    else if (marker == "CREDIT")
                    {
                        creditAccount = account;
                        creditAmount = amount;
                    }

                    // Common fields
                    txnRef = refValue;
                    currency = curr;
                    txnDate = date;
                }

                // ✅ Example: display or pass to View
                ViewBag.TransactionDetails = transactionDetails;
                ViewBag.DebitAccount = debitAccount;
                ViewBag.CreditAccount = creditAccount;
                ViewBag.DebitAmount = debitAmount;
                ViewBag.CreditAmount = creditAmount;
                ViewBag.TXNREF = txnRef;
                ViewBag.Currency = currency;
                ViewBag.TXNDATE = txnDate;


                // Optionally, pass to your View
                ViewBag.TransactionDetails = transactionDetails;


                // Validate account number match
                if (depositFCY.AccountNumber != creditAccount)
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                    return View(depositFCY);
                }

                // Parse transaction amount
                var amountMatch = Regex.Match(debitAmount, @"\d+(\.\d+)?");
                debitAmount = amountMatch.Success ? amountMatch.Value : "Not Found";

                // Store transaction details in ViewBag
                ViewBag.ReferenceExists = true;
                ViewBag.TransactionAmount = debitAmount;
                ViewBag.Currency = currency;
                ViewBag.TransactionReference = txnRef;
                return View(depositFCY);
            }
            else
            {

                if (ModelState.IsValid)
                {
                    if (depositFCY.TransactionAmount <= 0 || depositFCY.TransactionAmount > 1000000000)
                    {
                        TempData["ErrorMessage"] = "Transaction Amount must be greater than zero and less than or equal to 1 Billion.";
                        return View(depositFCY);
                    }
                    if (depositFCY.Currecy == null)
                    {
                        TempData["ErrorMessage"] = "Please Enter   Transaction Currecy!";
                        return View(depositFCY);
                    }
                    depositFCY.User = Session["UserName"].ToString();
                    var userName = Session["UserName"]?.ToString();
                    if (!string.IsNullOrEmpty(userName))
                    {
                        var user = db.Users.FirstOrDefault(u => u.UserName == userName);
                        if (user != null)
                        {
                            depositFCY.UserID = user.ID;
                        }
                        else
                        {
                            // Handle case where user is not found
                            // e.g., throw an error, redirect, or set a default value
                        }
                    }
                    else
                    {
                        return RedirectToAction("login", "User");
                    }
                    depositFCY.Process = Session["Process"]?.ToString() ?? string.Empty;
                    depositFCY.District = Session["District"].ToString();
                    depositFCY.Branch = Session["UserHomeBranch"].ToString();
                    depositFCY.Target = 0;
                    depositFCY.CreatedDate = DateTime.Now;
                    db.DepositFCies.Add(depositFCY);
                    db.SaveChanges();

                    TempData["successRes"] = "Transaction saved successfully!";

                    ModelState.Clear();
                    depositFCY = new DepositFCY(); // Reinitialize model

                    return View();
                }
            }
            return View(depositFCY);
        }

        // GET: DepositFCies/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositFCY depositFCY = db.DepositFCies.Find(id);
            if (depositFCY == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserID = new SelectList(db.Users, "ID", "UserName", depositFCY.UserID);
            return View(depositFCY);
        }

        // POST: DepositFCies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FID,District,Branch,Target,Currecy,AccountNumber,RefernceNumber,TransactionAmount,User,CreatedDate,UserID")] DepositFCY depositFCY)
        {
            if (ModelState.IsValid)
            {
                db.Entry(depositFCY).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserID = new SelectList(db.Users, "ID", "UserName", depositFCY.UserID);
            return View(depositFCY);
        }

        // GET: DepositFCies/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositFCY depositFCY = db.DepositFCies.Find(id);
            if (depositFCY == null)
            {
                return HttpNotFound();
            }
            return View(depositFCY);
        }

        // POST: DepositFCies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DepositFCY depositFCY = db.DepositFCies.Find(id);
            db.DepositFCies.Remove(depositFCY);
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
