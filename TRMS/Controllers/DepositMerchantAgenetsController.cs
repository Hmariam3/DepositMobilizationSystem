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
using Newtonsoft.Json.Linq;

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


            // ✅ Normalize input once (IMPORTANT)
            depositMerchantAgenet.AccountNumber = depositMerchantAgenet.AccountNumber?.Trim();
            depositMerchantAgenet.ReferenceNumber = depositMerchantAgenet.ReferenceNumber?.Trim();
            depositMerchantAgenet.LinkAccount = depositMerchantAgenet.LinkAccount?.Trim();

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



            if (!string.IsNullOrEmpty(depositMerchantAgenet.ReferenceNumber) && submit == "Validate")
            {
                try
                {
                    // Fetch and parse account balance
                    var balanceResponse = soapServiceHelper.GetAccountBalance(depositMerchantAgenet.AccountNumber);
                    if (string.IsNullOrEmpty(balanceResponse))
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account balance.";
                        return View(depositMerchantAgenet);
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
                        return View(depositMerchantAgenet);
                    }

                    // Extract account details
                    var accountNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/ACCTBALINFOType/ns2:gACCTBALINFODetailType/ns2:mACCTBALINFODetailType", nsManager);
                    if (accountNode == null)
                    {
                        TempData["ErrorMessage"] = "Account details not found in the response.";
                        return View(depositMerchantAgenet);
                    }

                    var accountNo = accountNode.SelectSingleNode("ns2:AcctNo", nsManager)?.InnerText ?? "Account Number Not Found";
                    var accountName = accountNode.SelectSingleNode("ns2:Name", nsManager)?.InnerText ?? "Account Name Not Found";
                    var workingBalance = accountNode.SelectSingleNode("ns2:WorkingBal", nsManager)?.InnerText.Replace(",", "") ?? "Working Balance Not Found";

                    // Store account details in ViewBag
                    ViewBag.AccountHolder = accountName;
                    ViewBag.AccountBalance = workingBalance;


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

                    // ✅ Extract reference type
                    string referenceNumber = depositMerchantAgenet.ReferenceNumber?.Trim() ?? "";
                    string refType = referenceNumber.Length >= 2 ? referenceNumber.Substring(0, 2).ToUpper() : "";
                    bool isFT = refType == "FT";
                    bool isTT = refType == "TT";
                    //bool isTF = refType == "TF";
                    bool isDC = refType == "DC";

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
                        if (isFT)
                        {
                            // Identify debit and credit accounts
                            if (marker == "DEBIT")
                                debitAccount = account;
                            else if (marker == "CREDIT")
                                creditAccount = account;

                            // ✅ Capture amount: take whichever node has a non-empty value
                            if (string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(amtClean))
                                amount = amtClean;
                            else if (!string.IsNullOrEmpty(amtClean))
                                amount = amtClean; // overwrite only if current node has valid amount

                            txnRef = refValue;
                            currency = curr;
                            txnDate = date;
                        }
                        else if (isTT)
                        {
                            // TT transactions have only one record
                            debitAccount = "";
                            creditAccount = account;
                            amount = amtClean;
                            txnRef = refValue;
                            currency = curr;
                            txnDate = date;
                        }
                        else if (isDC)
                        {
                            // TT transactions have only one record
                            debitAccount = "";
                            creditAccount = account;
                            amount = amtClean;
                            txnRef = refValue;
                            currency = curr;
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
                    if ((isFT) && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                        return View(depositMerchantAgenet);
                    }
                    else if (isTT && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for TT reference.";
                        return View(depositMerchantAgenet);
                    }
                    else if (isDC && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for DC reference.";
                        return View(depositMerchantAgenet);
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



                    // Fetch previous balance (string JSON)
                    string json = callbyReference.GetPreviousBalance(depositMerchantAgenet.AccountNumber, depositMerchantAgenet.ReferenceNumber);
                    decimal opening = 0;
                    if (!string.IsNullOrEmpty(json))
                    {
                        // Parse JSON array
                        JArray arr = JArray.Parse(json);

                        if (arr.Count > 0)
                        {
                            JObject txn = (JObject)arr[0];

                            opening = (decimal?)txn["openingBalance"] ?? 0;

                        }
                    }
                    ViewBag.PreviousBalance = opening;

                    // Populate dropdowns and return

                    return View(depositMerchantAgenet);

                }
                catch (XmlException ex)
                {
                    TempData["ErrorMessage"] = $"Error parsing API response: {ex.Message}";
                    return View(depositMerchantAgenet);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                    //PopulateUserDropdown();
                    return View(depositMerchantAgenet);
                }
            }
            else
            {
                // Check for merchantid duplication
                if (!string.IsNullOrEmpty(depositMerchantAgenet.LinkAccount))
                {
                    var existingRecord = db.DepositMerchantAgenets
                        .FirstOrDefault(t => t.LinkAccount == depositMerchantAgenet.LinkAccount);

                    // If record exists AND it was created by someone else → block
                    if (existingRecord != null &&
                        !string.Equals(existingRecord.CreatedBY, Session["username"]?.ToString(),
                                       StringComparison.OrdinalIgnoreCase))
                    {
                        var phoneNumber = db.Users
                            .Where(u => u.UserName == existingRecord.CreatedBY)
                            .Select(u => u.PhoneNumber)
                            .FirstOrDefault() ?? string.Empty;

                        TempData["ErrorMessage"] =
                            $"MerchantID already registered by {existingRecord.CreatedBY}. Contact phone: {phoneNumber}.";

                        return View(depositMerchantAgenet);
                    }

                    // ✅ If same user → allowed automatically
                }

                // Check for reference number duplication
                if (!string.IsNullOrEmpty(depositMerchantAgenet.ReferenceNumber))
                {
                    var existingMerchant = db.DepositMerchantAgenets
                        .Where(t => t.ReferenceNumber != null && t.ReferenceNumber == depositMerchantAgenet.ReferenceNumber)
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

                        if (depositMerchantAgenet.DepositType == "Branch")
                        {
                            depositMerchantAgenet.DepositType = "Branch";
                        }
                        else
                        {
                            depositMerchantAgenet.DepositType = "Individual";
                        }
                        if (depositMerchantAgenet.LinkAccount == null)
                        {
                            TempData["errorRes"] = "Please Enter Merchant ID!";
                            return View(depositMerchantAgenet);
                        }
                        depositMerchantAgenet.IntialAccountBalance = depositMerchantAgenet.AccountBalance;
                        depositMerchantAgenet.AccountBalance = depositMerchantAgenet.AccountBalance;
                        depositMerchantAgenet.Process = Session["Process"] as string ?? string.Empty;
                        depositMerchantAgenet.District = Session["District"] as string ?? string.Empty;
                        depositMerchantAgenet.Branch = Session["UserHomeBranch"] as string ?? string.Empty;


                    depositMerchantAgenet.CreatedDate = DateTime.Now;
                        db.DepositMerchantAgenets.Add(depositMerchantAgenet);


                        if (TempData["AccountMismatch"] != null && (bool)TempData["AccountMismatch"] == true)
                        {
                            // 🧠 Keep the flag for next request (since TempData clears after reading)
                            TempData.Keep("AccountMismatch");
                            TempData["ErrorMessage"] = "Cannot save — account number mismatch detected. Please revalidate.";
                            return View(depositMerchantAgenet);
                        }
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
