using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class HomeController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        callAccountwithRefrenece callbyReference = new callAccountwithRefrenece();
        public ActionResult Index()
        {
            // user limit 
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                //string UserName = "ABEBESD";
                string UserName = Session["UserName"].ToString();
                string UserID = Session["UserID"]?.ToString() ?? "";
                string process = Session["Process"]?.ToString() ?? "";
                string district = Session["District"]?.ToString() ?? "";
                string branch = Session["UserHomeBranch"]?.ToString() ?? "";
                string position = Session["Position"]?.ToString() ?? "";
                string role = Session["UserRole"]?.ToString() ?? "";


                if (role != null || position != null)
                {
                    // 1. Assigned target (unchanged)
                    decimal assignedTarget = db.Users
                        .Where(u => u.UserName == UserName)
                        .Select(u => (decimal?)u.DepositTargetAmount)
                        .FirstOrDefault() ?? 0;
                    ViewBag.AssinedTargetdeposit = assignedTarget;

                    // 2. Get all user's deposit rows
                    var userDeposits = db.DepositPlans
                        .Where(d => d.User == UserName)
                        .ToList();

                    // 3. Group by unique AccountNumber
                    var accounts = userDeposits
                        .Select(d => d.AccountNumber)
                        .Distinct()
                        .ToList();

                    decimal totalUserAchieved = 0m;
                    var breakdownList = new List<DepositAchievementBreakdown>();

                    foreach (var acc in accounts)
                    {
                        // All deposit rows for this account (not just user's)
                        var accDeposits = db.DepositPlans
                            .Where(d => d.AccountNumber == acc)
                            .OrderBy(d => d.RefDate)
                            .ThenBy(d => d.DID)
                            .ToList();

                        if (!accDeposits.Any()) continue;

                        // True Initial Balance: Prev_Ini_Bal from the earliest record
                        decimal trueInitial = accDeposits
                            .Where(d => d.Prev_Ini_Bal.HasValue)
                            .OrderBy(d => d.RefDate)
                            .ThenBy(d => d.DID)
                            .FirstOrDefault()?.Prev_Ini_Bal ?? 0m;

                        // Current Balance: Latest from AccountReserve (daily updated)
                        decimal currentBalance = db.AccountReserves
                            .Where(ar => ar.AccountNumber == acc)
                            .OrderByDescending(ar => ar.CreatedDate)
                            .Select(ar => ar.AccountBalance ?? 0m)
                            .FirstOrDefault();

                        // Total Deposits to this account (all users)
                        decimal totalDeposits = accDeposits.Sum(d => d.Amount);

                        // User's total contribution to this account
                        decimal userContribution = accDeposits
                            .Where(d => d.User == UserName)
                            .Sum(d => d.Amount);

                        if (userContribution <= 0) continue; // Skip if user did nothing

                        // Incremental / Available Fund
                        decimal availableFund = Math.Max(0m, currentBalance - trueInitial);

                        // Cap: never give more credit than total deposits
                        availableFund = Math.Min(availableFund, totalDeposits);

                        // User's proportional share
                        decimal userAchieved = (totalDeposits > 0)
                            ? (userContribution / totalDeposits) * availableFund
                            : 0m;

                        // Safety: never exceed user's actual contribution
                        userAchieved = Math.Min(userAchieved, userContribution);

                        totalUserAchieved += userAchieved;

                        // Add to breakdown for UI/report
                        breakdownList.Add(new DepositAchievementBreakdown
                        {
                            AccountNumber = acc,
                            InitialBalance = trueInitial,
                            FinalBalance = currentBalance,
                            TotalDeposited = totalDeposits,
                            UserContribution = userContribution,
                            AvailableFund = availableFund,
                            UserAchieved = userAchieved
                        });
                    }

                    // Outputs
                    ViewBag.AchievedBreakdown = breakdownList;
                    ViewBag.achivedDeposit = totalUserAchieved;

                    // Remaining target
                    decimal remaining = assignedTarget - totalUserAchieved;
                    ViewBag.remaing = remaining < 0 ? 0 : remaining;

                    // ---- DAILY PROGRESS LOGIC (unchanged) ----
                    DateTime startDate = new DateTime(2025, 10, 1);
                    DateTime today = DateTime.Today;
                    int totalDays = 91;

                    int daysElapsed = (today - startDate).Days;
                    if (daysElapsed < 0) daysElapsed = 0;
                    if (daysElapsed > totalDays) daysElapsed = totalDays;

                    decimal expected = (assignedTarget / totalDays) * daysElapsed;

                    decimal progressPercent = assignedTarget == 0 ? 0 :
                        (totalUserAchieved / assignedTarget) * 100;

                    string color = totalUserAchieved < expected ? "bg-danger" : "bg-success";

                    if (totalUserAchieved >= expected)
                    {
                        ViewBag.MessageClass = "alert-success";
                        ViewBag.MessageText = "✅ Great job! You are on track with your target.";
                    }
                    else
                    {
                        ViewBag.MessageClass = "alert-danger";
                        ViewBag.MessageText = "⚠️ You are behind the expected progress.";
                    }

                    ViewBag.ProgressPercent = progressPercent;
                    ViewBag.Color = color;
                    ViewBag.Collected = totalUserAchieved;
                    ViewBag.Expected = expected;
                    ViewBag.DaysElapsed = daysElapsed;

                    // 6. User branch progress
                    decimal branchProgress = 0;
                    decimal averageUserProgress = 0;

                    if (!string.IsNullOrEmpty(branch))
                    {
                        decimal branchTarget = db.Users
                            .Where(u => u.Branch == branch)
                            .Select(u => (decimal?)u.DepositTargetAmount)
                            .Sum() ?? 0;

                        decimal branchAchieved = 0;

                        // Sum achieved for ALL USERS IN BRANCH
                        var branchUsers = db.Users
                            .Where(u => u.Branch == branch)
                            .Select(u => u.UserName)
                            .ToList();

                        foreach (var u in branchUsers)
                        {
                            var uDeposits = db.DepositPlans.Where(d => d.User == u).ToList();
                            branchAchieved += uDeposits.Sum(d => d.Amount);
                        }

                        branchProgress = branchTarget == 0 ? 0 : (branchAchieved / branchTarget) * 100;

                        averageUserProgress = (progressPercent + branchProgress) / 2;
                    }

                    ViewBag.ProgressPercent = progressPercent;
                    ViewBag.avargaeUserProgressDeposit = averageUserProgress;
                    ViewBag.Color = color;
                    ViewBag.Collected = totalUserAchieved;
                    ViewBag.Expected = expected;
                    ViewBag.DaysElapsed = daysElapsed;


                    // =============== MERCHANT REPORT - UPDATED WITH VALIDATED ACHIEVEMENT LOGIC ===============
                    // =============== MERCHANT PERFORMANCE - SIMPLIFIED & CORRECT LOGIC ===============

                    // 1. Assigned Merchant Target (number of merchants to onboard)
                    decimal assignedTargetMerchant = db.Users
                        .Where(u => u.UserName == UserName)
                        .Select(u => (decimal?)u.MerchantTarget)
                        .FirstOrDefault() ?? 0;

                    ViewBag.AssinedTargetAgent = assignedTargetMerchant;

                    // 2. All merchant records created by this user
                    var userMerchantRecords = db.DepositMerchantAgenets
                        .Where(d => d.CreatedBY == UserName)
                        .ToList();

                    // Achieved: Number of unique LinkAccount (i.e., unique merchants onboarded by user)
                    int achievedMerchants = userMerchantRecords
                        .Where(d => !string.IsNullOrEmpty(d.LinkAccount))
                        .Select(d => d.LinkAccount)
                        .Distinct()
                        .Count();

                    ViewBag.achivedDepositAgenet = achievedMerchants;

                    // Collected Amount: Total Target (deposit amount) entered by this user across all their merchants
                    decimal collectedAmountByUser = userMerchantRecords.Sum(d => d.Target);
                    ViewBag.CollectedAgent = collectedAmountByUser;  // This is the actual money they brought in

                    // Planned total from user's entries (same as collected if no validation)
                    ViewBag.depositplanAgent = collectedAmountByUser;

                    // Optional: Total initial & current balance across user's merchants (for info)
                    decimal totalInitialBalance = userMerchantRecords.Sum(d => d.IntialAccountBalance);
                    decimal totalCurrentBalance = userMerchantRecords.Sum(d => d.AccountBalance);
                    ViewBag.AaccountountIntialAgent = totalInitialBalance;
                    ViewBag.acountBlancemercht = totalCurrentBalance;

                    // Remaining merchants to onboard
                    decimal remainingMerchants = assignedTargetMerchant - achievedMerchants;
                    if (remainingMerchants < 0) remainingMerchants = 0;
                    ViewBag.remaing1 = remainingMerchants;

                    // =============== DAILY PROGRESS (Merchant Onboarding) ===============
                    //DateTime startDate = new DateTime(2025, 10, 1);
                    //DateTime today = DateTime.Today;
                    //int totalDays = 90;
                    //int daysElapsed = Math.Max(0, Math.Min(totalDays, (today - startDate).Days));

                    decimal expectedMerchants = Math.Ceiling((assignedTargetMerchant / totalDays) * daysElapsed); // round up
                    decimal progressPercentAgent = assignedTargetMerchant == 0 ? 0 :
                        (achievedMerchants / assignedTargetMerchant) * 100;

                    bool isOnTrack = achievedMerchants >= expectedMerchants;
                    string colorAgent = isOnTrack ? "bg-success" : "bg-danger";

                    ViewBag.progressPercentAgent = Math.Round(progressPercentAgent, 2);
                    ViewBag.colorAgent = colorAgent;
                    ViewBag.ExpectedAgent = expectedMerchants;
                    ViewBag.DaysElapsed = daysElapsed;

                    ViewBag.MessageClass1 = isOnTrack ? "alert-success" : "alert-danger";
                    ViewBag.MessageText1 = isOnTrack
                        ? "Great job! You're on track with merchant onboarding."
                        : "Warning: You're behind on merchant onboarding target.";

                    // =============== Branch Merchant Progress (Simple & Fast) ===============
                    decimal branchProgressMer = 0;
                    decimal avargaeUserProgressMerchant = 0;

                    if (!string.IsNullOrEmpty(branch))
                    {
                        decimal branchTarget = db.Users
                            .Where(u => u.Branch == branch)
                            .Select(u => (decimal?)u.MerchantTarget)
                            .Sum() ?? 0;

                        int branchTotalMerchants = db.DepositMerchantAgenets
                            .Where(d => d.Branch == branch && !string.IsNullOrEmpty(d.LinkAccount))
                            .Select(d => d.LinkAccount)
                            .Distinct()
                            .Count();

                        branchProgressMer = branchTarget > 0 ? (branchTotalMerchants / branchTarget) * 100 : 0;
                        avargaeUserProgressMerchant = (progressPercentAgent + branchProgressMer) / 2;
                    }

                    ViewBag.avargaeUserProgressMerchant = Math.Round(avargaeUserProgressMerchant, 2);

                    // Optional: Pass list of onboarded merchants for modal (simple version)
                    var merchantList = userMerchantRecords
                        .Where(x => !string.IsNullOrEmpty(x.LinkAccount))
                        .GroupBy(d => d.LinkAccount)
                        .Select(g => new MerchantOnboardedViewModel
                        {
                            LinkAccount = g.Key,
                            AccountHolder = g.First().AccountHolder ?? "N/A",
                            AccountNumber = g.First().AccountNumber ?? "N/A",
                            TotalDepositedByUser = g.Sum(x => x.Target),
                            CreatedDate = g.Min(x => x.CreatedDate)
                        })
                        .OrderByDescending(x => x.CreatedDate)
                        .ToList();

                    ViewBag.AchievedBreakdownMerchant = merchantList;


                    // fcy deposit 

                    decimal AssinedTargetFCY = db.Users.Where(u => u.UserName == UserName).Select(d => (decimal?)d.FCYTargetAmount).Sum() ?? 0;
                    ViewBag.AssinedTargetFCY = AssinedTargetFCY;

                    //decimal depositplanFCY = db.Fcy_Branch.Where(u => u.CreatedBy == UserName).Select(d => (decimal?)d.FcyTargetAmount).Sum() ?? 0;
                    //ViewBag.depositplanFCY = depositplanFCY;

                    //achived target
                    decimal achivedDepositFCY = db.DepositFCies.Where(u => u.User == UserName).Sum(u => (decimal?)u.TransactionAmount) ?? 0;

                    decimal diffdepositplanFCY = AssinedTargetFCY - achivedDepositFCY;
                    if (diffdepositplanFCY < 0)
                    {
                        ViewBag.achivedDepositFCY = achivedDepositFCY;
                        ViewBag.remaing2 = 0;
                    }
                    else
                    {
                        ViewBag.remaing2 = AssinedTargetFCY - achivedDepositFCY;
                        ViewBag.achivedDepositFCY = achivedDepositFCY;
                    }
                    // daily progress report for fcy

                    decimal totalTargetFCY = AssinedTargetFCY;
                    // Expected amount to collect by today
                    decimal expectedFCY = (totalTargetFCY / totalDays) * daysElapsed;

                    // Actual collected from DB
                    decimal actualCollectedFCY = achivedDepositFCY;

                    // Progress percentage
                    decimal progressPercentFCY = totalTargetFCY != 0 ? (actualCollectedFCY / totalTargetFCY) * 100 : 0;


                    // Color logic
                    string colorFCY = actualCollectedFCY < expectedFCY ? "bg-danger" : "bg-success";
                    if (actualCollectedFCY >= expectedFCY)
                    {
                        ViewBag.MessageClass2 = "alert-success";
                        ViewBag.MessageText2 = "✅ Great job!  on track with your target.";
                    }
                    else
                    {
                        ViewBag.MessageClass2 = "alert-danger";
                        ViewBag.MessageText2 = "⚠️ Warning: You are behind the expected progress.";
                    }

                    // Calculate progress for Fcy 
                    decimal branchProgressFCy = 0;
                    decimal avargaeUserProgressFcy = 0;

                    if (branch != null || branch != "")
                    {
                        decimal branchTargetFcy = db.Users.Where(u => u.Branch == branch).Select(d => (decimal?)d.FCYTargetAmount).Sum() ?? 0;
                        decimal branchAchievedFcy = db.DepositFCies.Where(u => u.Branch == branch).Select(d => (decimal?)d.TransactionAmount).Sum() ?? 0;
                        branchProgressFCy = branchTargetFcy > 0 ? (branchAchievedFcy / branchTargetFcy) * 100 : 0;
                        avargaeUserProgressFcy = (progressPercentFCY + branchProgressFCy) / 2;
                    }
                    // Pass values to view
                    ViewBag.progressPercentFCY = progressPercentFCY;
                    ViewBag.avargaeUserProgressFcy = avargaeUserProgressFcy;

                    ViewBag.colorAFCY = colorFCY;
                    ViewBag.CollectedFCY = actualCollectedFCY;
                    ViewBag.ExpectedFCY = expectedFCY;
                    ViewBag.DaysElapsed = daysElapsed;

                    // ======================== SAVE / UPDATE UserAchieved TABLE ========================
                    try
                    {
                        // Try to find existing record (case-insensitive username match)
                        var existingRecord = db.UserAchieveds
                            .FirstOrDefault(ua => ua.UserName == UserName);

                        UserAchieved entityToSave;

                        // These are the values we *hope* to write
                        //decimal? newCollectedAmount = rawUserDeposits;
                        decimal? newAchievedAmount = totalUserAchieved;
                        decimal? newAchievedPercent = null;
                        decimal? newUserTarget = assignedTarget > 0 ? assignedTarget : (decimal?)null;

                        // Calculate percentage only if we have good data
                        if (newAchievedAmount.HasValue && newUserTarget.HasValue && newUserTarget.Value > 0)
                        {
                            newAchievedPercent = Math.Round((newAchievedAmount.Value / newUserTarget.Value) * 100, 2);
                        }
                        else if (existingRecord != null)
                        {
                            // If calculation failed → keep old values
                            //newCollectedAmount = existingRecord.CollectedAmount;
                            newAchievedAmount = existingRecord.AchievedAmount;
                            newAchievedPercent = existingRecord.AchievedPercent;
                            // UserTarget usually stays the same unless explicitly changed
                            // so we keep existing one if new one is invalid
                            newUserTarget = existingRecord.UserTarget;
                        }
                        // else: new record + bad calculation → will create with nulls/zeros (your choice)

                        if (existingRecord == null)
                        {
                            // INSERT - only if we have reasonable data, otherwise maybe skip?
                            // Here we still create, but with fallback to 0/null
                            entityToSave = new UserAchieved
                            {
                                UserName = UserName,
                                UserID = Convert.ToInt32(UserID),
                                Process = process,
                                District = district,
                                Branch = branch,
                                Position = position,
                                UserTarget = newUserTarget,
                                //CollectedAmount = newCollectedAmount ?? 0,
                                AchievedAmount = newAchievedAmount ?? 0,
                                AchievedPercent = newAchievedPercent ?? 0,
                                UpdatedDate = DateTime.Now
                            };

                            db.UserAchieveds.Add(entityToSave);
                        }
                        else
                        {
                            // UPDATE - always safer to keep old values if calculation failed
                            existingRecord.Process = process ?? existingRecord.Process;
                            existingRecord.District = district ?? existingRecord.District;
                            existingRecord.Branch = branch ?? existingRecord.Branch;
                            existingRecord.Position = position ?? existingRecord.Position;

                            existingRecord.UserTarget = newUserTarget;
                            //existingRecord.CollectedAmount = newCollectedAmount ?? existingRecord.CollectedAmount;
                            existingRecord.AchievedAmount = newAchievedAmount ?? existingRecord.AchievedAmount;
                            existingRecord.AchievedPercent = newAchievedPercent ?? existingRecord.AchievedPercent;

                            existingRecord.UpdatedDate = DateTime.Now;

                            entityToSave = existingRecord;
                        }

                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // Very important: if even SaveChanges fails → do NOT crash the whole process
                        System.Diagnostics.Debug.WriteLine($"Error saving UserAchieved for {UserName}: {ex.Message}");
                        // _logger?.LogError(ex, "Failed to update UserAchieved for user {UserName}", UserName);
                    }
                    // ================================================================================



                    // ======================== SAVE / UPDATE MerchantAchieved TABLE ========================
                    try
                    {
                        // Find existing record (case-insensitive)
                        var existingMerchantRecord = db.MerchantAchieveds
                            .FirstOrDefault(ma => ma.UserName == UserName);

                        MerchantAchieved entityToSave;

                        // These are the values we *try* to write
                        int? newUserTarget = assignedTargetMerchant > 0 ? (int?)assignedTargetMerchant : null;
                        decimal? newCollectedAmount = collectedAmountByUser;
                        int? newAchievedMerchant = achievedMerchants;
                        decimal? newAchievedPercent = null;

                        // Only calculate percentage if we have reasonable data
                        bool calculationLooksGood =
                            achievedMerchants > 0 ||
                            assignedTargetMerchant > 0 ||
                            collectedAmountByUser > 0;

                        if (calculationLooksGood && assignedTargetMerchant > 0)
                        {
                            newAchievedPercent = Math.Round((achievedMerchants * 100m) / assignedTargetMerchant, 2);
                        }
                        else if (existingMerchantRecord != null)
                        {
                            // Calculation failed → preserve previous good values
                            newUserTarget = existingMerchantRecord.UserTarget;
                            newCollectedAmount = existingMerchantRecord.CollectedAmount;
                            newAchievedMerchant = existingMerchantRecord.AchievedMerchant;
                            newAchievedPercent = existingMerchantRecord.AchievedPercent;
                        }
                        // else: new record + bad calculation → will be created with zeros/nulls

                        if (existingMerchantRecord == null)
                        {
                            // INSERT new record
                            entityToSave = new MerchantAchieved
                            {
                                UserName = UserName,
                                UserID = string.IsNullOrEmpty(UserID) ? (int?)null : Convert.ToInt32(UserID),
                                Process = process,
                                District = district,
                                Branch = branch,
                                Position = position,
                                UserTarget = newUserTarget,
                                CollectedAmount = newCollectedAmount ?? 0,
                                AchievedMerchant = newAchievedMerchant ?? 0,
                                AchievedPercent = newAchievedPercent ?? 0,
                                UpdatedDate = DateTime.Now
                            };

                            db.MerchantAchieveds.Add(entityToSave);
                        }
                        else
                        {
                            // UPDATE existing record - safe fallback
                            existingMerchantRecord.Process = process ?? existingMerchantRecord.Process;
                            existingMerchantRecord.District = district ?? existingMerchantRecord.District;
                            existingMerchantRecord.Branch = branch ?? existingMerchantRecord.Branch;
                            existingMerchantRecord.Position = position ?? existingMerchantRecord.Position;

                            existingMerchantRecord.UserTarget = newUserTarget;
                            existingMerchantRecord.CollectedAmount = newCollectedAmount ?? existingMerchantRecord.CollectedAmount;
                            existingMerchantRecord.AchievedMerchant = newAchievedMerchant ?? existingMerchantRecord.AchievedMerchant;
                            existingMerchantRecord.AchievedPercent = newAchievedPercent ?? existingMerchantRecord.AchievedPercent;

                            existingMerchantRecord.UpdatedDate = DateTime.Now;

                            entityToSave = existingMerchantRecord;
                        }

                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving MerchantAchieved for {UserName}: {ex.Message}");
                        // Recommended: _logger?.LogError(ex, "Failed to update MerchantAchieved for user {UserName}", UserName);
                    }
                    // ================================================================================

                }


                return View();
            }
        }


        public ActionResult BranchReport()
        {
            var model = new CombinedReportViewModel();

            string district = Session["UserHomeBranch"].ToString();
            var depositPlanGrouped = db.DepositPlans.Where(d => d.District.Trim() == district.Trim())
                .GroupBy(x => x.Branch.Trim()).Select(g => new
                {
                    Branch = g.Key,
                    AmountCollected = g.Sum(x => x.AccountBalance)
                }).ToList();

            var branchTargets = db.BranchTargets.Where(bt => bt.District.Trim() == district.Trim())
                .Select(bt => new
                {
                    Branch = bt.BranchName.Trim(),
                    TotalTarget = bt.Deposit
                }).ToList();
            model.DepositPlanData = (from deposit in depositPlanGrouped
                                     join target in branchTargets
                                     on deposit.Branch equals target.Branch into gj
                                     from subTarget in gj.DefaultIfEmpty()
                                     select new BranchDataViewModel
                                     {
                                         Branch = deposit.Branch,
                                         AmountCollected = deposit.AmountCollected,
                                         TotalTarget = subTarget != null ? subTarget.TotalTarget : 0
                                     }).ToList();




            // for merchant
            var merchantGroup = db.DepositMerchantAgenets.Where(d => d.District.Trim() == district.Trim())
                 .GroupBy(x => x.Branch.Trim()).Select(g => new
                 {
                     Branch = g.Key,
                     AmountCollected = g.Sum(x => x.AccountBalance)
                 }).ToList();

            var branchTargetsmerchant = db.BranchTargets.Where(bt => bt.District.Trim() == district.Trim())
                       .Select(bt => new
                       {
                           Branch = bt.BranchName.Trim(),
                           MerchantTarget = bt.merchant
                       }).ToList();

            model.DepositMerchantAgentData = (from merchant in merchantGroup
                                              join target in branchTargetsmerchant
                                              on merchant.Branch equals target.Branch into joined
                                              from sub in joined.DefaultIfEmpty()
                                              select new BranchDataViewModel
                                              {
                                                  Branch = merchant.Branch,
                                                  AmountCollected = merchant.AmountCollected,
                                                  TotalTarget = sub != null ? sub.MerchantTarget : 0
                                              }).ToList();


            // fcy 
            var depositFcyGroup = db.DepositFCies.Where(d => d.District.Trim() == district.Trim())
                  .GroupBy(x => x.Branch.Trim()).Select(g => new
                  {
                      Branch = g.Key,
                      AmountCollected = g.Sum(x => x.TransactionAmount)
                  }).ToList();

            var branchFcyTargets = db.BranchTargets.Where(bt => bt.District.Trim() == district.Trim())
                        .Select(bt => new
                        {
                            Branch = bt.BranchName.Trim(),
                            FcyTarget = bt.FCY
                        }).ToList();

            model.Depositfcy = (from deposit in depositFcyGroup
                                join target in branchFcyTargets
                                on deposit.Branch equals target.Branch into joined
                                from sub in joined.DefaultIfEmpty()
                                select new BranchDataViewModel
                                {
                                    Branch = deposit.Branch,
                                    AmountCollected = deposit.AmountCollected,
                                    TotalTarget = sub != null ? sub.FcyTarget : 0
                                }).ToList();

            return View(model);
        }


        public ActionResult BydistrictReport()


        {
            var model = new DistrictCombinedReportViewModel();

            string district = Session["UserHomeBranch"].ToString();

            // Deposit Plan Grouped by District
            var collectedDepositPlans = db.DepositPlans.GroupBy(x => x.District.Trim())
             .Select(g => new
             {
                 District = g.Key,
                 AmountCollected = g.Sum(x => x.AccountBalance),
                 InnitialBalance = g.Sum(x => x.IntialAccountBalance)
             }).ToList();
            var districtTargets = db.DistrictPlans.Select(d => new
            {
                District = d.DistrictName.Trim(),
                TotalTarget = d.Deposit
            }).ToList();
            model.DepositPlanData = (from collected in collectedDepositPlans
                                     join target in districtTargets
                                     on collected.District equals target.District into joined
                                     from sub in joined.DefaultIfEmpty()
                                     select new DistrictDataViewModel
                                     {
                                         District = collected.District,
                                         AmountCollected = collected.AmountCollected - collected.InnitialBalance,
                                         TotalTarget = sub != null ? sub.TotalTarget : 0
                                     }).ToList();


            // Merchant Agent Grouped by District

            var collectedMerchantAgent = db.DepositMerchantAgenets
                .GroupBy(x => x.District.Trim()).Select(g => new
                {
                    District = g.Key,
                    AmountCollected = g.Sum(x => x.AccountBalance),
                    IntialBalance = g.Sum(x => x.IntialAccountBalance)
                }).ToList();
            var districtTargetsagent = db.DistrictPlans.Select(dp => new
            {
                District = dp.DistrictName.Trim(),
                TotalTarget = dp.merchant
            }).ToList();
            model.DepositMerchantAgentData = (from collected in collectedMerchantAgent
                                              join target in districtTargetsagent
                                              on collected.District equals target.District into joined
                                              from sub in joined.DefaultIfEmpty()
                                              select new DistrictDataViewModel
                                              {
                                                  District = collected.District,
                                                  AmountCollected = collected.AmountCollected - collected.IntialBalance,
                                                  TotalTarget = sub != null ? sub.TotalTarget : 0
                                              }).ToList();


            // FCY Grouped by District
            var depositData = db.DepositFCies.GroupBy(x => x.District.Trim())
                     .Select(g => new
                     {
                         District = g.Key,
                         AmountCollected = g.Sum(x => x.TransactionAmount)
                     }).ToList();
            var targetData = db.DistrictPlans.GroupBy(x => x.DistrictName.Trim())
                .Select(g => new
                {
                    District = g.Key,
                    TotalTarget = g.Sum(x => x.FCY)
                }).ToList();
            var result = (from deposit in depositData
                          join target in targetData
                          on deposit.District equals target.District into joined
                          from sub in joined.DefaultIfEmpty()
                          select new DistrictDataViewModel
                          {
                              District = deposit.District,
                              AmountCollected = deposit.AmountCollected,
                              TotalTarget = sub != null ? sub.TotalTarget : 0
                          }).ToList();

            model.DepositFcy = result;
            return View(model);
        }


        public ActionResult bankDashbord()
        {
            var districtPlans = db.DistrictPlans.ToList();
            var depositRaw = db.DepositPlans
                .GroupBy(x => x.District)
                .Select(g => new
                {
                    District = g.Key,
                    TotalDeposit = g.Sum(x => x.AccountBalance - x.IntialAccountBalance)
                }).ToList();

            var merchantRaw = db.DepositMerchantAgenets
                .GroupBy(x => x.District)
                .Select(g => new
                {
                    District = g.Key,
                    TotalDeposit = g.Sum(x => x.AccountBalance - x.IntialAccountBalance)
                }).ToList();

            var fcyRaw = db.DepositFCies
                .GroupBy(x => x.District)
                .Select(g => new
                {
                    District = g.Key,
                    TotalDeposit = g.Sum(x => x.TransactionAmount)
                }).ToList();

            var depositData = depositRaw.Select(g =>
            {
                var plan = districtPlans.FirstOrDefault(p => p.DistrictName == g.District);
                return new DistrictDepositViewModel
                {
                    District = g.District,
                    TotalTarget = plan?.Deposit ?? 0,
                    TotalDeposit = g.TotalDeposit,
                    Type = "Deposit"
                };
            }).ToList();

            var merchantData = merchantRaw.Select(g =>
            {
                var plan = districtPlans.FirstOrDefault(p => p.DistrictName == g.District);
                return new DistrictDepositViewModel
                {
                    District = g.District,
                    TotalTarget = plan?.merchant ?? 0,
                    TotalDeposit = g.TotalDeposit,
                    Type = "Merchant"
                };
            }).ToList();

            var fcyData = fcyRaw.Select(g =>
            {
                var plan = districtPlans.FirstOrDefault(p => p.DistrictName == g.District);
                return new DistrictDepositViewModel
                {
                    District = g.District,
                    TotalTarget = plan?.FCY ?? 0,
                    TotalDeposit = g.TotalDeposit,
                    Type = "FCY"
                };
            }).ToList();

            var allData = depositData.Concat(merchantData).Concat(fcyData).ToList();
            ViewBag.AllData = allData;
            return View();
        }



        private (decimal Remaining, decimal Expected, decimal ProgressPercent, decimal Achieved, string MessageClass, string MessageText) CalculateProgress(
            decimal target, decimal achieved, decimal initial, int daysElapsed, int totalDays)
        {
            achieved -= initial;
            decimal remaining = Math.Max(target - achieved, 0);
            decimal expected = (target / totalDays) * daysElapsed;
            decimal progressPercent = target != 0 ? (achieved / target) * 100 : 0;
            string messageClass = achieved >= expected ? "alert-success" : "alert-danger";
            string messageText = achieved >= expected ? "✅ Great job! On track with your target." : "⚠️ Warning: You are behind the expected progress.";
            return (remaining, expected, progressPercent, achieved, messageClass, messageText);
        }


        public ActionResult Dashboard()
        {
            var summary = callbyReference.GetBankBalanceSummary();
            var bankData = summary?.FirstOrDefault() ?? new BankBalanceSummary();

            ViewBag.BankBalanceSummary = bankData;

            var currentUserName = Session["UserName"]?.ToString();
            if (string.IsNullOrEmpty(currentUserName))
                return RedirectToAction("Login", "User");

            var currentUser = db.Users.FirstOrDefault(u => u.UserName == currentUserName);
            if (currentUser == null)
                return RedirectToAction("Login", "User");

            string role = currentUser.Role ?? "";
            string position = currentUser.Postion ?? "";
            string branch = currentUser.Branch?.Trim();
            string district = currentUser.District?.Trim();
            string process = currentUser.Process?.Trim();

            // Default values
            // Default values
            decimal depositTarget = 0, depositAchieved = 0, depositCollected = 0;
            decimal merchantTarget = 0, merchantAchieved = 0, merchantCollected = 0;
            decimal fcyTarget = 0, fcyAchieved = 0;

            // Scope filter based on role & position
            IQueryable<UserAchieved> depositScope = db.UserAchieveds.Where(ua => ua.UserTarget >= 0);
            IQueryable<MerchantAchieved> merchantScope = db.MerchantAchieveds.Where(ma => ma.UserTarget >= 0);
            IQueryable<User> userTargetScope = db.Users.AsQueryable(); // For Merchant & FCY targets from Users table
            IQueryable<DepositFCY> fcyScope = db.DepositFCies.AsQueryable(); // FCY still from raw table

            if (role == "Maker" && (position == "Manager" || position == "BranchManager"))
            {
                ViewBag.Scope = "Branch";
                ViewBag.ScopeName = branch;

                depositScope = depositScope.Where(ua => ua.Branch == branch);
                merchantScope = merchantScope.Where(ma => ma.Branch == branch);
                userTargetScope = userTargetScope.Where(u => u.Branch == branch);
                fcyScope = fcyScope.Where(f => f.Branch == branch);

            }
            else if (role == "Maker" && (position == "Director" || position == "SeniorDirector"))
            {
                ViewBag.Scope = "District";
                ViewBag.ScopeName = district;

                depositScope = depositScope.Where(ua => ua.District == district);
                merchantScope = merchantScope.Where(ma => ma.District == district);
                userTargetScope = userTargetScope.Where(u => u.Branch == branch);
                fcyScope = fcyScope.Where(f => f.District == district);
            }
            else if (role == "Maker" && (position == "VP" || position == "CHF"))
            {
                ViewBag.Scope = "Process";
                ViewBag.ScopeName = process;

                depositScope = depositScope.Where(ua => ua.Process == process);
                merchantScope = merchantScope.Where(ma => ma.Process == process);
                userTargetScope = userTargetScope.Where(u => u.Process == process);
                fcyScope = fcyScope.Where(f => f.Process == process);
            }
            else if ((role == "Maker" && position == "CEO") || role == "Super")
            {
                ViewBag.Scope = "Bank";
                ViewBag.ScopeName = "COOPBANK";
                // No filter → entire bank
            }
            else
            {
                // Fallback: individual user only
                ViewBag.Scope = "Individual";
                ViewBag.ScopeName = currentUser.FullName ?? currentUserName;

                depositScope = depositScope.Where(ua => ua.UserName == currentUserName);
                merchantScope = merchantScope.Where(ma => ma.UserName == currentUserName);
                userTargetScope = userTargetScope.Where(u => u.UserName == currentUserName);
                fcyScope = fcyScope.Where(f => f.User == currentUserName);
            }

            // === DEPOSIT: From UserAchieved (fast & pre-calculated) ===
            var depositSummary = depositScope
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Target = g.Sum(x => x.UserTarget ?? 0),
                    Achieved = g.Sum(x => x.AchievedAmount ?? 0),
                    Collected = g.Sum(x => x.CollectedAmount ?? 0)
                })
                .FirstOrDefault();

            if (depositSummary != null)
            {
                depositTarget = depositSummary.Target;
                depositAchieved = depositSummary.Achieved;
                depositCollected = depositSummary.Collected;
            }

            // === MERCHANT: NOW FROM MerchantAchieved (fast & pre-calculated) ===
            var merchantSummary = merchantScope
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Target = g.Sum(x => x.UserTarget ?? 0),
                    Achieved = g.Sum(x => x.AchievedMerchant ?? 0),
                    Collected = g.Sum(x => x.CollectedAmount ?? 0m)
                })
                .FirstOrDefault();

            if (merchantSummary != null)
            {
                merchantTarget = merchantSummary.Target;
                merchantAchieved = merchantSummary.Achieved;
                merchantCollected = merchantSummary.Collected;
            }

            // === FCY TARGET: From Users table (scoped) ===
            fcyTarget = userTargetScope.Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

            // === FCY ACHIEVED: From DepositFCies (scoped) ===
            fcyAchieved = fcyScope.Sum(f => (decimal?)f.TransactionAmount) ?? 0;


            // === PROGRESS CALCULATIONS ===
            decimal depositProgress = depositTarget > 0 ? Math.Round(depositAchieved / depositTarget * 100, 2) : 0;
            decimal merchantProgress = merchantTarget > 0 ? Math.Round(merchantAchieved / merchantTarget * 100, 2) : 0;
            decimal fcyProgress = fcyTarget > 0 ? Math.Round(fcyAchieved / fcyTarget * 100, 2) : 0;

            // Remaining
            decimal depositRemaining = Math.Max(0, depositTarget - depositAchieved);
            decimal merchantRemaining = Math.Max(0, merchantTarget - merchantAchieved);
            decimal fcyRemaining = Math.Max(0, fcyTarget - fcyAchieved);

            // === SEND TO VIEW ===
            ViewBag.ScopeTitle = ViewBag.ScopeName;

            // Deposit
            ViewBag.DepositPlanTarget = depositTarget;
            ViewBag.DepositPlanAchieved = depositAchieved;
            ViewBag.DepositRemainingAmount = depositRemaining;
            ViewBag.DepositCollected = depositCollected;
            ViewBag.DepositPlanProgress = depositProgress;

            // Merchant (now accurate & fast!)
            ViewBag.MerchantTarget = merchantTarget;
            ViewBag.MerchantAchieved = merchantAchieved;
            ViewBag.MerchantCollected = merchantCollected;
            ViewBag.MerchantRemainingAmount = merchantRemaining;
            ViewBag.MerchantProgress = merchantProgress;

            // FCY
            ViewBag.FCYTarget = fcyTarget;
            ViewBag.FCYAchieved = fcyAchieved;
            ViewBag.FCYRemainingAmount = fcyRemaining;
            ViewBag.FCYProgress = fcyProgress;

            return View();
        }

        public ActionResult BankSummary()
        {
            var summary = callbyReference.GetBankBalanceSummary();
            return View(summary);
        }

        //public ActionResult Process()
        //{

        //    // Get current logged-in user (adjust to your login system)
        //    var currentUserName = Session["UserName"];
        //    User user = db.Users.FirstOrDefault(u => u.UserName == currentUserName);

        //    string role = user.Role;
        //    string userBranch = user.Branch;
        //    string userDistrict = user.Branch;
        //    string userPosition = user.Postion;

        //    decimal totalDepositTarget = 0, totalMerchantTarget = 0, totalFCYTarget = 0;
        //    decimal totalDepositAchieved = 0, totalMerchantAchieved = 0, totalFCYAchieved = 0;
        //    decimal initialBalance = 0, initialAmountMerchant = 0;
        //    decimal actualTargetDeposit = 0, actualTargetMerchant = 0, actualTargetFcy = 0;
        //    decimal remainingAmountDeposit = 0, remainingAmountMerchant = 0, remainingAmountFcy = 0;
        //    int uniqueMerchantCount = 0;
        //    ViewBag.Process = user.Process;
        //    string Process = user.Process.Trim();

        //    // Get all users in this process
        //    var processUsers = db.Users
        //        .Where(u => u.Process != null && u.Process.Trim() == Process)
        //        .Select(u => new
        //        {
        //            u.UserName,
        //            u.DepositTargetAmount,
        //            u.MerchantTarget,
        //            u.FCYTargetAmount
        //        })
        //        .ToList();

        //    // Total Targets (unchanged)
        //    totalDepositTarget = processUsers.Sum(u => (decimal?)u.DepositTargetAmount) ?? 0;
        //    totalMerchantTarget = processUsers.Sum(u => (decimal?)u.MerchantTarget) ?? 0;
        //    totalFCYTarget = processUsers.Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

        //    // Get user names list
        //    var processUserNames = processUsers.Select(u => u.UserName.Trim()).ToList();

        //    // Load deposits (filtered by process users)
        //    var processDeposits = db.DepositPlans
        //        .Where(d => processUserNames.Contains(d.User.Trim()))
        //        .Select(d => new
        //        {
        //            d.User,
        //            d.AccountNumber,
        //            d.Amount,
        //            d.Prev_Ini_Bal,
        //            d.AccountBalance,
        //            d.RefDate,
        //            d.CreatedDate,
        //            d.ReferenceNumber,
        //            d.Process,
        //            d.DID
        //        })
        //        .ToList();

        //    // Group by AccountNumber
        //    var accountsDict = processDeposits
        //        .GroupBy(d => d.AccountNumber)
        //        .ToDictionary(g => g.Key, g => g.ToList());

        //    // Dictionary to store each user's validated achieved amount
        //    Dictionary<string, decimal> userValidatedAchieved = new Dictionary<string, decimal>();

        //    // Initialize dictionary with zero values for all users
        //    foreach (var userName in processUserNames)
        //    {
        //        userValidatedAchieved[userName.Trim()] = 0m;
        //    }

        //    totalDepositAchieved = 0m;

        //    foreach (var kvp in accountsDict)
        //    {
        //        var accountNumber = kvp.Key;
        //        var rows = kvp.Value;

        //        // Group by ReferenceNumber
        //        var refGroups = rows
        //            .GroupBy(d => d.ReferenceNumber ?? $"DID_{d.DID}")
        //            .Select(g => new
        //            {
        //                Rows = g.OrderBy(r => r.RefDate ?? r.CreatedDate).ThenBy(r => r.DID).ToList(),
        //                FirstRefDate = g.Min(r => r.RefDate ?? r.CreatedDate),
        //                FirstDID = g.Min(r => r.DID),
        //                InitialBalance = g.OrderBy(r => r.RefDate ?? r.CreatedDate).ThenBy(r => r.DID).First().Prev_Ini_Bal ?? 0m,
        //                FinalBalanceInGroup = g.OrderBy(r => r.RefDate ?? r.CreatedDate).ThenBy(r => r.DID).Last().AccountBalance,
        //                TotalAmount = g.Sum(r => r.Amount),
        //                UserName = g.First().User?.Trim() ?? "", // Get user from the first row
        //    UserProcess = g.Select(r => r.Process)
        //            })
        //            .OrderBy(g => g.FirstRefDate).ThenBy(g => g.FirstDID)
        //            .ToList();

        //        if (!refGroups.Any()) continue;

        //        var reverseGroups = refGroups
        //            .OrderByDescending(g => g.FirstRefDate)
        //            .ThenByDescending(g => g.FirstDID)
        //            .ToList();

        //        decimal trueInitialBalance = refGroups.First().InitialBalance;
        //        decimal finalAccountBalance = refGroups.Last().FinalBalanceInGroup;

        //        decimal validatedAchieved = 0m;
        //        bool validationFailed = false;

        //        // Dictionary to track each user's contribution to this account's validated amount
        //        Dictionary<string, decimal> accountUserContributions = new Dictionary<string, decimal>();

        //        for (int i = 0; i < reverseGroups.Count; i++)
        //        {
        //            var group = reverseGroups[i];
        //            string groupUser = group.UserName;
        //            decimal prevBal = group.InitialBalance;
        //            decimal amount = group.TotalAmount;

        //            decimal validationBalance = i == 0
        //                ? group.FinalBalanceInGroup
        //                : reverseGroups[i - 1].InitialBalance;

        //            // Validation passes → full credit to this user
        //            if (prevBal + amount <= validationBalance + 0.01m)
        //            {
        //                validatedAchieved += amount;

        //                // Track user's contribution
        //                if (!accountUserContributions.ContainsKey(groupUser))
        //                    accountUserContributions[groupUser] = 0m;
        //                accountUserContributions[groupUser] += amount;
        //            }
        //            else
        //            {
        //                validationFailed = true;
        //                decimal availableFunds = validationBalance - trueInitialBalance;

        //                if (availableFunds > 0)
        //                {
        //                    // Calculate proportional share for users in failed group and beyond
        //                    decimal totalRemainingDeposits = 0m;
        //                    var userRemainingDeposits = new Dictionary<string, decimal>();

        //                    for (int j = i; j < reverseGroups.Count; j++)
        //                    {
        //                        var remainingGroup = reverseGroups[j];
        //                        string remainingUser = remainingGroup.UserName;
        //                        decimal remainingAmount = remainingGroup.TotalAmount;

        //                        totalRemainingDeposits += remainingAmount;

        //                        if (!userRemainingDeposits.ContainsKey(remainingUser))
        //                            userRemainingDeposits[remainingUser] = 0m;
        //                        userRemainingDeposits[remainingUser] += remainingAmount;
        //                    }

        //                    if (totalRemainingDeposits > 0)
        //                    {
        //                        // Distribute available funds proportionally among users
        //                        foreach (var userDeposit in userRemainingDeposits)
        //                        {
        //                            string depositUser = userDeposit.Key;
        //                            decimal userAmount = userDeposit.Value;
        //                            decimal userRatio = userAmount / totalRemainingDeposits;

        //                            decimal userShare = availableFunds * userRatio;

        //                            if (!accountUserContributions.ContainsKey(depositUser))
        //                                accountUserContributions[depositUser] = 0m;
        //                            accountUserContributions[depositUser] += userShare;

        //                            validatedAchieved += userShare;
        //                        }
        //                    }
        //                }
        //                break; // Stop on first withdrawal detection
        //            }
        //        }

        //        if (!validationFailed)
        //        {
        //            // If no validation failures, all deposits are valid
        //            foreach (var group in refGroups)
        //            {
        //                string groupUser = group.UserName;
        //                if (!accountUserContributions.ContainsKey(groupUser))
        //                    accountUserContributions[groupUser] = 0m;
        //                accountUserContributions[groupUser] += group.TotalAmount;
        //            }
        //            validatedAchieved = refGroups.Sum(g => g.TotalAmount);
        //        }

        //        validatedAchieved = Math.Max(0, validatedAchieved);
        //        totalDepositAchieved += validatedAchieved;

        //        // Add account contributions to user totals
        //        foreach (var userContribution in accountUserContributions)
        //        {
        //            string userName = userContribution.Key;
        //            decimal contribution = userContribution.Value;

        //            if (userValidatedAchieved.ContainsKey(userName))
        //            {
        //                userValidatedAchieved[userName] += contribution;
        //            }
        //            else
        //            {
        //                userValidatedAchieved[userName] = contribution;
        //            }
        //        }
        //    }

        //    // Now you have validated achieved per user in userValidatedAchieved dictionary
        //    // You can access each user's validated achieved amount like:
        //    foreach (var users in processUsers)
        //    {
        //        string userName = user.UserName.Trim();
        //        // Use TryGetValue instead of GetValueOrDefault
        //        decimal userValidatedAmount = 0m;
        //        if (userValidatedAchieved.TryGetValue(userName, out decimal validatedAmount))
        //        {
        //            userValidatedAmount = validatedAmount;
        //        }

        //        // Use userValidatedAmount for per-user calculations
        //        decimal userDepositTarget = user.DepositTargetAmount ?? 0m;
        //        decimal userRemaining = Math.Max(0, userDepositTarget - userValidatedAmount);
        //        decimal userProgress = userDepositTarget > 0 ? (userValidatedAmount / userDepositTarget) * 100 : 0;

        //        Console.WriteLine($"User: {userName}, Validated: {userValidatedAmount}, Target: {userDepositTarget}, Progress: {userProgress:F2}%");
        //    }

        //    // 4. Final calculations (unchanged)
        //    decimal totalRawDeposited = processDeposits.Sum(d => d.Amount);
        //    decimal totalWithdrawalImpact = totalRawDeposited - totalDepositAchieved;

        //    remainingAmountDeposit = Math.Max(0, totalDepositTarget - totalDepositAchieved);
        //    decimal planProgress = totalDepositTarget > 0 ? (totalDepositAchieved / totalDepositTarget) * 100 : 0;

        //    initialBalance = processDeposits
        //        .Where(d => d.Prev_Ini_Bal.HasValue)
        //        .Sum(d => d.Prev_Ini_Bal) ?? 0m;

        //    actualTargetDeposit = totalRawDeposited;


        //    // Merchant Data
        //    var merchantData = db.DepositMerchantAgenets
        //        .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim());

        //    if (merchantData.Any())
        //    {
        //        totalMerchantAchieved = merchantData
        //            .Sum(m => ((decimal?)m.AccountBalance ?? 0) - ((decimal?)m.IntialAccountBalance ?? 0) + ((decimal?)m.Target ?? 0));

        //        initialAmountMerchant = merchantData
        //            .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

        //        actualTargetMerchant = merchantData
        //            .Sum(d => (decimal?)d.Target ?? 0);

        //        uniqueMerchantCount = merchantData
        //            .Select(u => u.LinkAccount)
        //            .Distinct()
        //            .Count();

        //        remainingAmountMerchant = totalMerchantTarget - uniqueMerchantCount;


        //        if (remainingAmountMerchant < 0)
        //        {
        //            remainingAmountMerchant = 0;
        //        }
        //    }

        //    // FCY Data
        //    var fcyData = db.DepositFCies
        //        .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim());

        //    if (fcyData.Any())
        //    {
        //        totalFCYAchieved = fcyData
        //            .Sum(f => (decimal?)f.TransactionAmount ?? 0);

        //        actualTargetFcy = fcyData
        //            .Sum(d => (decimal?)d.Target ?? 0);

        //        remainingAmountFcy = totalFCYTarget - totalFCYAchieved;


        //        if (remainingAmountFcy < 0)
        //        {
        //            remainingAmountFcy = 0;
        //        }
        //    }

        //    // Calculate progress
        //    //var planProgress = totalDepositTarget > 0 ? (totalDepositAchieved / totalDepositTarget) * 100 : 0;?
        //    var merchantProgress = totalMerchantTarget > 0 ? (uniqueMerchantCount / totalMerchantTarget) * 100 : 0;
        //    var fcyProgress = totalFCYTarget > 0 ? (totalFCYAchieved / totalFCYTarget) * 100 : 0;



        //    // ———————————————————————————————————————
        //    // PASS TO VIEW
        //    // ———————————————————————————————————————
        //    ViewBag.DepositPlanTarget = totalDepositTarget;
        //    ViewBag.DepositPlanAchieved = Math.Round(totalDepositAchieved, 2);
        //    ViewBag.DepositRemainingAmount = Math.Round(remainingAmountDeposit, 2);
        //    ViewBag.DepositPlanProgress = Math.Round(planProgress, 2);

        //    ViewBag.TotalRawDeposited = Math.Round(totalRawDeposited, 2);
        //    ViewBag.WithdrawalImpact = Math.Round(totalWithdrawalImpact, 2); // Very useful KPI!
        //    ViewBag.DepositInitialAmount = Math.Round(initialBalance, 2);
        //    ViewBag.DepositActualTarget = Math.Round(actualTargetDeposit, 2);


        //    // Pass to view
        //    //ViewBag.DepositPlanTarget = totalDepositTarget;
        //    //ViewBag.DepositPlanAchieved = totalDepositAchieved;
        //    //ViewBag.DepositRemainingAmount = remainingAmountDeposit;
        //    //ViewBag.DepositActualTarget = actualTargetDeposit;
        //    //ViewBag.DepositInitialAmount = initialBalance;
        //    //ViewBag.DepositPlanProgress = Math.Round(planProgress, 2);

        //    ViewBag.MerchantTarget = totalMerchantTarget;
        //    ViewBag.MerchantAchieved = totalMerchantAchieved;
        //    ViewBag.MerchantRemainingAmount = remainingAmountMerchant;
        //    ViewBag.MerchantActualTarget = actualTargetMerchant;
        //    ViewBag.MerchantInitialAmount = initialAmountMerchant;
        //    ViewBag.MerchantProgress = Math.Round(merchantProgress, 2);

        //    ViewBag.FCYTarget = totalFCYTarget;
        //    ViewBag.FCYAchieved = totalFCYAchieved;
        //    ViewBag.FCYRemainingAmount = remainingAmountFcy;
        //    ViewBag.FCYActualTarget = actualTargetFcy;
        //    ViewBag.FCYProgress = Math.Round(fcyProgress, 2);

        //    return View();
        //}

        public ActionResult UnAuthorized()
        {
            return View();
        }
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}