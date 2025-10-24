using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using TRMS.Models;
using TRMS.Security;
using System.Text.RegularExpressions;

namespace TRMS.Controllers
{
    public class DepositMerchantAgenetsController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        SoapServiceHelper soapServiceHelper = new SoapServiceHelper();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();
        // GET: DepositMerchantAgenets
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var transactions = db.DepositMerchantAgenets.Where(t => t.CreatedBY == UserName).ToList();
                if (transactions == null)
                {
                    transactions = new List<DepositMerchantAgenet>();
                }
                return View(transactions);
            }
        }

        // GET: DepositMerchantAgenets/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositMerchantAgenet depositMerchantAgenet = db.DepositMerchantAgenets.Find(id);
            if (depositMerchantAgenet == null)
            {
                return HttpNotFound();
            }
            return View(depositMerchantAgenet);
        }

        // GET: DepositMerchantAgenets/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DepositMerchantAgenets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DepositMerchantAgenet depositMerchantAgenet, string submit)
        {
            if (depositMerchantAgenet == null)
            {
                TempData["errorRes"] = "Invalid Transaction Data!";
                return View(depositMerchantAgenet);
            }
            if (depositMerchantAgenet.AccountNumber == null)
            {
                TempData["errorRes"] = "Please Account Number!";
                return View(depositMerchantAgenet);
            }
            if (depositMerchantAgenet.ReferenceNumber == null)
            {
                TempData["errorRes"] = "Please Enter  Reference Number!";
                return View(depositMerchantAgenet);
            }

            if (submit.Equals("Validate"))
            {
                string xmlResponse = soapServiceHelper.GetAccountBalance(depositMerchantAgenet.AccountNumber);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResponse);
                XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                nsManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                nsManager.AddNamespace("ns11", "http://temenos.com/TWSMMT");
                nsManager.AddNamespace("ns7", "http://temenos.com/ACCTBALCTS");

                // Check the success status from the XML
                var statusNode = xmlDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/Status/successIndicator", nsManager);

                if (statusNode != null && statusNode.InnerText == "Success")
                {
                    var accountNode = xmlDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/ACCTBALCTSType/ns7:gACCTBALCTSDetailType/ns7:mACCTBALCTSDetailType", nsManager);

                    if (accountNode != null)
                    {
                        // Check if the child nodes are not null before accessing their InnerText
                        var accountNoNode = accountNode.SelectSingleNode("ns7:AcctNo", nsManager);
                        string accountNo = accountNoNode != null ? accountNoNode.InnerText : "Account Number Not Found";

                        var accountNameNode = accountNode.SelectSingleNode("ns7:Name", nsManager);
                        string accountName = accountNameNode != null ? accountNameNode.InnerText : "Account Name Not Found";

                        var workingBalNode = accountNode.SelectSingleNode("ns7:WorkingBal", nsManager);
                        string workingBal = workingBalNode != null ? workingBalNode.InnerText : "Working Balance Not Found";
                        workingBal = workingBal.Replace(",", string.Empty);
                        ViewBag.AccountHolder = accountName;
                        ViewBag.AccountBalance = workingBal;

                    }
                    else
                    {
                        ViewBag.AccountHolder = "Account details not found in the XML response.";
                    }


                }
                else
                {
                    ViewBag.AccountBalance = "Failed to retrieve account details or status not 'Success'.";
                }



                // Fetch and parse transaction details by reference
                var transactionResponse = callbyReference.GetAccountinformationByrefrence(depositMerchantAgenet.ReferenceNumber);
                if (string.IsNullOrEmpty(transactionResponse))
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Failed to retrieve transaction details or invalid reference number.";
                    return View(depositMerchantAgenet);
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
                    return View(depositMerchantAgenet);
                }

                // ✅ Select all transaction detail nodes
                var transactionNodes = transactionDoc.SelectNodes("//S:Body/ns4:CBOTXNDETAILResponse/FTTTTXNDETAILType/ns3:gFTTTTXNDETAILDetailType/ns3:mFTTTTXNDETAILDetailType", transactionNsManager);

                if (transactionNodes == null || transactionNodes.Count == 0)
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "No transaction details found in the response.";
                    return View(depositMerchantAgenet);
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
                if (depositMerchantAgenet.AccountNumber != creditAccount)
                {
                    ViewBag.ReferenceExists = false;
                    TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                    return View(depositMerchantAgenet);
                }

                // Parse transaction amount
                var amountMatch = Regex.Match(debitAmount, @"\d+(\.\d+)?");
                debitAmount = amountMatch.Success ? amountMatch.Value : "Not Found";

                // Store transaction details in ViewBag
                ViewBag.ReferenceExists = true;
                ViewBag.TransactionAmount = debitAmount;
                ViewBag.Currency = currency;
                ViewBag.TransactionReference = txnRef;


                return View(depositMerchantAgenet);
            }
            else
            {
                // Check for reference number duplication
                if (!string.IsNullOrEmpty(depositMerchantAgenet.ReferenceNumber))
                {
                    var existingMerchant = db.DepositMerchantAgenets
                        .Where(t => t.ReferenceNumber != null && t.ReferenceNumber.Trim() == depositMerchantAgenet.ReferenceNumber.Trim())
                        .ToList();
                    if (existingMerchant.Any())
                    {
                        var createdBy = existingMerchant.FirstOrDefault()?.CreatedBY ?? string.Empty;
                        var phoneNumber = db.Users
                            .Where(u => u.UserName == createdBy)
                            .FirstOrDefault()?.PhoneNumber ?? string.Empty;

                        TempData["ErrorMessage"] = $"Reference number already registered by {createdBy}. Contact phone: {phoneNumber}.";
                        return View(depositMerchantAgenet);
                    }
                }
             
                    if (ModelState.IsValid)
                    {
                        depositMerchantAgenet.CreatedBY = Session["UserName"].ToString();
                        var userName = Session["UserName"]?.ToString();
                        if (!string.IsNullOrEmpty(userName))
                        {
                            var user = db.Users.FirstOrDefault(u => u.UserName == userName);
                            if (user != null)
                            {
                                depositMerchantAgenet.UserID = user.ID;
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

                        depositMerchantAgenet.IntialAccountBalance = depositMerchantAgenet.AccountBalance;
                        depositMerchantAgenet.AccountBalance = depositMerchantAgenet.AccountBalance;
                        depositMerchantAgenet.District = Session["District"].ToString();
                        depositMerchantAgenet.Branch = Session["UserHomeBranch"].ToString();
                        depositMerchantAgenet.Process = Session["Process"]?.ToString() ?? string.Empty;
                        depositMerchantAgenet.CreatedDate = DateTime.Now;
                        db.DepositMerchantAgenets.Add(depositMerchantAgenet);
                        db.SaveChanges();

                    TempData["SuccessMessage"] = "Transaction saved successfully!";
                    ModelState.Clear();

                    depositMerchantAgenet = new DepositMerchantAgenet(); // Reinitialize model
                        return View(depositMerchantAgenet);
                    
                }
            }
            return View(depositMerchantAgenet);
        }

        // GET: DepositMerchantAgenets/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositMerchantAgenet depositMerchantAgenet = db.DepositMerchantAgenets.Find(id);
            if (depositMerchantAgenet == null)
            {
                return HttpNotFound();
            }
            return View(depositMerchantAgenet);
        }

        // POST: DepositMerchantAgenets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MID,District,Branch,Target,LinkAccount,AccountNumber,AccountHolder,AccountBalance,CreatedBY,CreatedDate")] DepositMerchantAgenet depositMerchantAgenet)
        {
            if (ModelState.IsValid)
            {
                db.Entry(depositMerchantAgenet).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(depositMerchantAgenet);
        }

        // GET: DepositMerchantAgenets/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositMerchantAgenet depositMerchantAgenet = db.DepositMerchantAgenets.Find(id);
            if (depositMerchantAgenet == null)
            {
                return HttpNotFound();
            }
            return View(depositMerchantAgenet);
        }

        // POST: DepositMerchantAgenets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DepositMerchantAgenet depositMerchantAgenet = db.DepositMerchantAgenets.Find(id);
            db.DepositMerchantAgenets.Remove(depositMerchantAgenet);
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
