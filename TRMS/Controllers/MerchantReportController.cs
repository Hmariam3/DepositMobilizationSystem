using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;
using TRMS.Models.Report;

namespace TRMS.Controllers
{
    public class MerchantReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // ======================== VIEWS ========================

        public ActionResult MerchantAchievementReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult MerchantProcessReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult MerchantDistrictReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult MerchantSubprocessReport()
        {
            GetCommonViewBags();
            return View();
        }

        private void GetCommonViewBags()
        {
            ViewBag.District = new List<string> { "All" }
                .Concat(db.Users.Where(u => u.District != null)
                    .Select(u => u.District.Trim())
                    .Distinct()
                    .OrderBy(d => d))
                .ToList();

            ViewBag.CurrentUserRole = Session["UserRole"]?.ToString();
            ViewBag.CurrentUserPosition = Session["Position"]?.ToString() ?? "";
            ViewBag.CurrentUserProcess = Session["Process"]?.ToString() ?? "";
            ViewBag.CurrentUserDistrict = Session["District"]?.ToString() ?? "";
            ViewBag.CurrentUserBranch = Session["UserHomeBranch"]?.ToString() ?? "";
            ViewBag.CurrentUserFullName = Session["FullName"]?.ToString() ?? "";
            ViewBag.CurrentUserName = Session["UserName"]?.ToString() ?? "";
            ViewBag.ReportStatus = Session["ReportStatus"]?.ToString() ?? "";
        }

        // ======================== AJAX HANDLERS ========================

        // 1. Individual Merchant Achievement Report (Smart – role-based filtering)
        [HttpPost]
        public ActionResult SmartMerchantAchievementAjax()
        {
            var draw = int.Parse(Request.Form["draw"]);
            var start = int.Parse(Request.Form["start"]);
            var length = int.Parse(Request.Form["length"]);
            var searchValue = (Request.Form["search[value]"] ?? "").Trim().ToLower();
            var districtFilter = Request.Form["district"];
            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];
            var fixedBranch = Request.Form["fixedBranch"];
            var fixedPosition = Request.Form["fixedPosition"];
            var fixedUserName = Request.Form["fixedUserName"];

            // Base query from pre-calculated MerchantAchieved table
            var query = db.MerchantAchieveds.AsQueryable();

            // Role-based fixed filters
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                query = query.Where(ma => ma.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                query = query.Where(ma => ma.District == fixedDistrict);
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                query = query.Where(ma => ma.Branch == fixedBranch);
            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName))
                query = query.Where(ma => ma.UserName == fixedUserName);

            // Dynamic district dropdown filter
            if (!string.IsNullOrEmpty(districtFilter) && districtFilter != "All")
                query = query.Where(ma => ma.District.Trim() == districtFilter.Trim());

            // Only users with a merchant target
            query = query.Where(ma => (ma.UserTarget ?? 0) >= 0);

            // Global search
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(ma =>
                    (ma.UserName ?? "").ToLower().Contains(searchValue) ||
                    (ma.Process ?? "").ToLower().Contains(searchValue) ||
                    (ma.District ?? "").ToLower().Contains(searchValue) ||
                    (ma.Branch ?? "").ToLower().Contains(searchValue) ||
                    (ma.Position ?? "").ToLower().Contains(searchValue)
                );
            }

            int totalRecords = query.Count();

            // Safe paging (handles export with length = -1)
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;

            var pagedData = query
                .Join(db.Users,
                      ma => ma.UserName,
                      u => u.UserName,
                      (ma, u) => new { MA = ma, U = u })
                .OrderBy(x => x.MA.District)
                .ThenBy(x => x.MA.Branch)
                .ThenBy(x => x.U.FullName ?? x.MA.UserName)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new
                {
                    FullName = x.U.FullName ?? x.MA.UserName,
                    Process = x.MA.Process ?? "-",
                    District = x.MA.District ?? "-",
                    Branch = x.MA.Branch ?? "-",
                    Postion = x.MA.Position ?? "-",
                    Target = x.MA.UserTarget ?? 0,
                    CollectedAmount = x.MA.CollectedAmount ?? 0m,
                    AchievedMerchants = x.MA.AchievedMerchant ?? 0,
                    Percentage = x.MA.AchievedPercent ?? 0m
                })
                .ToList();

            // Grand totals
            // Grand totals
            var totals = query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    TotalCollected = g.Sum(x => x.CollectedAmount ?? 0m),
                    TotalAchieved = g.Sum(x => x.AchievedMerchant ?? 0)
                })
                .FirstOrDefault() ?? new { TotalTarget = 0, TotalCollected = 0m, TotalAchieved = 0 };

            // FIX: Cast to decimal before division to avoid integer truncation
            decimal grandTotalPercentage = totals.TotalTarget > 0
                ? Math.Round(((decimal)totals.TotalAchieved / totals.TotalTarget) * 100m, 1)
                : 0m;

            var results = pagedData.Select(r => new
            {
                r.FullName,
                r.Process,
                r.District,
                r.Branch,
                r.Postion,
                Target = r.Target,
                CollectedAmount = r.CollectedAmount,
                AchievedMerchants = r.AchievedMerchants,
                Percentage = Math.Round(r.Percentage, 2)
            }).ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = totals.TotalTarget.ToString("N0"),
                grandTotalCollected = totals.TotalCollected.ToString("N2"),
                grandTotalAchieved = totals.TotalAchieved.ToString("N0"),
                grandTotalPercentage = grandTotalPercentage
            }, JsonRequestBehavior.AllowGet);
        }

        // 2. By Process
        [HttpPost]
        public ActionResult MerchantProcessAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var query = db.MerchantAchieveds
                .Where(ma => ma.UserTarget >= 0 && !string.IsNullOrEmpty(ma.Process))
                .GroupBy(ma => ma.Process.Trim())
                .Select(g => new
                {
                    Process = g.Key,
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    TotalCollected = g.Sum(x => x.CollectedAmount ?? 0m),
                    TotalAchieved = g.Sum(x => x.AchievedMerchant ?? 0)
                });

            int totalRecords = query.Count();
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;

            var data = query
                .OrderBy(x => x.Process)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var results = new List<object>();
            int displayIndex = skip + 1;
            decimal grandTarget = 0, grandCollected = 0, grandAchieved = 0;

            foreach (var p in data)
            {
                // FIX: Cast to decimal to avoid integer division
                decimal percentage = p.TotalTarget > 0
                    ? Math.Round(((decimal)p.TotalAchieved / p.TotalTarget) * 100m, 2)
                    : 0m;


                results.Add(new
                {
                    No = displayIndex++,
                    Process = p.Process ?? "Unassigned",
                    Target = p.TotalTarget,
                    CollectedAmount = p.TotalCollected,
                    AchievedMerchants = p.TotalAchieved,
                    Percentage = percentage
                });

                grandTarget += p.TotalTarget;
                grandCollected += p.TotalCollected;
                grandAchieved += p.TotalAchieved;
            }

            // FIX: Same here for grand total
            decimal grandPercentage = grandTarget > 0
                ? Math.Round(((decimal)grandAchieved / grandTarget) * 100m, 2)
                : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = grandTarget.ToString("N0"),
                grandTotalCollected = grandCollected.ToString("N2"),
                grandTotalAchieved = grandAchieved.ToString("N0"),
                grandTotalPercentage = grandPercentage
            }, JsonRequestBehavior.AllowGet);
        }

        // 3. By District (Operation Management Office only)
        [HttpPost]
        public ActionResult MerchantDistrictAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var data = db.MerchantAchieveds
                .Where(ma =>
                    ma.UserTarget >= 0 &&
                    ma.Process == "Growth and Operation" &&
                    !string.IsNullOrEmpty(ma.District) &&
                    ma.District.Contains("District") &&                    // LIKE '%District%'
                    ma.District != "District Follow Up" &&                  // NOT IN
                    ma.District != "District Support")
                .GroupBy(ma => ma.District.Trim())
                .Select(g => new
                {
                    District = g.Key,
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    TotalCollected = g.Sum(x => x.CollectedAmount ?? 0m),
                    TotalAchieved = g.Sum(x => x.AchievedMerchant ?? 0)
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildMerchantResponse(draw, data, "ALL DISTRICTS", start, length);
        }

        // 4. By Subprocess (Head Office Units)
        [HttpPost]
        public ActionResult MerchantSubprocessAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var data = db.MerchantAchieveds
                .Where(ma =>
                    ma.UserTarget >= 0 &&
                    !string.IsNullOrEmpty(ma.District) &&
                        (
                            !ma.District.Contains("District") ||      // NOT a real district
                            ma.District == "District Follow Up" ||     // Explicit subprocess
                            ma.District == "District Support"
                        )
                    )
                .GroupBy(ma => ma.District.Trim())
                .Select(g => new
                {
                    District = g.Key, // Reused as Subprocess name
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    TotalCollected = g.Sum(x => x.CollectedAmount ?? 0m),
                    TotalAchieved = g.Sum(x => x.AchievedMerchant ?? 0)
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildMerchantResponse(draw, data, "ALL HEAD OFFICE UNITS", start, length);
        }

        private JsonResult BuildMerchantResponse(string draw, IEnumerable<dynamic> data, string totalLabel, int start = 0, int length = 10)
        {
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;
            var pagedData = data.Skip(skip).Take(pageSize).ToList();
            int totalRecords = data.Count();

            var results = new List<object>();
            int displayIndex = skip + 1;
            decimal grandTarget = 0, grandCollected = 0, grandAchieved = 0;

            foreach (var d in pagedData)
            {
                decimal percentage = d.TotalTarget > 0
                    ? Math.Round(((decimal)d.TotalAchieved / d.TotalTarget) * 100, 2)
                    : 0m;

                results.Add(new
                {
                    No = displayIndex++,
                    Name = d.District ?? "Unassigned",
                    Target = d.TotalTarget,
                    CollectedAmount = d.TotalCollected,
                    AchievedMerchants = d.TotalAchieved,
                    Percentage = percentage
                });

                grandTarget += d.TotalTarget;
                grandCollected += d.TotalCollected;
                grandAchieved += d.TotalAchieved;
            }

            decimal grandPercentage = grandTarget > 0
                ? Math.Round(((decimal)grandAchieved / grandTarget) * 100, 2)
                : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = grandTarget.ToString("N0"),
                grandTotalCollected = grandCollected.ToString("N2"),
                grandTotalAchieved = grandAchieved.ToString("N0"),
                grandTotalPercentage = grandPercentage
            }, JsonRequestBehavior.AllowGet);
        }
    }
}