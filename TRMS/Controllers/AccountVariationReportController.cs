using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;

namespace TRMS.Controllers
{
    public class AccountVariationReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // -------------------------------------------------------------------------
        //  VIEW ACTIONS (with common filters)
        // -------------------------------------------------------------------------
        public ActionResult UserVariationReport()
        {
            PrepareCommonViewBag();
            return View();
        }

        public ActionResult BranchVariationReport()
        {
            PrepareCommonViewBag();
            return View();
        }

        public ActionResult DistrictVariationReport()
        {
            PrepareCommonViewBag();
            return View();
        }

        private void PrepareCommonViewBag()
        {
            var districts = db.AccountMappings
                .Where(m => !string.IsNullOrEmpty(m.District))
                .Select(m => m.District.Trim())
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            ViewBag.Districts = new List<string> { "All" }.Concat(districts).ToList();

            ViewBag.CurrentUserRole = Session["UserRole"]?.ToString() ?? "";
            ViewBag.CurrentUserDistrict = Session["District"]?.ToString() ?? "";
            ViewBag.CurrentUserBranch = Session["UserHomeBranch"]?.ToString() ?? "";
            ViewBag.CurrentUserName = Session["UserName"]?.ToString() ?? "";
            ViewBag.CurrentUserFullName = Session["FullName"]?.ToString() ?? "";
            ViewBag.CurrentUserPosition = Session["Position"]?.ToString() ?? "";
            ViewBag.CurrentUserProcess = Session["Process"]?.ToString() ?? "";
            ViewBag.ReportStatus = Session["ReportStatus"]?.ToString() ?? "";
        }

        // -------------------------------------------------------------------------
        //  AJAX – USER LEVEL (my accounts or filtered users)
        // -------------------------------------------------------------------------
        [HttpPost]
        public JsonResult UserVariationAjax()
        {
            var draw = int.Parse(Request.Form["draw"] ?? "1");
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");
            var search = (Request.Form["search[value]"] ?? "").Trim().ToLower();

            var districtFilter = Request.Form["district"] ?? "All";
            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];
            var fixedBranch = Request.Form["fixedBranch"];
            var fixedUserName = Request.Form["fixedUserName"];

            // 1. Driving query from Users joined with AccountMappings
            // This ensures we get FullName from the User table.
            var usersQuery = db.Users.AsQueryable();

            // Apply fixed filters (role-based)
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                usersQuery = usersQuery.Where(u => u.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                usersQuery = usersQuery.Where(u => u.District == fixedDistrict);
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                usersQuery = usersQuery.Where(u => u.Branch == fixedBranch);
            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName))
                usersQuery = usersQuery.Where(u => u.UserName == fixedUserName);

            // Dynamic filter
            if (!string.IsNullOrEmpty(districtFilter) && districtFilter != "All")
                usersQuery = usersQuery.Where(u => u.District.Trim() == districtFilter.Trim());

            // 2. Perform the grouping and joining
            var grouped = usersQuery
                .GroupJoin(db.AccountMappings,
                           u => u.UserName,
                           m => m.UserName,
                           (u, mappings) => new
                           {
                               UserName = u.UserName,
                               FullName = u.FullName ?? u.UserName,
                               District = u.District ?? "-",
                               Branch = u.Branch ?? "-",
                               Mappings = mappings
                           })
                .Select(g => new
                {
                    g.UserName,
                    g.FullName,
                    g.District,
                    g.Branch,
                    AccountCount = g.Mappings.Count(),
                    SumBeginning = g.Mappings.Sum(x => x.BegginingBalance ?? 0m),
                    SumCurrent = g.Mappings.Sum(x => x.CurrentBalance ?? 0m),
                    AccountNumbers = g.Mappings.Select(x => x.AccountNumber ?? "N/A").ToList()
                })
                .Where(u => u.AccountCount > 0); // Only show users who actually have account mappings

            // Global search
            if (!string.IsNullOrEmpty(search))
            {
                grouped = grouped.Where(u =>
                    (u.UserName ?? "").ToLower().Contains(search) ||
                    (u.FullName ?? "").ToLower().Contains(search) ||
                    (u.District ?? "").ToLower().Contains(search) ||
                    (u.Branch ?? "").ToLower().Contains(search) ||
                    u.AccountNumbers.Any(acc => (acc ?? "").ToLower().Contains(search))
                );
            }

            int totalRecords = grouped.Count();

            // Paging + final projection
            var pagedData = grouped
                .OrderBy(u => u.District)
                .ThenBy(u => u.Branch)
                .ThenBy(u => u.FullName)
                .Skip(start)
                .Take(length > 0 ? length : int.MaxValue)
                .AsEnumerable()
                .Select(u => new
                {
                    FullName = u.FullName,
                    AccountsList = u.AccountNumbers,
                    AccountCount = u.AccountCount,
                    Branch = u.Branch,
                    District = u.District,
                    Beginning = u.SumBeginning,
                    Current = u.SumCurrent,
                    Variation = u.SumCurrent - u.SumBeginning,
                    Percentage = u.SumBeginning > 0
                        ? Math.Round((u.SumCurrent - u.SumBeginning) / u.SumBeginning * 100, 2)
                        : 0m
                })
                .ToList();

            // Grand totals
            var grandVariation = pagedData.Sum(x => x.Variation);
            var grandBeginning = pagedData.Sum(x => x.Beginning);
            var grandPercentage = grandBeginning > 0
                ? Math.Round(grandVariation / grandBeginning * 100, 2)
                : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = pagedData,
                grandVariation = grandVariation.ToString("N2"),
                grandPercentage = grandPercentage.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }
        // -------------------------------------------------------------------------
        //  AJAX – BRANCH LEVEL
        // -------------------------------------------------------------------------
        [HttpPost]
        public JsonResult BranchVariationAjax()
        {
            var draw = Request.Form["draw"];
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");
            var districtFilter = Request.Form["district"] ?? "All";
            
            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];
            var fixedBranch = Request.Form["fixedBranch"];

            var q = db.AccountMappings.AsQueryable();

            // Apply fixed filters (role-based)
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                q = q.Where(m => m.UserName != null && db.Users.Any(u => u.UserName == m.UserName && u.Process == fixedProcess));
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                q = q.Where(m => m.District.Trim() == fixedDistrict.Trim());
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                q = q.Where(m => m.Branch.Trim() == fixedBranch.Trim());

            // Dynamic district filter
            if (districtFilter != "All" && !string.IsNullOrEmpty(districtFilter))
                q = q.Where(m => m.District.Trim() == districtFilter.Trim());

            var data = q
                .Where(m => !string.IsNullOrEmpty(m.Branch))
                .GroupBy(m => m.Branch.Trim())
                .Select(g => new
                {
                    Branch = g.Key,
                    CountAccounts = g.Count(),
                    SumBeginning = g.Sum(x => x.BegginingBalance ?? 0),
                    SumCurrent = g.Sum(x => x.CurrentBalance ?? 0)
                })
                .AsEnumerable()
                .Select(x => new
                {
                    x.Branch,
                    x.CountAccounts,
                    Beginning = x.SumBeginning,
                    Current = x.SumCurrent,
                    Variation = x.SumCurrent - x.SumBeginning,
                    Percentage = x.SumBeginning > 0
                        ? Math.Round((x.SumCurrent - x.SumBeginning) / x.SumBeginning * 100, 2)
                        : 0m
                })
                .OrderBy(x => x.Branch)
                .Skip(start)
                .Take(length > 0 ? length : 999999)
                .ToList();

            var totalRecords = data.Count; // after filter

            var grandVar = data.Sum(x => x.Variation);
            var grandBeg = data.Sum(x => x.Beginning);
            var grandPct = grandBeg > 0 ? Math.Round(grandVar / grandBeg * 100, 2) : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data,
                grandVariation = grandVar.ToString("N2"),
                grandPercentage = grandPct.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }

        // -------------------------------------------------------------------------
        //  AJAX – DISTRICT LEVEL
        // -------------------------------------------------------------------------
        [HttpPost]
        public JsonResult DistrictVariationAjax()
        {
            var draw = Request.Form["draw"];
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");
            var districtFilter = Request.Form["district"] ?? "All";

            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];

            var q = db.AccountMappings.AsQueryable();

            // Apply fixed filters (role-based)
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                q = q.Where(m => m.UserName != null && db.Users.Any(u => u.UserName == m.UserName && u.Process == fixedProcess));
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                q = q.Where(m => m.District.Trim() == fixedDistrict.Trim());

            // Dynamic District filter (if not already fixed)
            if (districtFilter != "All" && !string.IsNullOrEmpty(districtFilter))
                q = q.Where(m => m.District.Trim() == districtFilter.Trim());

            var data = q
                .Where(m => !string.IsNullOrEmpty(m.District))
                .GroupBy(m => m.District.Trim())
                .Select(g => new
                {
                    District = g.Key,
                    CountAccounts = g.Count(),
                    SumBeginning = g.Sum(x => x.BegginingBalance ?? 0),
                    SumCurrent = g.Sum(x => x.CurrentBalance ?? 0)
                })
                .AsEnumerable()
                .Select(x => new
                {
                    x.District,
                    x.CountAccounts,
                    Beginning = x.SumBeginning,
                    Current = x.SumCurrent,
                    Variation = x.SumCurrent - x.SumBeginning,
                    Percentage = x.SumBeginning > 0
                        ? Math.Round((x.SumCurrent - x.SumBeginning) / x.SumBeginning * 100, 2)
                        : 0m
                })
                .OrderBy(x => x.District)
                .Skip(start)
                .Take(length > 0 ? length : 999999)
                .ToList();

            var totalRecords = data.Count;

            var grandVar = data.Sum(x => x.Variation);
            var grandBeg = data.Sum(x => x.Beginning);
            var grandPct = grandBeg > 0 ? Math.Round(grandVar / grandBeg * 100, 2) : 0m;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data,
                grandVariation = grandVar.ToString("N2"),
                grandPercentage = grandPct.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }
    }
}