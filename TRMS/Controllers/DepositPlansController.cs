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
    public class DepositPlansController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        SoapServiceHelper soapServiceHelper = new SoapServiceHelper();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();

        [HttpGet]
        public JsonResult SearchUsers(string term, int page = 1)
        {
            const int pageSize = 10;

            // Start with base query
            var query = db.Users.AsQueryable();

            // Apply search filter if term is provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term;

                query = query.Where(s =>
                    (s.FullName != null && s.FullName.Contains(term)) ||
                    (s.UserName != null && s.UserName.Contains(term))
                );
            }

            // Count total results
            var total = query.Count();

            // Apply ordering, paging, and select required fields
            var users = query
                .OrderBy(s => s.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    id = s.UserName, // Use username as unique identifier if needed in dropdown
            text = s.FullName + " (" + s.UserName + ")"
                })
                .ToList();

            // Return paginated result for Select2
            return Json(new
            {
                items = users,
                hasMore = total > page * pageSize
            }, JsonRequestBehavior.AllowGet);
        }


        private void PopulateUserDropdown()
        {
            // Fetch users from DB first, then format
            ViewBag.Users = db.Users
                .AsEnumerable() // switch to in-memory, allows string formatting
                .Select(u => new SelectListItem
                {
                    Text = $"{u.FullName} ({u.UserName})",
                    Value = u.UserName
                })
                .ToList();           
        }

        private void PopulateReservedAccounts(string selectedAccount = null)
        {
            var currentUserName = Session["UserName"]?.ToString();

            // 1. Get raw data first (this is executed in SQL)
            var reservedAccountsRaw = db.AccountReserves
                .Where(a => a.UserName == currentUserName)
                .Select(a => new
                {
                    a.AccountNumber,
                    a.AccountHolder
                })
                .ToList();  // ← Execute query here

            // 2. Now format in memory (safe!)
            var reservedAccounts = reservedAccountsRaw
                .Select(a => new SelectListItem
                {
                    Value = a.AccountNumber,
                    Text = $"{a.AccountNumber} ({a.AccountHolder})"
                })
                .ToList();

            ViewBag.ReservedAccountNumbers = new SelectList(
                reservedAccounts,
                "Value",
                "Text",
                selectedAccount
            );
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

        public JsonResult SearchReservedAccounts(string q)
        {
            var currentUserName = Session["UserName"]?.ToString();

            if (string.IsNullOrEmpty(currentUserName))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            if (string.IsNullOrEmpty(q) || q.Length < 3)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // Step 1: Get raw data that EF can translate to SQL
            var rawResults = db.AccountReserves
                .Where(a => a.UserName == currentUserName &&
                            (a.AccountNumber.Contains(q) || a.AccountHolder.Contains(q)))
                .Select(a => new
                {
                    a.AccountNumber,
                    a.AccountHolder
                })
                .Take(50)
                .ToList(); // ← Execute query HERE, bring data into memory

            // Step 2: Now format safely in C# (in memory)
            var results = rawResults.Select(a => new
            {
                Value = a.AccountNumber,
                Text = $"{a.AccountNumber} ({a.AccountHolder})" // Safe now!
            }).ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetReservedAccountByNumber(string accountNumber)
        {
            var currentUserName = Session["UserName"]?.ToString();

            if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(currentUserName))
                return Json(null, JsonRequestBehavior.AllowGet);

            var account = db.AccountReserves
                .Where(a => a.UserName == currentUserName && a.AccountNumber == accountNumber)
                .Select(a => new
                {
                    a.AccountNumber,
                    a.AccountHolder
                })
                .FirstOrDefault();

            if (account == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                Value = account.AccountNumber,
                Text = $"{account.AccountNumber} ({account.AccountHolder})"
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Create()
        {
            PopulateUserDropdown();

            var currentUserName = Session["UserName"]?.ToString(); // or Session["UserName"]?.ToString();
            //PopulateReservedAccounts(); // no selection

            var model = new DepositPlanViewModel
            {
                DepositPlan = new DepositPlan()
            };

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
                                    // Pass the posted AccountNumber so it stays selected after validation
                                    //PopulateReservedAccounts(vm?.DepositPlan?.AccountNumber);
            ViewBag.SelectedReservedAccount = vm?.DepositPlan?.AccountNumber;

            var depositPlan = vm.DepositPlan;
        

            // ✅ Normalize input once (IMPORTANT)
            depositPlan.AccountNumber = depositPlan.AccountNumber?.Trim();
            depositPlan.ReferenceNumber = depositPlan.ReferenceNumber?.Trim();

            // Validate input model
            if (depositPlan == null || string.IsNullOrEmpty(depositPlan.AccountNumber))
            {
                TempData["ErrorMessage"] = depositPlan == null ? "Invalid transaction data." : "Account number is required.";
                return View(vm);
            }

            // Validate input model
            if (depositPlan.AccountNumber.Length < 5)
            {
                TempData["ErrorMessage"] = "The Minimum Length for Account Number is 5";
                return View(vm);
            }

            if (depositPlan.ReferenceNumber == "FT25284D3J02")
            {
                TempData["ErrorMessage"] = "Invalid Reference! The Amount exceeds the Initial Current Balance.";
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
                    nsManager.AddNamespace("ns2", "http://temenos.com/ACCTBALINFO");
                    nsManager.AddNamespace("ns4", "http://temenos.com/TWSTXNDETAIL");

                    // Check balance API status
                    var statusNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/Status/successIndicator", nsManager);
                    if (statusNode?.InnerText != "Success")
                    {
                        TempData["ErrorMessage"] = "Failed to retrieve account details or invalid account number.";
                        return View(vm);
                    }

                    // Extract account details
                    var accountNode = balanceDoc.SelectSingleNode("//S:Body/ns4:ACCOUNTBALANCEINFOResponse/ACCTBALINFOType/ns2:gACCTBALINFODetailType/ns2:mACCTBALINFODetailType", nsManager);
                    if (accountNode == null)
                    {
                        TempData["ErrorMessage"] = "Account details not found in the response.";
                        return View(vm);
                    }

                    var accountNo = accountNode.SelectSingleNode("ns2:AcctNo", nsManager)?.InnerText ?? "Account Number Not Found";
                    var accountName = accountNode.SelectSingleNode("ns2:Name", nsManager)?.InnerText ?? "Account Name Not Found";
                    var workingBalance = accountNode.SelectSingleNode("ns2:WorkingBal", nsManager)?.InnerText.Replace(",", "") ?? "Working Balance Not Found";

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
                    bool isTF = refType == "TF";
                    bool isDC = refType == "DC";
                    bool isMM = refType == "MM";

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
                        else if (isTF)
                        {
                            if (marker == "CREDIT" && !string.IsNullOrEmpty(account) && account == accountNo && curr == "ETB")
                            {
                                // Only pick the matching CREDIT account
                                creditAccount = account;
                                if (string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(amtClean))
                                    amount = amtClean;

                                txnRef = refValue;
                                currency = curr;
                                txnDate = date;
                            }
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
                        else if (isMM)
                        {
                            // MM transactions have only one record
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
                        return View(vm);
                    }
                    else if ((isTT) && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for TT reference.";
                        return View(vm);
                    }
                    else if ((isDC) && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for DC reference.";
                        return View(vm);
                    }
                    else if ((isTF) && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for TF reference.";
                        return View(vm);
                    }
                    else if ((isMM) && accountNo != creditAccount)
                    {
                        TempData["AccountMismatch"] = true;
                        ViewBag.ReferenceExists = false;
                        TempData["ErrorMessage"] = "Account number does not match transaction account for TF reference.";
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
                    ViewBag.TXNDATE = txnDate;

                    // Fetch previous balance (string JSON)
                    string json = callbyReference.GetPreviousBalance(depositPlan.AccountNumber, depositPlan.ReferenceNumber);
                    decimal opening = 0;
                    bool hasValidOpeningBalance = false;

                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            JArray arr = JArray.Parse(json);
                            if (arr.Count > 0)
                            {
                                JObject txn = (JObject)arr[0];

                                // Try to get and convert the value safely
                                if (txn["openingBalance"] != null)
                                {
                                    if (decimal.TryParse(txn["openingBalance"].ToString(), out decimal parsedValue))
                                    {
                                        opening = parsedValue;
                                        hasValidOpeningBalance = true;
                                    }
                                    else
                                    {
                                        // Log: value exists but is not a valid decimal
                                        System.Diagnostics.Debug.WriteLine($"Invalid decimal format for openingBalance in account {depositPlan.AccountNumber}");
                                    }
                                }
                                else
                                {
                                    // Log: key missing
                                    System.Diagnostics.Debug.WriteLine($"openingBalance key missing in response for account {depositPlan.AccountNumber}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Empty array returned for account {depositPlan.AccountNumber}");
                            }
                        }
                        catch (JsonException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"JSON parse error for account {depositPlan.AccountNumber}: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"GetPreviousBalance returned null/empty for account {depositPlan.AccountNumber}");
                    }

                    // Now decide what to show in UI
                    if (hasValidOpeningBalance)
                    {
                        ViewBag.PreviousBalance = opening;
                        ViewBag.PreviousBalanceStatus = "success"; // optional - for UI coloring
                    }
                    else
                    {
                        ViewBag.PreviousBalance = null;                  // or "-" or string.Empty
                        ViewBag.PreviousBalanceStatus = "error";         // optional
                        ViewBag.PreviousBalanceError = "Unable to load previous balance"; // optional message
                    }


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
                    .Where(t => t.ReferenceNumber != null && t.ReferenceNumber == depositPlan.ReferenceNumber)
                    .ToList();
                if (existingDeposits.Any())
                {
                    var createdBy = existingDeposits.FirstOrDefault()?.User ?? string.Empty;
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
                        $"<b>Process:</b> {process}<br/>" +
                        $"<b>District/SubProcess:</b> {district}<br/>" +
                        $"<b>Branch/Team:</b> {branch}";

                    return View(vm);
                }
            }


            if (submit == "Save")
            {
                var userName = Session["UserName"]?.ToString();
                var user = db.Users.FirstOrDefault(u => u.UserName == userName);
                //var projectStart = DateTime.ParseExact(
                //            "10/15/2025",
                //            "MM/dd/yyyy",
                //            System.Globalization.CultureInfo.InvariantCulture
                //        );
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

                // --- Guard: vm.depositplan.refdate after 20251015  ---
                if (vm.DepositPlan.RefDate == null)
                {
                    TempData["ErrorMessage"] = "You cannot register a ref date with null";
                    return View(vm);
                }

                // --- Guard: vm.depositplan.refdate after 20251015  ---
                if (vm.DepositPlan.RefDate < new DateTime(2026, 01, 1))
                {
                    TempData["ErrorMessage"] = "You cannot register a transaction before the project date";
                    return View(vm);
                }

                //// --- Guard: if the account is OD  ---
                if (vm.DepositPlan.Prev_Ini_Bal < 0 || vm.DepositPlan.Prev_Ini_Bal == null)
                {
                    TempData["ErrorMessage"] = "The Previous Initial Balance must be greater than 0";
                    return View(vm);
                }

                // --- Guard: if the account is OD  ---
                if (vm.DepositPlan.AccountBalance > 0)
                {
                    // --- Guard: vm.amount must less than or equal to initialbalance ---
                    if (vm.DepositPlan.Amount > vm.DepositPlan.AccountBalance)
                    {
                        TempData["ErrorMessage"] = "The Amount exceeds the Initial Current Balance.";
                        return View(vm);
                    }
                }


                // --- Guard: vm.DepositPlan must exist ---
                if (vm.DepositPlan.Amount == null || vm.DepositPlan.Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid Amount.";
                    return View(vm);
                }



                // --- Guard: vm.DepositPlan must exist ---

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

                        if (depositPlan.DepositType == "Individual")
                        {
                            TempData["ErrorMessage"] = "Eco account can not be registered for Individual";
                            return View(vm);
                        }
                        // The account belongs to another branch — split 60/40
                        var depositorAmount = depositPlan.Amount * 0.6m;
                        var ecoBranchAmount = depositPlan.Amount * 0.4m;

                        // 1️⃣ 60% for depositor
                        var depositorDeposit = new DepositPlan
                        {
                            AccountNumber = depositPlan.AccountNumber,
                            ReferenceNumber = depositPlan.ReferenceNumber,
                            RefDate = depositPlan.RefDate,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = depositorAmount,
                            AccountBalance = depositPlan.AccountBalance,
                            Prev_Ini_Bal = depositPlan.Prev_Ini_Bal,
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
                            RefDate = depositPlan.RefDate,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = ecoBranchAmount,
                            AccountBalance = depositPlan.AccountBalance,
                            Prev_Ini_Bal = depositPlan.Prev_Ini_Bal,
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
                    var ecoAccount = db.ECOes.FirstOrDefault(e => e.AccountNumber == depositPlan.AccountNumber);
                    if (ecoAccount != null)
                    {
                        TempData["ErrorMessage"] = "You can not share a deposit that comes from Eco Account";
                        return View(vm);
                    }
                    // ✅ Ensure first shared user and amount are always provided
                    if (string.IsNullOrWhiteSpace(vm.SharedUser1) || !vm.SharedAmount1.HasValue || vm.SharedAmount1 <= 0)
                    {
                        TempData["ErrorMessage"] = "The first shared user and amount must always be provided.";
                        return View(vm);
                    }
                    // ✅ Shared deposit validation
                    var sharedUsers = new List<(string username, decimal? amount)>
                        {
                            (vm.SharedUser1, vm.SharedAmount1),
                            (vm.SharedUser2, vm.SharedAmount2),
                            (vm.SharedUser3, vm.SharedAmount3),
                            (vm.SharedUser4, vm.SharedAmount4),
                            (vm.SharedUser5, vm.SharedAmount5)
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
                            RefDate = depositPlan.RefDate,
                            AccountHolder = depositPlan.AccountHolder ?? "Unknown",
                            Amount = sharedAmount ?? 0,
                            AccountBalance = depositPlan.AccountBalance,
                            Prev_Ini_Bal = depositPlan.Prev_Ini_Bal,
                            IntialAccountBalance = depositPlan.AccountBalance,
                            Process = sharedUser.Process ?? "",
                            District = sharedUser.District ?? "",
                            Branch = sharedUser.Branch ?? "",
                            Narative = "Shared",
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



        public ActionResult AssignMM()
        {
            // Don't load anything initially
            ViewBag.AccountList = new SelectList(new List<string>());
            return View();
        }

        // Separate AJAX endpoint for searching
        [HttpGet]
        public JsonResult SearchAccounts(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 8) // Minimum 2 chars
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var accounts = db.AccountReserves
                .AsNoTracking()
                .Where(x => x.AccountNumber != null &&
                           x.AccountNumber != "" &&
                           x.AccountNumber.Contains(term))
                .Select(x => x.AccountNumber)
                .Distinct()
                .Take(100)
                .OrderBy(x => x)
                .Select(x => new {
                    id = x,
                    text = x
                })
                .ToList();

            return Json(accounts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ValidateMM(string accountNumber, string mmReference)
        {
            if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(mmReference))
                return Json(new { success = false, message = "Account number and MM reference are required." });

            try
            {
                // Call your API
                var response = callbyReference.GetAccountinformationByrefrence(mmReference);

                if (string.IsNullOrEmpty(response))
                    return Json(new { success = false, message = "Invalid MM reference or no data returned." });

                // Parse XML
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(response);

                var ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                ns.AddNamespace("ns3", "http://temenos.com/FTTTTXNDETAIL");
                ns.AddNamespace("ns4", "http://temenos.com/TWSTXNDETAIL");

                var txnNode = doc.SelectSingleNode("//S:Body/ns4:CBOTXNDETAILResponse/FTTTTXNDETAILType/ns3:gFTTTTXNDETAILDetailType/ns3:mFTTTTXNDETAILDetailType", ns);

                if (txnNode == null)
                    return Json(new { success = false, message = "MM transaction details not found." });

                var account = txnNode.SelectSingleNode("ns3:Account", ns)?.InnerText ?? "";
                var amtRaw = txnNode.SelectSingleNode("ns3:Amount", ns)?.InnerText ?? "";

                string amtClean = Regex.Replace(amtRaw, @"^[A-Za-z]+", "").Trim();

                // Cross-check account
                if (account != accountNumber)
                    return Json(new { success = false, message = "MM reference account doesn't match selected account." });

                return Json(new
                {
                    success = true,
                    amount = amtClean
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SaveMM(string accountNumber, string mmReference, decimal amount)
        {
            if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(mmReference))
                return Json(new { success = false, message = "Account number and MM reference are required." });

            try
            {
                var rows = db.AccountReserves
                    .Where(x => x.AccountNumber == accountNumber)
                    .ToList();

                if (!rows.Any())
                    return Json(new { success = false, message = "No deposit records found with that account number." });

                foreach (var row in rows)
                {
                    row.MMACC = mmReference;
                    row.MMBAL = amount;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully updated {rows.Count} records with MMACC and MMBAL."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
