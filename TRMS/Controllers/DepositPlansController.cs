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


namespace TRMS.Controllers
{
    public class DepositPlansController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        SoapServiceHelper soapServiceHelper = new SoapServiceHelper();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();

        private void PopulateUserDropdown()
        {
            ViewBag.Users = new SelectList(db.Users.ToList(), "UserName", "UserName");
        }

        // GET: DepositPlans
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();
                var transactions = db.DepositPlans.Where(t => t.User == UserName).ToList();
                if (transactions == null)
                {
                    transactions = new List<DepositPlan>();
                }
                return View(transactions);
            }
        }

        
        // GET: DepositPlans/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositPlan depositPlan = db.DepositPlans.Find(id);
            if (depositPlan == null)
            {
                return HttpNotFound();
            }
            return View(depositPlan);
        }

        public ActionResult Create()
        {
            PopulateUserDropdown();
            var model = new DepositPlanViewModel
            {
                DepositPlan = new DepositPlan()
            };

            // Pass user list to dropdowns (for shared users)
            ViewBag.Users = db.Users
                .Select(u => new SelectListItem { Text = u.UserName, Value = u.UserName })
                .ToList();

            return View(model);
        }


        // POST: DepositPlans/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DepositPlanViewModel vm, string submit)
        {

            PopulateUserDropdown(); // ✅ always load before any View return

            var depositPlan = vm.DepositPlan;
            // Validate input model
            if (depositPlan == null || string.IsNullOrEmpty(depositPlan.AccountNumber))
            {
                TempData["ErrorMessage"] = depositPlan == null ? "Invalid transaction data." : "Account number is required.";
                return View(vm);
            }

            if (!string.IsNullOrEmpty(depositPlan.ReferenceNumber) && submit == "Validate")
            {
                try
                {
                    // Fetch and parse account balance
                    var balanceResponse = soapServiceHelper.GetAccountBalance(depositPlan.AccountNumber);
                    if (string.IsNullOrEmpty(balanceResponse))
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account balance.";
                        return View(vm);
                    }

                    var balanceDoc = new XmlDocument();
                    balanceDoc.LoadXml(balanceResponse);
                    var nsManager = new XmlNamespaceManager(balanceDoc.NameTable);
                    nsManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                    nsManager.AddNamespace("ns11", "http://temenos.com/TWSMMT");
                    nsManager.AddNamespace("ns7", "http://temenos.com/ACCTBALCTS");

                    // Check balance API status
                    var statusNode = balanceDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/Status/successIndicator", nsManager);
                    if (statusNode?.InnerText != "Success")
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account details or invalid account number.";
                        return View(vm);
                    }

                    // Extract account details
                    var accountNode = balanceDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/ACCTBALCTSType/ns7:gACCTBALCTSDetailType/ns7:mACCTBALCTSDetailType", nsManager);
                    if (accountNode == null)
                    {
                        TempData["ErrorMessage"] = "Account details not found in the response.";
                        return View(vm);
                    }

                    var accountNo = accountNode.SelectSingleNode("ns7:AcctNo", nsManager)?.InnerText ?? "Account Number Not Found";
                    var accountName = accountNode.SelectSingleNode("ns7:Name", nsManager)?.InnerText ?? "Account Name Not Found";
                    var workingBalance = accountNode.SelectSingleNode("ns7:WorkingBal", nsManager)?.InnerText.Replace(",", "") ?? "Working Balance Not Found";

                    // Store account details in ViewBag
                    ViewBag.AccountHolder = accountName;
                    ViewBag.AccountBalance = workingBalance;

                    // Fetch and parse transaction details by reference
                    var transactionResponse = callbyReference.GetAccountinformationByrefrence(depositPlan.ReferenceNumber);
                    if (string.IsNullOrEmpty(transactionResponse))
                    {
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Failed to retrieve transaction details or invalid reference number.";
                        return View(vm);
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
                        return View(vm);
                    }

                    // ✅ Select all transaction detail nodes
                    var transactionNodes = transactionDoc.SelectNodes("//S:Body/ns4:CBOTXNDETAILResponse/FTTTTXNDETAILType/ns3:gFTTTTXNDETAILDetailType/ns3:mFTTTTXNDETAILDetailType", transactionNsManager);

                    if (transactionNodes == null || transactionNodes.Count == 0)
                    {
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "No transaction details found in the response.";
                        return View(vm);
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
                    if (accountNo != creditAccount)
                    {
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number from transaction details does not match the provided account number.";
                        return View(vm);
                    }

                    // Parse transaction amount
                    var amountMatch = Regex.Match(debitAmount, @"\d+(\.\d+)?");
                    debitAmount = amountMatch.Success ? amountMatch.Value : "Not Found";

                    // Store transaction details in ViewBag
                    ViewBag.ReferenceExists = true;
                    ViewBag.TransactionAmount = debitAmount;
                    ViewBag.Currency = currency;
                    ViewBag.TransactionReference = txnRef;


                    //ViewBag.TransactionAmount = 5000;
                    PopulateUserDropdown();
                    return View(vm);
                }
                catch (XmlException ex)
                {
                    TempData["ErrorMessage"] = $"Error parsing API response: {ex.Message}";
                    PopulateUserDropdown();
                    return View(depositPlan);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                    PopulateUserDropdown();
                    return View(vm);
                }
            }

            // Check for reference number duplication
            if (!string.IsNullOrEmpty(depositPlan.ReferenceNumber))
            {
                var existingDeposits = db.DepositPlans
                    .Where(t => t.ReferenceNumber != null && t.ReferenceNumber.Trim() == depositPlan.ReferenceNumber.Trim())
                    .ToList();
                if (existingDeposits.Any())
                {
                    var createdBy = existingDeposits.FirstOrDefault()?.User ?? string.Empty;
                    var phoneNumber = db.Users
                        .Where(u => u.UserName == createdBy)
                        .FirstOrDefault()?.PhoneNumber ?? string.Empty;

                    TempData["ErrorMessage"] = $"Reference number already registered by {createdBy}. Contact phone: {phoneNumber}.";
                    return View(vm);
                }
            }

            if (submit == "Save")
            {
                var userName = Session["UserName"]?.ToString();
                var user = db.Users.FirstOrDefault(u => u.UserName == userName);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "User");
                }

                // ✅ Handle "Individual" deposit
                if (vm.DepositType == "Individual")
                {
                    depositPlan.User = userName;
                    depositPlan.UserID = user.ID;
                    depositPlan.Process = Session["Process"]?.ToString() ?? "";
                    depositPlan.District = Session["District"]?.ToString() ?? "";
                    depositPlan.Branch = Session["UserHomeBranch"]?.ToString() ?? "";
                    depositPlan.CreatedDate = DateTime.Now;

                    db.DepositPlans.Add(depositPlan);
                }
                else if (vm.DepositType == "Shared")
                {
                    // ✅ Save up to 3 shared users (but same UserID)
                    var sharedUsers = new List<(string username, decimal? amount)>
                            {
                                (vm.SharedUser1, vm.SharedAmount1),
                                (vm.SharedUser2, vm.SharedAmount2),
                                (vm.SharedUser3, vm.SharedAmount3)
                            }.Where(x => !string.IsNullOrEmpty(x.username) && x.amount.HasValue && x.amount.Value > 0)
                                     .ToList();

                    // Ensure at least 2 users sharing
                    if (sharedUsers.Count < 2)
                    {
                        TempData["ErrorMessage"] = "Please select at least two users for shared deposit.";
                        return View(vm);
                    }

                    foreach (var (sharedUserName, sharedAmount) in sharedUsers)
                    {
                        //  Fetch the actual shared user from DB
                        var sharedUser = db.Users.FirstOrDefault(u => u.UserName == sharedUserName);

                        if (sharedUser == null)
                            continue; // or handle error if you prefer strict validation

                        var dp = new DepositPlan
                        {
                            AccountNumber = depositPlan.AccountNumber,
                            ReferenceNumber = depositPlan.ReferenceNumber,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = sharedAmount ?? 0,
                            AccountBalance = depositPlan.AccountBalance,
                            IntialAccountBalance = depositPlan.AccountBalance,
                            Process = sharedUser.Process ?? "",
                            District = sharedUser.District ?? "",
                            Branch = sharedUser.Branch ?? "",
                            Narative = depositPlan.Narative ?? "",
                            User = sharedUser.UserName,
                            UserID = user.ID, // main recorder (logged-in user)
                            CreatedDate = DateTime.Now
                        };

                        db.DepositPlans.Add(dp);
                    }
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Deposit saved successfully!";
                return RedirectToAction("Create");
            }


            TempData["ErrorMessage"] = "Invalid data provided. Please check the form.";
            return View(vm);
        }




        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(DepositPlan depositPlan, string submit)
        //{
        //    if (depositPlan == null)
        //    {
        //        TempData["errorRes"] = "Invalid Transaction Data!";
        //        return View(depositPlan);
        //    }
        //    if (depositPlan.AccountNumber ==null)
        //    {
        //        TempData["errorRes"] = "Please Account Number!";
        //        return View(depositPlan);
        //    }

        //    if (submit.Equals("Validate"))
        //    {
        //        string xmlResponse = soapServiceHelper.GetAccountBalance(depositPlan.AccountNumber);
        //        XmlDocument xmlDoc = new XmlDocument();
        //        xmlDoc.LoadXml(xmlResponse); 
        //        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        //        nsManager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
        //        nsManager.AddNamespace("ns11", "http://temenos.com/TWSMMT");
        //        nsManager.AddNamespace("ns7", "http://temenos.com/ACCTBALCTS");

        //        // Check the success status from the XML
        //        var statusNode = xmlDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/Status/successIndicator", nsManager);

        //        if (statusNode != null && statusNode.InnerText == "Success")
        //        {
        //            var accountNode = xmlDoc.SelectSingleNode("//S:Body/ns11:MMTACCTBALANCEResponse/ACCTBALCTSType/ns7:gACCTBALCTSDetailType/ns7:mACCTBALCTSDetailType", nsManager);

        //            if (accountNode != null)
        //            {
        //                // Check if the child nodes are not null before accessing their InnerText
        //                var accountNoNode = accountNode.SelectSingleNode("ns7:AcctNo", nsManager);
        //                string accountNo = accountNoNode != null ? accountNoNode.InnerText : "Account Number Not Found";
        //                //string amount = accountNoNode.SelectSingleNode("ns2:Amount", nsManager)?.InnerText ?? "Not Found";

        //                var accountNameNode = accountNode.SelectSingleNode("ns7:Name", nsManager);
        //                string accountName = accountNameNode != null ? accountNameNode.InnerText : "Account Name Not Found";


        //                var workingBalNode = accountNode.SelectSingleNode("ns7:WorkingBal", nsManager);
        //                string workingBal = workingBalNode != null ? workingBalNode.InnerText : "Working Balance Not Found";
        //                workingBal = workingBal.Replace(",", string.Empty);

        //                //amount = Regex.Match(amount ?? "", @"\d+(\.\d+)?").Value;

        //                ViewBag.AccountHolder = accountName;
        //                ViewBag.AccountBalance = workingBal;
        //                //ViewBag.Amount = amount;

        //                bool refEXist = false;
        //                // from core system refrenece
        //                string xmlResponserref= callbyRefrence.GetAccountinformationByrefrence(depositPlan.ReferenceNumber); // Your method here
        //                XmlDocument xmlDocref = new XmlDocument();
        //                xmlDoc.LoadXml(xmlResponserref);

        //                XmlNamespaceManager nsManagerref = new XmlNamespaceManager(xmlDocref.NameTable);
        //                nsManagerref.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
        //                nsManagerref.AddNamespace("ns2", "http://temenos.com/FTTTTXNDETAIL");
        //                nsManagerref.AddNamespace("ns3", "http://temenos.com/TWSTXNDETAIL");

        //                // Check if the call was successful
        //                var statusNoderef = xmlDocref.SelectSingleNode("//S:Body/ns3:FTTTTXNDETAILResponse/Status/successIndicator", nsManagerref);
        //                if (statusNoderef != null && statusNoderef.InnerText == "Success")
        //                {
        //                    var detailNode = xmlDocref.SelectSingleNode("//S:Body/ns3:FTTTTXNDETAILResponse/FTTTTXNDETAILType/ns2:gFTTTTXNDETAILDetailType/ns2:mFTTTTXNDETAILDetailType", nsManagerref);
        //                    if (detailNode != null)
        //                    {
        //                        string txnRef = detailNode.SelectSingleNode("ns2:TXNREF", nsManagerref)?.InnerText ?? "Not Found";
        //                        string drcrMarker = detailNode.SelectSingleNode("ns2:DRCRMARKER", nsManagerref)?.InnerText ?? "Not Found";
        //                        string account = detailNode.SelectSingleNode("ns2:Account", nsManagerref)?.InnerText ?? "Not Found";
        //                        string amount = detailNode.SelectSingleNode("ns2:Amount", nsManagerref)?.InnerText ?? "Not Found";
        //                        string currency = detailNode.SelectSingleNode("ns2:Currency", nsManagerref)?.InnerText ?? "Not Found";

        //                        amount = Regex.Match(amount ?? "", @"\d+(\.\d+)?").Value;

        //                        ViewBag.TransactionAmount = amount;
        //                        ViewBag.Currency = currency;
        //                        refEXist = true;
        //                        ViewBag.refEXist = refEXist;

        //                    }
        //                    else
        //                    {
        //                        refEXist = false;
        //                        ViewBag.refEXist = refEXist;
        //                        TempData["errorRes"] = "Transaction details not found.";
        //                    }
        //                }
        //                else
        //                {
        //                    refEXist = false;
        //                    ViewBag.refEXist = refEXist;
        //                    TempData["errorRes"] = "Failed To Retrieve Transaction Detail or Invalide  RefernceNumber or Not Found.";
        //                }

        //                //ViewBag.TransactionAmount = 5000;
        //            }
        //            else
        //            {

        //                TempData["errorRes"] = "Account details not found in the XML response.";
        //            }
        //        }
        //        else
        //        {

        //            TempData["errorRes"]  = "Failed to retrieve account details or status not 'Success'.";
        //        }

        //        return View(depositPlan);
        //    }
        //    var accountNumberExist = db.DepositPlans.Where(t => t.AccountNumber.Trim() == depositPlan.AccountNumber.Trim()).ToList();
        //    if (accountNumberExist.Count() >= 1)
        //    {
        //        // Ensure transaction is not null before accessing ReferenceNumber
        //        string createdBy = db.DepositPlans
        //            .Where(u => u.AccountNumber == depositPlan.AccountNumber)
        //            .FirstOrDefault()?.User ?? string.Empty;  // Default to empty string if null

        //        string phoneNumber = db.Users
        //            .Where(u => u.UserName == createdBy)
        //            .FirstOrDefault()?.PhoneNumber ?? string.Empty;  // Default to empty string if null

        //        TempData["errorRes"] = "Account Number  duplication!" + "Registerd BY " + createdBy + "  " + "And  Phone Number is " + phoneNumber;
        //        return View(depositPlan);
        //    }
        //    else
        //    {

        //        if (ModelState.IsValid)
        //        {
        //            depositPlan.User = Session["UserName"].ToString();
        //            var userName = Session["UserName"]?.ToString();
        //            if (!string.IsNullOrEmpty(userName))
        //            {
        //                var user = db.Users.FirstOrDefault(u => u.UserName == userName);
        //                if (user != null)
        //                {
        //                    depositPlan.UserID = user.ID;
        //                }
        //                else
        //                {
        //                    // Handle case where user is not found
        //                    // e.g., throw an error, redirect, or set a default value
        //                }
        //            }
        //            else
        //            {
        //                return RedirectToAction("login", "User");
        //            }

        //            depositPlan.District = Session["District"].ToString();
        //            depositPlan.IntialAccountBalance= depositPlan.AccountBalance;
        //            depositPlan.AccountBalance = depositPlan.AccountBalance;
        //            depositPlan.Branch = Session["UserHomeBranch"].ToString();
        //            depositPlan.CreatedDate = DateTime.Now;
        //            db.DepositPlans.Add(depositPlan);
        //            db.SaveChanges();

        //            TempData["successRes"] = "Transaction saved successfully!";

        //            ModelState.Clear();
        //            depositPlan = new DepositPlan(); // Reinitialize model

        //            return View();
        //        }
        //    }
        //    return View(depositPlan);

        //}

        // GET: DepositPlans/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositPlan depositPlan = db.DepositPlans.Find(id);
            if (depositPlan == null)
            {
                return HttpNotFound();
            }
            return View(depositPlan);
        }

        // POST: DepositPlans/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DID,District,Branch,Target,AccountNumber,AccountHolder,AccountBalance,User,CreatedDate")] DepositPlan depositPlan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(depositPlan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(depositPlan);
        }

        // GET: DepositPlans/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DepositPlan depositPlan = db.DepositPlans.Find(id);
            if (depositPlan == null)
            {
                return HttpNotFound();
            }
            return View(depositPlan);
        }

        // POST: DepositPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DepositPlan depositPlan = db.DepositPlans.Find(id);
            db.DepositPlans.Remove(depositPlan);
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
