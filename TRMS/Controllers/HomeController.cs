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
        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        public ActionResult Index()
        {
            // user limit 
            if (Session["UserName"] == null)
            {
                return RedirectToAction("login", "User");
            }
            else
            {
                string UserName = Session["UserName"].ToString();

                string role = Session["UserRole"].ToString();
                string position = Session["Position"].ToString();
                if (role == "Maker" || role == "Admin")
                {
                    //string branch = Session["UserHomeBranch"].ToString();
                    decimal AssinedTargetdeposit = db.Users.Where(u => u.UserName.Trim() == UserName.Trim()).Select(d => (decimal?)d.DepositTargetAmount).Sum() ?? 0;
                    ViewBag.AssinedTargetdeposit = AssinedTargetdeposit;

                    decimal depositplan = db.DepositPlans.Where(u => u.User == UserName).Select(d => (decimal?)d.Amount).Sum() ?? 0;
                    ViewBag.depositplan = depositplan;

                    decimal AaccountountIntial = db.DepositPlans.Where(u => u.User == UserName).Sum(u => (decimal?)u.IntialAccountBalance) ?? 0;
                    ViewBag.AaccountountIntial = AaccountountIntial;
                    //achived target
                    //acount blance
                    decimal acountBlance = db.DepositPlans.Where(u => u.User == UserName).Sum(u => (decimal?)u.AccountBalance) ?? 0;
                    ViewBag.acountBlance = acountBlance;
                    decimal blanceDif = acountBlance - AaccountountIntial;

                    decimal achived = AssinedTargetdeposit - depositplan;
                    if (achived < 0)
                    {
                        ViewBag.achivedDeposit = depositplan;
                        ViewBag.remaing = 0;
                    }
                    else
                    {
                        ViewBag.remaing = AssinedTargetdeposit - depositplan;
                        ViewBag.achivedDeposit = depositplan;
                    }

                    // daily progress report for deposit
                    DateTime startDate = new DateTime(2025, 10, 1);
                    DateTime today = DateTime.Today;
                    int totalDays = 90;
                    // Calculate how many days have passed
                    int daysElapsed = (today - startDate).Days;
                    if (daysElapsed < 0) daysElapsed = 0;
                    if (daysElapsed > totalDays) daysElapsed = totalDays;

                    decimal totalTarget = AssinedTargetdeposit;
                    // Expected amount to collect by today
                    decimal expected = (totalTarget / totalDays) * daysElapsed;

                    // Actual collected from DB
                    decimal actualCollected = depositplan;

                    // Progress percentage
                    decimal progressPercent = totalTarget == 0 ? 0 : (actualCollected / totalTarget) * 100;

                    // Color logic
                    string color = actualCollected < expected ? "bg-danger" : "bg-success";
                    if (actualCollected >= expected)
                    {
                        ViewBag.MessageClass = "alert-success";
                        ViewBag.MessageText = "✅ Great job!  on track with your target.";
                    }
                    else
                    {
                        ViewBag.MessageClass = "alert-danger";
                        ViewBag.MessageText = "⚠️ Warning: You are behind the expected progress.";
                    }


                    //user branch progress
                    string branch = Session["UserHomeBranch"]?.ToString() ?? "";
                    decimal branchProgressDep = 0;
                    decimal avargaeUserProgressDeposit = 0;
                    if (branch != null || branch != "")
                    {
                        
                        // Calculate progress for deposit 
                        decimal branchTarget = db.Users.Where(u => u.Branch.Trim() == branch.Trim()).Select(d => (decimal?)d.DepositTargetAmount).Sum() ?? 0;
                        decimal branchAchieved = db.DepositPlans.Where(u => u.Branch.Trim() == branch.Trim()).Select(d => (decimal?)d.Amount).Sum() ?? 0;
                        branchProgressDep = branchTarget > 0 ? (branchAchieved / branchTarget) * 100 : 0;
                        avargaeUserProgressDeposit = (progressPercent + branchProgressDep) / 2;
                    }

                    // Pass values to view
                    ViewBag.ProgressPercent = progressPercent;
                    ViewBag.avargaeUserProgressDeposit = avargaeUserProgressDeposit;
                    ViewBag.Color = color;
                    ViewBag.Collected = actualCollected;
                    ViewBag.Expected = expected;
                    ViewBag.DaysElapsed = daysElapsed;


                    // for agenet and merchant 

                    decimal AssinedTargetAgent = db.Users.Where(u => u.UserName == UserName.Trim()).Select(d => d.MerchantTarget).Sum() ?? 0;
                    ViewBag.AssinedTargetAgent = AssinedTargetAgent;

                    decimal depositplanAgent = db.DepositMerchantAgenets.Where(u => u.CreatedBY == UserName).Select(d => (decimal?)d.Target).Sum() ?? 0;
                    ViewBag.depositplanAgent = depositplanAgent;
                    // account baalace for 
                    decimal AaccountountIntialAgent = db.DepositMerchantAgenets.Where(u => u.CreatedBY == UserName).Sum(u => (decimal?)u.IntialAccountBalance) ?? 0;
                    ViewBag.AaccountountIntialAgent = AaccountountIntialAgent;



                    int uniqueMerchantCount = db.DepositMerchantAgenets.Where(u => u.CreatedBY == UserName).Select(u => u.LinkAccount).Distinct().Count();
                    ViewBag.uniqueMerchantCount = uniqueMerchantCount;


                    //achived target
                    decimal acountBlancemercht = db.DepositMerchantAgenets.Where(u => u.CreatedBY == UserName).Sum(u => (decimal?)u.AccountBalance) ?? 0;
                    ViewBag.acountBlancemercht = acountBlancemercht;

                    decimal diffagent = AssinedTargetAgent - uniqueMerchantCount;
                    if (diffagent < 0)
                    {
                        ViewBag.achivedDepositAgenet = uniqueMerchantCount;
                        ViewBag.remaing1 = 0;
                    }
                    else
                    {
                        ViewBag.remaing1 = AssinedTargetAgent - uniqueMerchantCount;
                        ViewBag.achivedDepositAgenet = uniqueMerchantCount;
                    }


                    // daily progress report for Merchant and Agent

                    decimal totalTargetAgent = AssinedTargetAgent;
                    // Expected amount to collect by today
                    decimal expectedAgent = (totalTargetAgent / totalDays) * daysElapsed;

                    // Actual collected from DB
                    decimal actualCollectedAgent = uniqueMerchantCount;

                    // Progress percentage
                    decimal progressPercentAgent = totalTargetAgent == 0 ? 0 : (actualCollectedAgent / totalTargetAgent) * 100;

                    // Color logic
                    string colorAgent = actualCollectedAgent < expectedAgent ? "bg-danger" : "bg-success";
                    if (actualCollectedAgent >= expectedAgent)
                    {
                        ViewBag.MessageClass1 = "alert-success";
                        ViewBag.MessageText1 = "✅ Great job!  on track with your target.";
                    }
                    else
                    {
                        ViewBag.MessageClass1 = "alert-danger";
                        ViewBag.MessageText1 = "⚠️ Warning: You are behind the expected progress.";
                    }


                    // Calculate progress for Merchant 
                    decimal branchProgressMer = 0;
                    decimal avargaeUserProgressMerchant = 0;

                    if (branch != null || branch != "")
                    {

                        decimal branchTargetMer = db.Users.Where(u => u.Branch.Trim() == branch.Trim()).Select(d => (decimal?)d.MerchantTarget).Sum() ?? 0;
                        int branchAchievedmerc = db.DepositMerchantAgenets.Where(u => u.Branch == branch.Trim()).Select(u => u.LinkAccount).Distinct().Count();
                        branchProgressMer = branchTargetMer > 0 ? (branchAchievedmerc / branchTargetMer) * 100 : 0;
                        avargaeUserProgressMerchant = (progressPercentAgent + branchProgressMer) / 2;
                    }

                    // Pass values to view
                    ViewBag.progressPercentAgent = progressPercentAgent;
                    ViewBag.avargaeUserProgressMerchant = avargaeUserProgressMerchant;
                    ViewBag.colorAgent = colorAgent;
                    ViewBag.CollectedAgent = actualCollectedAgent;
                    ViewBag.ExpectedAgent = expectedAgent;
                    ViewBag.DaysElapsed = daysElapsed;



                    // fcy deposit 

                    decimal AssinedTargetFCY = db.Users.Where(u => u.UserName.Trim() == UserName.Trim()).Select(d => (decimal?)d.FCYTargetAmount).Sum() ?? 0;
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
                        decimal branchTargetFcy = db.Users.Where(u => u.Branch.Trim() == branch.Trim()).Select(d => (decimal?)d.FCYTargetAmount).Sum() ?? 0;
                        decimal branchAchievedFcy = db.DepositFCies.Where(u => u.Branch.Trim() == branch.Trim()).Select(d => (decimal?)d.TransactionAmount).Sum() ?? 0;
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


        public ActionResult TvDashboard()
        {
            try
            {
                DateTime startDate = new DateTime(2025, 4, 1);
                DateTime today = DateTime.Today;
                int totalDays = 90;
                int daysElapsed = Math.Max(0, Math.Min((today - startDate).Days, totalDays));

                // Deposits
                decimal assignedTargetDeposit = db.DistrictPlans.Sum(d => (decimal?)d.Deposit) ?? 0;
                decimal depositPlan = db.DepositPlans.Sum(d => (decimal?)d.Amount) ?? 0;
                decimal accountInitial = db.DepositPlans.Sum(u => (decimal?)u.IntialAccountBalance) ?? 0;
                decimal achievedDeposit = db.DepositPlans.Sum(u => (decimal?)u.AccountBalance) ?? 0;
                var depositProgress = CalculateProgress(assignedTargetDeposit, achievedDeposit, accountInitial, daysElapsed, totalDays);

                // Agents
                decimal assignedTargetAgent = db.DistrictPlans.Sum(d => (decimal?)d.merchant) ?? 0;
                decimal depositPlanAgent = db.DepositMerchantAgenets.Sum(d => (decimal?)d.Target) ?? 0;
                decimal accountInitialAgent = db.DepositMerchantAgenets.Sum(u => (decimal?)u.IntialAccountBalance) ?? 0;
                decimal achievedDepositAgent = db.DepositMerchantAgenets.Sum(u => (decimal?)u.AccountBalance) ?? 0;
                var agentProgress = CalculateProgress(assignedTargetAgent, achievedDepositAgent, accountInitialAgent, daysElapsed, totalDays);

                // FCY
                decimal assignedTargetFCY = db.DistrictPlans.Sum(d => (decimal?)d.FCY) ?? 0;
                decimal depositPlanFCY = db.Fcy_Branch.Sum(d => (decimal?)d.FcyTargetAmount) ?? 0;
                decimal achievedDepositFCY = db.DepositFCies.Sum(u => (decimal?)u.TransactionAmount) ?? 0;
                var fcyProgress = CalculateProgress(assignedTargetFCY, achievedDepositFCY, 0, daysElapsed, totalDays);

                var responseData = new
                {
                    success = true,
                    Deposits = new
                    {
                        AssignedTarget = assignedTargetDeposit,
                        Plan = depositPlan,
                        Initial = accountInitial,
                        Achieved = depositProgress.Achieved,
                        Remaining = depositProgress.Remaining,
                        ProgressPercent = depositProgress.ProgressPercent,
                        MessageClass = depositProgress.MessageClass,
                        MessageText = depositProgress.MessageText,
                        Collected = depositProgress.Achieved,
                        Expected = depositProgress.Expected,
                        DaysElapsed = daysElapsed
                    },
                    Agents = new
                    {
                        AssignedTarget = assignedTargetAgent,
                        Plan = depositPlanAgent,
                        Initial = accountInitialAgent,
                        Achieved = agentProgress.Achieved,
                        Remaining = agentProgress.Remaining,
                        ProgressPercent = agentProgress.ProgressPercent,
                        MessageClass = agentProgress.MessageClass,
                        MessageText = agentProgress.MessageText,
                        Collected = agentProgress.Achieved,
                        Expected = agentProgress.Expected
                    },
                    FCY = new
                    {
                        AssignedTarget = assignedTargetFCY,
                        Plan = depositPlanFCY,
                        Achieved = fcyProgress.Achieved,
                        Remaining = fcyProgress.Remaining,
                        ProgressPercent = fcyProgress.ProgressPercent,
                        MessageClass = fcyProgress.MessageClass,
                        MessageText = fcyProgress.MessageText,
                        Collected = fcyProgress.Achieved,
                        Expected = fcyProgress.Expected
                    }
                };

                return Json(responseData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Dashboard()
        {

            // Get current logged-in user (adjust to your login system)
            var currentUserName = Session["UserName"];
            User user = db.Users.FirstOrDefault(u => u.UserName == currentUserName);

            //if (user == null)
            //    return RedirectToAction("Login", "Account"); // or handle unauthorized

            string role = user.Role;
            string userBranch = user.Branch;
            string userDistrict = user.Branch;
            string userPosition = user.Postion;

            decimal totalDepositTarget = 0, totalMerchantTarget = 0, totalFCYTarget = 0;
            decimal totalDepositAchieved = 0, totalMerchantAchieved = 0, totalFCYAchieved = 0;
            decimal initialAmountDeposit = 0, initialAmountMerchant = 0;
            decimal actualTargetDeposit = 0, actualTargetMerchant = 0, actualTargetFcy = 0;
            decimal remainingAmountDeposit = 0, remainingAmountMerchant = 0, remainingAmountFcy = 0;
            int uniqueMerchantCount=0;

            if (role == "Maker" && (userPosition == "Manager" || userPosition == "BranchManager"))
            {
                // Show everything
                ViewBag.Branch = user.Branch;
                totalDepositTarget = db.Users.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(u => (decimal?)u.DepositTargetAmount) ?? 0;
                totalMerchantTarget = db.Users.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(u => (decimal?)u.MerchantTarget) ?? 0;
                totalFCYTarget = db.Users.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

                var depositdata = db.DepositPlans.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim());

                if (depositdata.Any())
                {
                    totalDepositAchieved = db.DepositPlans.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.AccountBalance - d.IntialAccountBalance + d.Amount);
                    //Get Total Initial, Achieved, Remaining for deposit
                    initialAmountDeposit = db.DepositPlans.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.IntialAccountBalance);
                    actualTargetDeposit = db.DepositPlans.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.Amount);
                    remainingAmountDeposit = totalDepositTarget - totalDepositAchieved;
                    if(remainingAmountDeposit<0)
                    {
                        remainingAmountDeposit = 0;
                    }
                        
                }
                var merchantData = db.DepositMerchantAgenets
                    .Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim());
                if (merchantData.Any())
                {
                    totalMerchantAchieved = merchantData.Sum(m => m.AccountBalance - m.IntialAccountBalance +  m.Target );
                    //Get Total Initial, Achieved, Remaining for Merchant
                    initialAmountMerchant = db.DepositMerchantAgenets.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.IntialAccountBalance);
                    actualTargetMerchant = db.DepositMerchantAgenets.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.Target);
                    uniqueMerchantCount = db.DepositMerchantAgenets.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Select(u => u.LinkAccount).Distinct().Count();
                    remainingAmountMerchant = totalMerchantTarget - uniqueMerchantCount;

                    if (remainingAmountMerchant < 0)
                    {
                        remainingAmountMerchant = 0;
                    }
                }

                var fcyData = db.DepositFCies.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim());

                if (fcyData.Any())
                {
                    totalFCYAchieved = db.DepositFCies.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(f => f.TransactionAmount);
                    //Get Total Initial, Achieved, Remaining for Fcy
                    actualTargetFcy = db.DepositFCies.Where(u => u.Branch != null && u.Branch.Trim() == user.Branch.Trim()).Sum(d => d.Target);
                    remainingAmountFcy = totalFCYTarget - totalFCYAchieved;

                    if (remainingAmountFcy < 0)
                    {
                        remainingAmountFcy = 0;
                    }
                }
            }
            // For Directors
            else if (role == "Maker" && (userPosition == "Director"))
            {
                // Show everything
                ViewBag.District = user.District;

                // Total Targets
                totalDepositTarget = db.Users
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim())
                    .Sum(u => (decimal?)u.DepositTargetAmount) ?? 0;

                totalMerchantTarget = db.Users
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim())
                    .Sum(u => (decimal?)u.MerchantTarget) ?? 0;

                totalFCYTarget = db.Users
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim())
                    .Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

                // Deposit Data
                var depositData = db.DepositPlans
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim());

                if (depositData.Any())
                {
                    totalDepositAchieved = depositData
                        .Sum(d => ((decimal?)d.AccountBalance ?? 0) - ((decimal?)d.IntialAccountBalance ?? 0) + ((decimal?)d.Amount ?? 0));

                    initialAmountDeposit = depositData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetDeposit = depositData
                        .Sum(d => (decimal?)d.Amount ?? 0);

                    remainingAmountDeposit = totalDepositTarget - totalDepositAchieved;

                    if (remainingAmountDeposit < 0)
                    {
                        remainingAmountDeposit = 0;
                    }

                }

                // Merchant Data
                var merchantData = db.DepositMerchantAgenets
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim());

                if (merchantData.Any())
                {
                    totalMerchantAchieved = merchantData
                        .Sum(m => ((decimal?)m.AccountBalance ?? 0) - ((decimal?)m.IntialAccountBalance ?? 0) + ((decimal?)m.Target ?? 0));

                    initialAmountMerchant = merchantData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetMerchant = merchantData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    uniqueMerchantCount = merchantData
                        .Select(u => u.LinkAccount)
                        .Distinct()
                        .Count();

                    remainingAmountMerchant = totalMerchantTarget - uniqueMerchantCount;

                    if (remainingAmountMerchant < 0)
                    {
                        remainingAmountMerchant = 0;
                    }
                }

                // FCY Data
                var fcyData = db.DepositFCies
                    .Where(u => u.District != null && u.District.Trim() == user.District.Trim());

                if (fcyData.Any())
                {
                    totalFCYAchieved = fcyData
                        .Sum(f => (decimal?)f.TransactionAmount ?? 0);

                    actualTargetFcy = fcyData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    remainingAmountFcy = totalFCYTarget - totalFCYAchieved;


                    if (remainingAmountFcy < 0)
                    {
                        remainingAmountFcy = 0;
                    }
                }
            }

            // For VP and CHief
            else if (role == "Maker" && (userPosition == "VP" || userPosition == "CHF"))
            {
                // Show everything
                ViewBag.VP = user.Process;

                // Total Targets
                totalDepositTarget = db.Users
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim())
                    .Sum(u => (decimal?)u.DepositTargetAmount) ?? 0;

                totalMerchantTarget = db.Users
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim())
                    .Sum(u => (decimal?)u.MerchantTarget) ?? 0;

                totalFCYTarget = db.Users
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim())
                    .Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

                // Deposit Data
                var depositData = db.DepositPlans
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim());

                if (depositData.Any())
                {
                    totalDepositAchieved = depositData
                        .Sum(d => ((decimal?)d.AccountBalance ?? 0) - ((decimal?)d.IntialAccountBalance ?? 0) + ((decimal?)d.Amount ?? 0));

                    initialAmountDeposit = depositData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetDeposit = depositData
                        .Sum(d => (decimal?)d.Amount ?? 0);

                    remainingAmountDeposit = totalDepositTarget - totalDepositAchieved;


                    if (remainingAmountDeposit < 0)
                    {
                        remainingAmountDeposit = 0;
                    }
                }

                // Merchant Data
                var merchantData = db.DepositMerchantAgenets
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim());

                if (merchantData.Any())
                {
                    totalMerchantAchieved = merchantData
                        .Sum(m => ((decimal?)m.AccountBalance ?? 0) - ((decimal?)m.IntialAccountBalance ?? 0) + ((decimal?)m.Target ?? 0));

                    initialAmountMerchant = merchantData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetMerchant = merchantData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    uniqueMerchantCount = merchantData
                        .Select(u => u.LinkAccount)
                        .Distinct()
                        .Count();

                    remainingAmountMerchant = totalMerchantTarget - uniqueMerchantCount;


                    if (remainingAmountMerchant < 0)
                    {
                        remainingAmountMerchant = 0;
                    }
                }

                // FCY Data
                var fcyData = db.DepositFCies
                    .Where(u => u.Process != null && u.Process.Trim() == user.Process.Trim());

                if (fcyData.Any())
                {
                    totalFCYAchieved = fcyData
                        .Sum(f => (decimal?)f.TransactionAmount ?? 0);

                    actualTargetFcy = fcyData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    remainingAmountFcy = totalFCYTarget - totalFCYAchieved;


                    if (remainingAmountFcy < 0)
                    {
                        remainingAmountFcy = 0;
                    }
                }
            }

            // For CEO
            else if (role == "Maker" && (userPosition == "CEO"))
            {
                // Show everything
                ViewBag.CEO = "COOPBANK";

                // Total Targets
                totalDepositTarget = db.Users
                    .Sum(u => (decimal?)u.DepositTargetAmount) ?? 0;

                totalMerchantTarget = db.Users
                    .Sum(u => (decimal?)u.MerchantTarget) ?? 0;

                totalFCYTarget = db.Users
                    .Sum(u => (decimal?)u.FCYTargetAmount) ?? 0;

                // Deposit Data
                var depositData = db.DepositPlans;

                if (depositData.Any())
                {
                    totalDepositAchieved = depositData
                        .Sum(d => ((decimal?)d.AccountBalance ?? 0) - ((decimal?)d.IntialAccountBalance ?? 0) + ((decimal?)d.Amount ?? 0));

                    initialAmountDeposit = depositData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetDeposit = depositData
                        .Sum(d => (decimal?)d.Amount ?? 0);

                    remainingAmountDeposit = totalDepositTarget - totalDepositAchieved;

                    if (remainingAmountDeposit < 0)
                    {
                        remainingAmountDeposit = 0;
                    }
                }

                // Merchant Data
                var merchantData = db.DepositMerchantAgenets;

                if (merchantData.Any())
                {
                    totalMerchantAchieved = merchantData
                        .Sum(m => ((decimal?)m.AccountBalance ?? 0) - ((decimal?)m.IntialAccountBalance ?? 0) + ((decimal?)m.Target ?? 0));

                    initialAmountMerchant = merchantData
                        .Sum(d => (decimal?)d.IntialAccountBalance ?? 0);

                    actualTargetMerchant = merchantData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    uniqueMerchantCount = merchantData
                        .Select(u => u.LinkAccount)
                        .Distinct()
                        .Count();

                    remainingAmountMerchant = totalMerchantTarget - uniqueMerchantCount;

                    if (remainingAmountMerchant < 0)
                    {
                        remainingAmountMerchant = 0;
                    }
                }

                // FCY Data
                var fcyData = db.DepositFCies;

                if (fcyData.Any())
                {
                    totalFCYAchieved = fcyData
                        .Sum(f => (decimal?)f.TransactionAmount ?? 0);

                    actualTargetFcy = fcyData
                        .Sum(d => (decimal?)d.Target ?? 0);

                    remainingAmountFcy = totalFCYTarget - totalFCYAchieved;

                    if (remainingAmountFcy < 0)
                    {
                        remainingAmountFcy = 0;
                    }
                }
            }


            // Calculate progress
            var planProgress = totalDepositTarget > 0 ? (totalDepositAchieved / totalDepositTarget) * 100 : 0;
            var merchantProgress = totalMerchantTarget > 0 ? (uniqueMerchantCount / totalMerchantTarget) * 100 : 0;
            var fcyProgress = totalFCYTarget > 0 ? (totalFCYAchieved / totalFCYTarget) * 100 : 0;

            // Pass to view
            ViewBag.DepositPlanTarget = totalDepositTarget;
            ViewBag.DepositPlanAchieved = totalDepositAchieved;
            ViewBag.DepositRemainingAmount = remainingAmountDeposit;
            ViewBag.DepositActualTarget = actualTargetDeposit;
            ViewBag.DepositInitialAmount = initialAmountDeposit;
            ViewBag.DepositPlanProgress = Math.Round(planProgress, 2);

            ViewBag.MerchantTarget = totalMerchantTarget;
            ViewBag.MerchantAchieved = totalMerchantAchieved;
            ViewBag.MerchantRemainingAmount = remainingAmountMerchant;
            ViewBag.MerchantActualTarget = actualTargetMerchant;
            ViewBag.MerchantInitialAmount = initialAmountMerchant;
            ViewBag.MerchantProgress = Math.Round(merchantProgress, 2);

            ViewBag.FCYTarget = totalFCYTarget;
            ViewBag.FCYAchieved = totalFCYAchieved;
            ViewBag.FCYRemainingAmount = remainingAmountFcy;
            ViewBag.FCYActualTarget = actualTargetFcy;
            ViewBag.FCYProgress = Math.Round(fcyProgress, 2);


            return View();
        }

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