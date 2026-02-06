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
    public class FcyReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // ======================== VIEWS ========================
        public ActionResult FcyAchievementReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult FcyProcessReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult FcyDistrictReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult FcySubprocessReport()
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
        // 1. Individual FCY Achievement Report (Smart – role-based filtering)
        [HttpPost]
        public ActionResult SmartFcyAchievementAjax()
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

            // Base query from Users with FCY targets
            var query = db.Users
                .GroupJoin(db.DepositFCies,
                           u => u.ID,
                           d => d.UserID,
                           (u, deposits) => new
                           {
                               UserName = u.UserName,
                               FullName = u.FullName ?? u.UserName,
                               Process = u.Process ?? "-",
                               District = u.District ?? "-",
                               Branch = u.Branch ?? "-",
                               Position = u.Postion ?? "-", // Note: typo in model (should be Position)
                               UserTarget = u.FCYTargetAmount ?? 0m,
                               AchievedAmount = deposits.Sum(d => (decimal?)d.TransactionAmount) ?? 0m
                           })
                .Select(ma => new
                {
                    ma.UserName,
                    ma.FullName,
                    ma.Process,
                    ma.District,
                    ma.Branch,
                    ma.Position,
                    ma.UserTarget,
                    ma.AchievedAmount,
                    AchievedPercent = ma.UserTarget > 0
                        ? Math.Round((ma.AchievedAmount / ma.UserTarget) * 100m, 2)
                        : 0m   // or you can use null / "-" later in view if preferred
    })
                .AsQueryable();

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
                .OrderBy(x => x.District)
                .ThenBy(x => x.Branch)
                .ThenBy(x => x.FullName)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            // Grand totals
            var totals = query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTarget = g.Sum(x => x.UserTarget),
                    TotalAchieved = g.Sum(x => x.AchievedAmount)
                })
                .FirstOrDefault() ?? new { TotalTarget = 0m, TotalAchieved = 0m };

            decimal grandTotalPercentage = totals.TotalTarget > 0
                ? Math.Round((totals.TotalAchieved / totals.TotalTarget) * 100m, 1)
                : 0m;

            var results = pagedData.Select(r => new
            {
                r.FullName,
                r.Process,
                r.District,
                r.Branch,
                r.Position,
                Target = r.UserTarget,
                AchievedAmount = r.AchievedAmount,
                Percentage = Math.Round(r.AchievedPercent, 2)
            }).ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = totals.TotalTarget.ToString("N2"),
                grandTotalAchieved = totals.TotalAchieved.ToString("N2"),
                grandTotalPercentage = grandTotalPercentage
            }, JsonRequestBehavior.AllowGet);
        }

        // 2. By Process
        [HttpPost]
        public ActionResult FcyProcessAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var query = db.Users
                .Where(u => !string.IsNullOrEmpty(u.Process))
                .GroupBy(u => u.Process.Trim())
                .Select(g => new
                {
                    Process = g.Key,
                    TotalTarget = g.Sum(u => u.FCYTargetAmount ?? 0m),
                    TotalAchieved = g.SelectMany(u => u.DepositFCies).Sum(d => (decimal?)d.TransactionAmount) ?? 0m
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
            decimal grandTarget = 0m, grandAchieved = 0m;

            foreach (var p in data)
            {
                decimal percentage = p.TotalTarget > 0
                    ? Math.Round((p.TotalAchieved / p.TotalTarget) * 100m, 2)
                    : 0m;

                results.Add(new
                {
                    No = displayIndex++,
                    Process = p.Process ?? "Unassigned",
                    Target = p.TotalTarget,
                    AchievedAmount = p.TotalAchieved,
                    Percentage = percentage
                });

                grandTarget += p.TotalTarget;
                grandAchieved += p.TotalAchieved;
            }

            decimal grandPercentage = grandTarget > 0
                ? Math.Round((grandAchieved / grandTarget) * 100m, 2)
                : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = grandTarget.ToString("N2"),
                grandTotalAchieved = grandAchieved.ToString("N2"),
                grandTotalPercentage = grandPercentage
            }, JsonRequestBehavior.AllowGet);
        }

        // 3. By District (Operation Management Office only)
        [HttpPost]
        public ActionResult FcyDistrictAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var data = db.Users
                .Where(u => u.Process == "Growth and Operation" &&                    
                    !string.IsNullOrEmpty(u.District) &&
                    u.District.Contains("District") &&                    // LIKE '%District%'
                    u.District != "District Follow Up" &&                  // NOT IN
                    u.District != "District Support")
                .GroupBy(u => u.District.Trim())
                .Select(g => new
                {
                    District = g.Key,
                    TotalTarget = g.Sum(u => u.FCYTargetAmount ?? 0m),
                    TotalAchieved = g.SelectMany(u => u.DepositFCies).Sum(d => (decimal?)d.TransactionAmount) ?? 0m
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildFcyResponse(draw, data, "ALL DISTRICTS", start, length);
        }

        // 4. By Subprocess (Head Office Units)
        [HttpPost]
        public ActionResult FcySubprocessAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var data = db.Users
                .Where(u => !string.IsNullOrEmpty(u.District) &&
                            (
                                !u.District.Contains("District") ||      // NOT a real district
                                u.District == "District Follow Up" ||     // Explicit subprocess
                                u.District == "District Support"
                            )                   
                    )
                .GroupBy(u => u.District.Trim())
                .Select(g => new
                {
                    District = g.Key, // Reused as Subprocess name
                    TotalTarget = g.Sum(u => u.FCYTargetAmount ?? 0m),
                    TotalAchieved = g.SelectMany(u => u.DepositFCies).Sum(d => (decimal?)d.TransactionAmount) ?? 0m
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildFcyResponse(draw, data, "ALL HEAD OFFICE UNITS", start, length);
        }

        private JsonResult BuildFcyResponse(string draw, IEnumerable<dynamic> data, string totalLabel, int start = 0, int length = 10)
        {
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;
            var pagedData = data.Skip(skip).Take(pageSize).ToList();
            int totalRecords = data.Count();

            var results = new List<object>();
            int displayIndex = skip + 1;
            decimal grandTarget = 0m, grandAchieved = 0m;

            foreach (var d in pagedData)
            {
                decimal percentage = d.TotalTarget > 0
                    ? Math.Round((d.TotalAchieved / d.TotalTarget) * 100m, 2)
                    : 0m;

                results.Add(new
                {
                    No = displayIndex++,
                    Name = d.District ?? "Unassigned",
                    Target = d.TotalTarget,
                    AchievedAmount = d.TotalAchieved,
                    Percentage = percentage
                });

                grandTarget += d.TotalTarget;
                grandAchieved += d.TotalAchieved;
            }

            decimal grandPercentage = grandTarget > 0
                ? Math.Round((grandAchieved / grandTarget) * 100m, 2)
                : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = grandTarget.ToString("N2"),
                grandTotalAchieved = grandAchieved.ToString("N2"),
                grandTotalPercentage = grandPercentage
            }, JsonRequestBehavior.AllowGet);
        }
    }
}