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
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var transactions = db.DepositFCies.Where(t => t.User == UserName).ToList();
                if (transactions == null)
                {
                    transactions = new List<DepositFCY>();
                }
                return View(transactions);
            }
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
            depositFCY.AccountNumber = depositFCY.AccountNumber?.Trim();
            depositFCY.RefernceNumber = depositFCY.RefernceNumber?.Trim();


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
            var arefreneceExist = db.DepositFCies.Where(t => t.RefernceNumber.Trim() == depositFCY.RefernceNumber.Trim()).ToList();
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

                // ✅ Extract reference type
                string referenceNumber = depositFCY.RefernceNumber?.Trim() ?? "";
                string refType = referenceNumber.Length >= 2 ? referenceNumber.Substring(0, 2).ToUpper() : "";
                bool isFT = refType == "FT";
                //bool isTT = refType == "TT";
                bool isTF = refType == "TF";


                var transactionDetails = new List<dynamic>();
                string debitAccount = "";
                string creditAccount = "";
                string amount = "";
                string txnRef = "";
                string currency = "";
                string txnDate = "";

                foreach (XmlNode txnNode in transactionNodes)
                {
                    var refValue = txnNode.SelectSingleNode("ns3:TXNREF", transactionNsManager)?.InnerText ?? "";
                    var marker = txnNode.SelectSingleNode("ns3:DRCRMARKER", transactionNsManager)?.InnerText ?? "";
                    var account = txnNode.SelectSingleNode("ns3:Account", transactionNsManager)?.InnerText ?? "";
                    var amtRaw = txnNode.SelectSingleNode("ns3:Amount", transactionNsManager)?.InnerText ?? "";
                    var curr = txnNode.SelectSingleNode("ns3:Currency", transactionNsManager)?.InnerText ?? "";
                    var date = txnNode.SelectSingleNode("ns3:TXNDATE", transactionNsManager)?.InnerText ?? "";

                    // ✅ Clean amount: remove leading currency letters (e.g., "EUR624.42" → "624.42")
                    string amtClean = Regex.Replace(amtRaw, @"^[A-Za-z]+", "").Trim();

                    transactionDetails.Add(new
                    {
                        TXNREF = refValue,
                        DRCRMARKER = marker,
                        Account = account,
                        Amount = amtClean,
                        Currency = curr,
                        TXNDATE = date
                    });

                    // Assign based on type
                    if (isTF)
                    {
                        // TF transactions have only one record
                        //debitAccount = "";
                        //creditAccount = account;
                        //amount = amtClean;
                        //txnRef = refValue;
                        //currency = curr;
                        //txnDate = date;

                        if (!string.IsNullOrEmpty(account) && account == depositFCY.AccountNumber)
                        {
                            // Only pick the matching CREDIT account
                            creditAccount = account;
                            if (string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(amtClean))
                                amount = amtClean;

                            txnRef = refValue;
                            currency = curr;
                            txnDate = date;
                        }
                    }                        // Assign based on type
                    else if (isFT)
                    {
                        // Identify debit and credit accounts
                        if (marker == "DEBIT")
                        {
                            debitAccount = account;
                            amount = amtClean;
                            currency = curr;
                        }
                        else if (marker == "CREDIT")
                        {
                            creditAccount = account;
                        }


                        txnRef = refValue;
                        txnDate = date;
                    }
                }

                // ✅ Populate ViewBag for UI
                ViewBag.TransactionDetails = transactionDetails;
                ViewBag.DebitAccount = debitAccount;
                ViewBag.CreditAccount = creditAccount;
                ViewBag.TransactionAmount = amount;
                ViewBag.TXNREF = txnRef;
                ViewBag.Currency = currency;
                ViewBag.TXNDATE = txnDate;

                // ✅ Validate account number
                if ((isTF) && depositFCY.AccountNumber != creditAccount)
                {
                    TempData["AccountMismatch"] = true;
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                    return View(depositFCY);
                }
                if ((isFT) && depositFCY.AccountNumber != creditAccount)
                {
                    TempData["AccountMismatch"] = true;
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                    return View(depositFCY);
                }
                else
                {
                    TempData["AccountMismatch"] = false;
                }

                // ✅ Extract numeric value of amount
                var amountMatch = Regex.Match(amount, @"\d+(\.\d+)?");
                amount = amountMatch.Success ? amountMatch.Value : "Not Found";

                // ✅ Set success info
                ViewBag.ReferenceExists = true;
                ViewBag.TransactionAmount = amount;
                ViewBag.TransactionReference = txnRef;

                // Populate dropdowns and return

                return View(depositFCY);

            }
            else
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    TempData["ErrorMessage"] = "Model validation failed: " + string.Join(", ", errors);
                    return View(depositFCY);
                }


                if (ModelState.IsValid)
                {
                    if (depositFCY.TransactionAmount <= 0 || depositFCY.TransactionAmount > 1000000000)
                    {
                        TempData["ErrorMessage"] = "Transaction Amount must be greater than zero and less than or equal to 1 Billion.";
                        return View(depositFCY);
                    }
                    if (depositFCY.Currecy == null)
                    {
                        TempData["ErrorMessage"] = "Please Enter Transaction Currecy!";
                        return View(depositFCY);
                    }
                    if (depositFCY.Currecy == "ETB")
                    {
                        TempData["ErrorMessage"] = "The Currency Type should be Foreign!";
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

                    if (depositFCY.DepositType == "Branch")
                    {
                        depositFCY.DepositType = "Branch";
                    }
                    else
                    {
                        depositFCY.DepositType = "Individual";
                    }

                    depositFCY.Process = Session["Process"] as string ?? string.Empty;
                    depositFCY.District = Session["District"] as string ?? string.Empty;
                    depositFCY.Branch = Session["UserHomeBranch"] as string ?? string.Empty;
                    depositFCY.Target = 0;
                    depositFCY.CreatedDate = DateTime.Now;
                    db.DepositFCies.Add(depositFCY);

                    if (TempData["AccountMismatch"] != null && (bool)TempData["AccountMismatch"] == true)
                    {
                        // 🧠 Keep the flag for next request (since TempData clears after reading)
                        TempData.Keep("AccountMismatch");
                        TempData["ErrorMessage"] = "Cannot save — account number mismatch detected. Please revalidate.";
                        return View(depositFCY);
                    }
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
