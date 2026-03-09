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

            // 1. Base query for users
            var usersQuery = db.Users.AsQueryable();

            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                usersQuery = usersQuery.Where(u => u.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                usersQuery = usersQuery.Where(u => u.District == fixedDistrict);
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                usersQuery = usersQuery.Where(u => u.Branch == fixedBranch);
            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName))
                usersQuery = usersQuery.Where(u => u.UserName == fixedUserName);

            if (!string.IsNullOrEmpty(districtFilter) && districtFilter != "All")
                usersQuery = usersQuery.Where(u => u.District.Trim() == districtFilter.Trim());

            // 2. Query from VariationReport instead of joining with AccountMapping
            var query = from r in db.VariationReports
                        join u in usersQuery on r.UserName equals u.UserName
                        select new
                        {
                            UserName = r.UserName,
                            FullName = u.FullName ?? r.UserName,
                            District = r.District ?? "-",
                            Branch = r.Branch ?? "-",
                            AccountCount = r.Accounts, // This is a string in the model
                            SumBeginning = r.BeginningBalance ?? 0m,
                            SumCurrent = r.CurrentBalance ?? 0m
                        };

            // Global search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(search) ||
                    u.FullName.ToLower().Contains(search) ||
                    u.District.ToLower().Contains(search) ||
                    u.Branch.ToLower().Contains(search)
                );
            }

            int totalRecords = query.Count();

            // 3. Grand Totals (Calculated on the ENTIRE filtered query - SERVER SIDE)
            var totals = query.GroupBy(x => 1).Select(g => new
            {
                Beg = g.Sum(x => x.SumBeginning),
                Cur = g.Sum(x => x.SumCurrent)
            }).FirstOrDefault();

            var gVariation = (totals?.Cur ?? 0m) - (totals?.Beg ?? 0m);
            var gBeginning = totals?.Beg ?? 0m;
            var gPercentage = gBeginning > 0 ? Math.Round(gVariation / gBeginning * 100, 2) : 0m;

            // 4. Paging Logic (Export safe)
            int pageSize = length > 0 ? length : 999999;
            var pagedData = query
                .OrderBy(u => u.District).ThenBy(u => u.Branch).ThenBy(u => u.FullName)
                .Skip(start)
                .Take(pageSize)
                .ToList();

            // 5. Final Projection (Optimized: No account details to prevent timeouts on massive datasets)
            var result = pagedData.Select(u => new
            {
                u.FullName,
                u.AccountCount,
                u.Branch,
                u.District,
                Beginning = u.SumBeginning,
                Current = u.SumCurrent,
                Variation = u.SumCurrent - u.SumBeginning,
                Percentage = u.SumBeginning > 0 ? Math.Round((u.SumCurrent - u.SumBeginning) / u.SumBeginning * 100, 2) : 0m
            }).ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = result,
                grandVariation = gVariation.ToString("N2"),
                grandPercentage = gPercentage.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }
        // -------------------------------------------------------------------------
        //  AJAX – BRANCH LEVEL
        // -------------------------------------------------------------------------
        [HttpPost]
        public JsonResult BranchVariationAjax()
        {
            var draw = int.Parse(Request.Form["draw"] ?? "1");
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");
            var districtFilter = Request.Form["district"] ?? "All";
            
            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];
            var fixedBranch = Request.Form["fixedBranch"];

            var q = db.VariationReports.AsQueryable();

            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                q = q.Where(m => m.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                q = q.Where(m => m.District.Trim() == fixedDistrict.Trim());
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                q = q.Where(m => m.Branch.Trim() == fixedBranch.Trim());

            if (districtFilter != "All" && !string.IsNullOrEmpty(districtFilter))
                q = q.Where(m => m.District.Trim() == districtFilter.Trim());

            var query = from m in q
                        where !string.IsNullOrEmpty(m.Branch)
                        group m by new { Branch = m.Branch.Trim(), District = (m.District ?? "-").Trim() } into g
                        select new
                        {
                            g.Key.Branch,
                            g.Key.District,
                            AccountStrings = g.Select(x => x.Accounts),
                            SumBeginning = g.Sum(x => x.BeginningBalance ?? 0m),
                            SumCurrent = g.Sum(x => x.CurrentBalance ?? 0m)
                        };

            int totalRecords = query.Count();

            // Grand Totals (FULL Dataset - SERVER SIDE)
            var totals = query.GroupBy(x => 1).Select(g => new {
                Beg = g.Sum(x => x.SumBeginning),
                Cur = g.Sum(x => x.SumCurrent)
            }).FirstOrDefault();

            var gVariation = (totals?.Cur ?? 0m) - (totals?.Beg ?? 0m);
            var gBeginning = totals?.Beg ?? 0m;
            var gPercentage = gBeginning > 0 ? Math.Round(gVariation / gBeginning * 100, 2) : 0m;

            var data = query
                .OrderBy(x => x.Branch)
                .Skip(start)
                .Take(length > 0 ? length : 999999)
                .AsEnumerable() // Map to memory to sum the account strings
                .Select(x => new
                {
                    x.Branch,
                    x.District,
                    CountAccounts = x.AccountStrings.ToList().Sum(s => int.TryParse(s, out int val) ? val : 0),
                    Beginning = x.SumBeginning,
                    Current = x.SumCurrent,
                    Variation = x.SumCurrent - x.SumBeginning,
                    Percentage = x.SumBeginning > 0 ? Math.Round((x.SumCurrent - x.SumBeginning) / x.SumBeginning * 100, 2) : 0m
                })
                .ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data,
                grandVariation = gVariation.ToString("N2"),
                grandPercentage = gPercentage.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }

        // -------------------------------------------------------------------------
        //  AJAX – DISTRICT LEVEL
        // -------------------------------------------------------------------------
        [HttpPost]
        public JsonResult DistrictVariationAjax()
        {
            var draw = int.Parse(Request.Form["draw"] ?? "1");
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");
            var districtFilter = Request.Form["district"] ?? "All";

            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];

            var q = db.VariationReports.AsQueryable();

            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                q = q.Where(m => m.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                q = q.Where(m => m.District.Trim() == fixedDistrict.Trim());

            if (districtFilter != "All" && !string.IsNullOrEmpty(districtFilter))
                q = q.Where(m => m.District.Trim() == districtFilter.Trim());

            var query = from m in q
                        where !string.IsNullOrEmpty(m.District)
                        group m by m.District.Trim() into g
                        select new
                        {
                            District = g.Key,
                            AccountStrings = g.Select(x => x.Accounts),
                            SumBeginning = g.Sum(x => x.BeginningBalance ?? 0m),
                            SumCurrent = g.Sum(x => x.CurrentBalance ?? 0m)
                        };

            int totalRecords = query.Count();

            // Grand Totals (FULL Dataset - SERVER SIDE)
            var totals = query.GroupBy(x => 1).Select(g => new {
                Beg = g.Sum(x => x.SumBeginning),
                Cur = g.Sum(x => x.SumCurrent)
            }).FirstOrDefault();

            var gVariation = (totals?.Cur ?? 0m) - (totals?.Beg ?? 0m);
            var gBeginning = totals?.Beg ?? 0m;
            var gPercentage = gBeginning > 0 ? Math.Round(gVariation / gBeginning * 100, 2) : 0m;

            var data = query
                .OrderBy(x => x.District)
                .Skip(start)
                .Take(length > 0 ? length : 999999)
                .AsEnumerable()
                .Select(x => new
                {
                    x.District,
                    CountAccounts = x.AccountStrings.ToList().Sum(s => int.TryParse(s, out int val) ? val : 0),
                    Beginning = x.SumBeginning,
                    Current = x.SumCurrent,
                    Variation = x.SumCurrent - x.SumBeginning,
                    Percentage = x.SumBeginning > 0 ? Math.Round((x.SumCurrent - x.SumBeginning) / x.SumBeginning * 100, 2) : 0m
                })
                .ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data,
                grandVariation = gVariation.ToString("N2"),
                grandPercentage = gPercentage.ToString("N2") + "%"
            }, JsonRequestBehavior.AllowGet);
        }
    }
}