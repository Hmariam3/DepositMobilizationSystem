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

            // Fetch users from DB first, then format
            ViewBag.Users = db.Users
                .AsEnumerable() // switch to in-memory, allows string formatting
                .Select(u => new SelectListItem
                {
                    Text = $"{u.FullName} ({u.UserName})",
                    Value = u.UserName
                })
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

                    // ✅ Extract reference type
                    string referenceNumber = depositPlan.ReferenceNumber?.Trim() ?? "";
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
                        var amt = txnNode.SelectSingleNode("ns3:Amount", transactionNsManager)?.InnerText ?? "";
                        var curr = txnNode.SelectSingleNode("ns3:Currency", transactionNsManager)?.InnerText ?? "";
                        var date = txnNode.SelectSingleNode("ns3:TXNDATE", transactionNsManager)?.InnerText ?? "";

                        transactionDetails.Add(new
                        {
                            TXNREF = refValue,
                            DRCRMARKER = marker,
                            Account = account,
                            Amount = amt,
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
                            if (string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(amt))
                                amount = amt;
                            else if (!string.IsNullOrEmpty(amt))
                                amount = amt; // overwrite only if current node has valid amount

                            txnRef = refValue;
                            currency = curr;
                            txnDate = date;
                        }
                        else if (isTT)
                        {
                            // TT transactions have only one record
                            debitAccount = "";
                            creditAccount = account;
                            amount = amt;
                            txnRef = refValue;
                            currency = curr;
                            txnDate = date;
                        }
                        else if (isDC)
                        {
                            // TT transactions have only one record
                            debitAccount = "";
                            creditAccount = account;
                            amount = amt;
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
                        return View(vm);
                    }
                    else if (isTT && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for TT reference.";
                        return View(vm);
                    }
                    else if (isDC && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for DC reference.";
                        return View(vm);
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

                // --- Guard: vm.DepositPlan must exist ---
                if (vm.DepositPlan == null)
                {
                    TempData["ErrorMessage"] = "Deposit plan data is missing.";
                    return View(vm);
                }
                // --- Guard: vm.DepositPlan must exist ---
                if (vm.DepositPlan.Amount == null || vm.DepositPlan.Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid Amount.";
                    return View(vm);
                }

                //var depositPlan = vm.DepositPlan;

                // --- Read checkbox safely (HTML sends "true,false" if unchecked) ---
                bool isShared = vm.ShareDeposit; // Always reliable now
                if (depositPlan.DepositType == "Branch")
                {
                    depositPlan.DepositType = "Branch";
                }
                else
                {
                    depositPlan.DepositType = "Individual";
                }

                if (!isShared)
                {
                    // Single deposit (Individual or Branch)
                    var ecoAccount = db.ECOes.FirstOrDefault(e => e.AccountNumber == depositPlan.AccountNumber);
                    var depositorBranch = Session["UserHomeBranch"]?.ToString() ?? "";

                    if (ecoAccount != null && !string.Equals(ecoAccount.Branch, depositorBranch, StringComparison.OrdinalIgnoreCase))
                    {
                        // The account belongs to another branch — split 60/40
                        var depositorAmount = depositPlan.Amount * 0.6m;
                        var ecoBranchAmount = depositPlan.Amount * 0.4m;

                        // 1️⃣ 60% for depositor
                        var depositorDeposit = new DepositPlan
                        {
                            AccountNumber = depositPlan.AccountNumber,
                            ReferenceNumber = depositPlan.ReferenceNumber,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = depositorAmount,
                            AccountBalance = depositPlan.AccountBalance,
                            IntialAccountBalance = depositPlan.AccountBalance,
                            Process = Session["Process"]?.ToString() ?? "",
                            District = Session["District"]?.ToString() ?? "",
                            Branch = depositorBranch,
                            Narative = "(Auto-share) ECO",
                            User = userName,
                            UserID = user.ID,
                            CreatedDate = DateTime.Now,
                            DepositType = depositPlan.DepositType
                        };
                        db.DepositPlans.Add(depositorDeposit);

                        // 2️⃣ 40% for ECO branch owner
                        var ecoUser = db.Users.FirstOrDefault(u => u.UserName == ecoAccount.UserName);
                        var ecoDeposit = new DepositPlan
                        {
                            AccountNumber = depositPlan.AccountNumber,
                            ReferenceNumber = depositPlan.ReferenceNumber,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = ecoBranchAmount,
                            AccountBalance = depositPlan.AccountBalance,
                            IntialAccountBalance = depositPlan.AccountBalance,
                            Process = ecoUser?.Process ?? "",
                            District = ecoUser?.District ?? "",
                            Branch = ecoAccount.Branch,
                            Narative = "(Auto-share) ECO",
                            User = ecoAccount.UserName,
                            UserID = user.ID,
                            CreatedDate = DateTime.Now,
                            DepositType = depositPlan.DepositType
                        };
                        db.DepositPlans.Add(ecoDeposit);
                    }
                    else
                    {
                        // Normal deposit — same branch or not in ECO
                        depositPlan.User = userName;
                        depositPlan.UserID = user.ID;
                        depositPlan.Process = Session["Process"]?.ToString() ?? "";
                        depositPlan.District = Session["District"]?.ToString() ?? "";
                        depositPlan.Branch = depositorBranch;
                        depositPlan.IntialAccountBalance = depositPlan.AccountBalance;
                        depositPlan.CreatedDate = DateTime.Now;

                        db.DepositPlans.Add(depositPlan);
                    }
                }
                else
                {
                    // ✅ Shared deposit validation
                    var sharedUsers = new List<(string username, decimal? amount)>
                        {
                            (vm.SharedUser1, vm.SharedAmount1),
                            (vm.SharedUser2, vm.SharedAmount2),
                            (vm.SharedUser3, vm.SharedAmount3)
                        }
                    .Where(x => !string.IsNullOrWhiteSpace(x.username) && x.amount.GetValueOrDefault() > 0)
                    .ToList();

                    if (sharedUsers.Count < 2)
                    {
                        TempData["ErrorMessage"] = "Please select at least two users for shared deposit.";
                        return View(vm);
                    }

                    var totalSharedAmount = sharedUsers.Sum(x => x.amount.GetValueOrDefault());
                    if (totalSharedAmount > depositPlan.Amount)
                    {
                        TempData["ErrorMessage"] = $"Total shared amount ({totalSharedAmount:N2}) exceeds the deposit amount ({depositPlan.Amount:N2}).";
                        return View(vm);
                    }

                    foreach (var (sharedUserName, sharedAmount) in sharedUsers)
                    {
                        var sharedUser = db.Users.FirstOrDefault(u => u.UserName == sharedUserName);
                        if (sharedUser == null) continue;

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
                            UserID = user.ID,
                            DepositType = depositPlan.DepositType,
                            CreatedDate = DateTime.Now
                        };

                        db.DepositPlans.Add(dp);
                    }
                }

                try
                {
                    if (TempData["AccountMismatch"] != null && (bool)TempData["AccountMismatch"] == true)
                    {
                        // 🧠 Keep the flag for next request (since TempData clears after reading)
                        TempData.Keep("AccountMismatch");
                        TempData["ErrorMessage"] = "Cannot save — account number mismatch detected. Please revalidate.";
                        return View(vm);
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Deposit saved successfully!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Failed to save: " + ex.Message;
                    return View(vm);
                }

                return RedirectToAction("Create");
            }
            TempData["ErrorMessage"] = "Invalid data provided. Please check the form.";

            // Fetch users from DB first, then format
            ViewBag.Users = db.Users
                .AsEnumerable() // switch to in-memory, allows string formatting
                .Select(u => new SelectListItem
                {
                    Text = $"{u.FullName} ({u.UserName})",
                    Value = u.UserName
                })
                .ToList();
            return View(vm);
        }






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
