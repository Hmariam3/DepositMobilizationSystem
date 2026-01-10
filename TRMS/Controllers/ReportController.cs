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
    public class ReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // EXISTING VIEWS
        public ActionResult UserAchievementReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult ProcessAchievementReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult DistrictAchievementReport()
        {
            GetCommonViewBags();
            return View();
        }

        public ActionResult SubprocessAchievementReport()
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
        }


        // SMART AJAX HANDLER – ONE FOR ALL ROLES
        [HttpPost]
        public ActionResult SmartAchievementReportAjax()
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
            var fixedUserName = Request.Form["fixedUserName"];

            // Base query from the PRE-CALCULATED UserAchieved table
            var query = db.UserAchieveds.AsQueryable();

            // Apply role-based fixed filters
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                query = query.Where(ua => ua.Process == fixedProcess);

            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                query = query.Where(ua => ua.District == fixedDistrict);

            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                query = query.Where(ua => ua.Branch == fixedBranch);

            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName))
                query = query.Where(ua => ua.UserName == fixedUserName);

            // Dynamic district dropdown filter
            if (!string.IsNullOrEmpty(districtFilter) && districtFilter != "All")
                query = query.Where(ua => ua.District.Trim() == districtFilter.Trim());

            // Only include users who have a target (optional, but clean)
            query = query.Where(ua => ua.UserTarget > 0);

            // Search filter across multiple fields
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(ua =>
                    (ua.UserName ?? "").ToLower().Contains(searchValue) ||
                    (ua.Process ?? "").ToLower().Contains(searchValue) ||
                    (ua.District ?? "").ToLower().Contains(searchValue) ||
                    (ua.Branch ?? "").ToLower().Contains(searchValue) ||
                    (ua.Position ?? "").ToLower().Contains(searchValue)
                );
            }

            // Get total before paging
            int totalRecords = query.Count();

            // Sorting: District → Branch → Full Name (via join with Users table for FullName)
            // ---- SAFE PAGING (handles length = -1 for export) ----
            int pageSize = length > 0 ? length : int.MaxValue;   // if length == -1 → take everything
            int skip = start;

            // When exporting we don't want to skip anything if we're taking all rows
            if (length < 0)
                skip = 0;

            var pagedData = query
                .Join(db.Users,
                      ua => ua.UserName,
                      u => u.UserName,
                      (ua, u) => new { UA = ua, U = u })
                .OrderBy(x => x.UA.District)
                .ThenBy(x => x.UA.Branch)
                .ThenBy(x => x.U.FullName ?? x.UA.UserName)
                .Skip(skip)
                .Take(pageSize)          // now pageSize is always ≥ 1
                .Select(x => new
                {
                    FullName = x.U.FullName ?? x.UA.UserName,
                    Process = x.UA.Process ?? "-",
                    District = x.UA.District ?? "-",
                    Branch = x.UA.Branch ?? "-",
                    Target = x.UA.UserTarget ?? 0,
                    //Deposited = x.UA.CollectedAmount ?? 0,
                    Achieved = x.UA.AchievedAmount ?? 0,
                    Percentage = x.UA.AchievedPercent ?? 0
                })
                .ToList();
            // Grand totals
            var totals = query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    //TotalDeposited = g.Sum(x => x.CollectedAmount ?? 0),
                    TotalAchieved = g.Sum(x => x.AchievedAmount ?? 0)
                })
                .FirstOrDefault();

            decimal grandTotalTarget = totals?.TotalTarget ?? 0;
            //decimal grandTotalDeposited = totals?.TotalDeposited ?? 0;
            decimal grandTotalAchieved = totals?.TotalAchieved ?? 0;
            decimal grandTotalPercentage = grandTotalTarget > 0
                    ? Math.Round((grandTotalAchieved / grandTotalTarget) * 100, 1)
                    : 0m;

            var results = pagedData.Select(r => new
            {
                r.FullName,
                r.Process,
                r.District,
                r.Branch,
                Target = r.Target,
                //Deposited = r.Deposited,
                Achieved = r.Achieved,
                Percentage = Math.Round(r.Percentage, 2)
            }).ToList();

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalTarget = grandTotalTarget.ToString("N2"),
                //grandTotalDeposited = grandTotalDeposited.ToString("N2"),
                grandTotalAchieved = grandTotalAchieved.ToString("N2"),
                grandTotalPercentage = grandTotalPercentage   // ← THIS WAS MISSING!
            }, JsonRequestBehavior.AllowGet);
        }

        // BY PROCESS – ULTRA FAST VERSION USING UserAchieved TABLE
        [HttpPost]
        public ActionResult ProcessAchievementAjax()
        {
            var draw = Request.Form["draw"].ToString();

            // Extract DataTables parameters safely
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            // Optional: Date filters (uncomment when you implement date filtering)
            // var startDate = Request.Form["startDate"];
            // var endDate = Request.Form["endDate"];

            // Base query: Group by Process
            var query = db.UserAchieveds
                .Where(ua => ua.UserTarget > 0 && !string.IsNullOrEmpty(ua.Process))
                .GroupBy(ua => ua.Process.Trim())
                .Select(g => new
                {
                    Process = g.Key,
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    //TotalDeposited = g.Sum(x => x.CollectedAmount ?? 0),
                    TotalAchieved = g.Sum(x => x.AchievedAmount ?? 0)
                });

            // Get total count before paging (for DataTables)
            int totalRecords = query.Count();

            // === SAFE PAGING: Handle length = -1 (used during export) ===
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;  // Don't skip when exporting all

            var processData = query
                .OrderBy(x => x.Process)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var results = new List<object>();
            int displayIndex = skip + 1; // Correct "No" numbering even when paged

            decimal grandTotalTarget = 0;
            decimal grandTotalDeposited = 0;
            decimal grandTotalAchieved = 0;

            foreach (var p in processData)
            {
                decimal percentage = p.TotalTarget > 0
                    ? Math.Round((p.TotalAchieved / p.TotalTarget) * 100, 2)
                    : 0;

                results.Add(new
                {
                    No = displayIndex++,
                    Process = p.Process ?? "Unassigned",
                    Target = p.TotalTarget,
                    //Deposited = p.TotalDeposited,
                    Achieved = p.TotalAchieved,
                    Percentage = percentage
                });

                // Accumulate grand totals
                grandTotalTarget += p.TotalTarget;
                //grandTotalDeposited += p.TotalDeposited;
                grandTotalAchieved += p.TotalAchieved;
            }

            // DO NOT ADD fake "ALL PROCESSES" row → removed completely

            // Calculate overall achievement percentage
            decimal grandTotalPercentage = grandTotalTarget > 0
                ? Math.Round((grandTotalAchieved / grandTotalTarget) * 100, 2)
                : 0;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,

                // These will be used in your header summary
                grandTotalTarget = grandTotalTarget.ToString("N2"),
                grandTotalDeposited = grandTotalDeposited.ToString("N2"),
                grandTotalAchieved = grandTotalAchieved.ToString("N2"),
                grandTotalPercentage = grandTotalPercentage
            }, JsonRequestBehavior.AllowGet);
        }

        // BY DISTRICT – Only Operation Management Office (Real Districts)
        [HttpPost]
        public ActionResult DistrictAchievementAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var districtData = db.UserAchieveds
                .Where(ua =>
                    ua.UserTarget > 0 &&
                    ua.Process == "Operation Management Office" &&   // Only real districts
                    !string.IsNullOrEmpty(ua.District) &&
                    ua.District.Trim() != "")
                .GroupBy(ua => ua.District.Trim())
                .Select(g => new
                {
                    District = g.Key,
                    TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    //TotalDeposited = g.Sum(x => x.CollectedAmount ?? 0),
                    TotalAchieved = g.Sum(x => x.AchievedAmount ?? 0)
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildAchievementResponse(
                draw: draw,
                data: districtData,
                totalLabel: "ALL DISTRICTS",
                start: start,
                length: length
            );
        }

        // BY SUBPROCESS – All non-Operation Management Office (Head Office Units)
        [HttpPost]
        public ActionResult SubprocessAchievementAjax()
        {
            var draw = Request.Form["draw"].ToString();
            var start = int.Parse(Request.Form["start"] ?? "0");
            var length = int.Parse(Request.Form["length"] ?? "10");

            var subprocessData = db.UserAchieveds
                .Where(ua =>
                    ua.UserTarget > 0 &&
                    ua.Process != "Operation Management Office" &&   // Only Head Office subprocesses
                    !string.IsNullOrEmpty(ua.District) &&
                    ua.District.Trim() != "")
                .GroupBy(ua => ua.District.Trim())
                .Select(g => new
                {
                    District = g.Key,  // This field is reused for Subprocess name
            TotalTarget = g.Sum(x => x.UserTarget ?? 0),
                    //TotalDeposited = g.Sum(x => x.CollectedAmount ?? 0),
                    TotalAchieved = g.Sum(x => x.AchievedAmount ?? 0)
                })
                .OrderBy(x => x.District)
                .ToList();

            return BuildAchievementResponse(
                draw: draw,
                data: subprocessData,
                totalLabel: "ALL HEAD OFFICE UNITS",
                start: start,
                length: length
            );
        }


        private JsonResult BuildAchievementResponse(
            string draw,
            IEnumerable<dynamic> data,
            string totalLabel,
            int start = 0,
            int length = 10)
        {
            // Safe paging – critical for export (length = -1)
            int pageSize = length > 0 ? length : int.MaxValue;
            int skip = length < 0 ? 0 : start;

            var pagedData = data.Skip(skip).Take(pageSize).ToList();
            int totalRecords = data.Count();

            var results = new List<object>();
            int displayIndex = skip + 1;

            decimal grandTarget = 0;
            //decimal grandDeposited = 0;
            decimal grandAchieved = 0;

            foreach (var d in pagedData)
            {
                decimal percentage = d.TotalTarget > 0
                    ? Math.Round((d.TotalAchieved / d.TotalTarget) * 100, 2)
                    : 0;

                results.Add(new
                {
                    No = displayIndex++,
                    Name = d.District ?? "Unassigned",  // Works for both District & Subprocess
                    Target = d.TotalTarget,
                    //Deposited = d.TotalDeposited,
                    Achieved = d.TotalAchieved,
                    Percentage = percentage
                });

                grandTarget += (decimal)d.TotalTarget;
                //grandDeposited += (decimal)d.TotalDeposited;
                grandAchieved += (decimal)d.TotalAchieved;
            }

            decimal grandPercentage = grandTarget > 0
                ? Math.Round((grandAchieved / grandTarget) * 100, 2)
                : 0;

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,

                // For header summary
                grandTotalTarget = grandTarget.ToString("N2"),
                //grandTotalDeposited = grandDeposited.ToString("N2"),
                grandTotalAchieved = grandAchieved.ToString("N2"),
                grandTotalPercentage = grandPercentage
            }, JsonRequestBehavior.AllowGet);
        }




        public ActionResult EcoShareReport()
        {
            var data = db.DepositPlans
                .Where(x => x.AccountNumber.Trim() == "1022200344558")
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new EcoShareReportVM
                {
                    DID = x.DID,
                    FullName = db.Users
                        .Where(u => u.UserName == x.User)
                        .Select(u => u.FullName)
                        .FirstOrDefault(),

                    Process = x.Process,
                    District = x.District,
                    Branch = x.Branch,
                    AccountNumber = x.AccountNumber,
                    ReferenceNumber = x.ReferenceNumber,
                    Amount = x.Amount,
                    RefDate = x.RefDate,
                    AccountHolder = x.AccountHolder,
                    Narative = x.Narative,
                    CreatedDate = x.CreatedDate
                })
                .ToList();


            return View(data);
        }


        // SMART EXCEL EXPORT – Works for ALL roles
        public ActionResult ExportSmartAchievementToExcel(string startDate, string endDate, string district,
            string filterMode, string fixedProcess, string fixedDistrict, string fixedBranch, string fixedUserName)
        {
            DateTime? start = null, end = null;
            if (DateTime.TryParse(startDate, out DateTime s)) start = s;
            if (DateTime.TryParse(endDate, out DateTime e)) end = e.Date.AddDays(1).AddSeconds(-1);

            var query = db.Users.Where(u => u.DepositTargetAmount >= 0);

            // Apply same fixed filters
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess)) query = query.Where(u => u.Process == fixedProcess);
            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict)) query = query.Where(u => u.District == fixedDistrict);
            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch)) query = query.Where(u => u.Branch == fixedBranch);
            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName)) query = query.Where(u => u.UserName == fixedUserName);
            if (!string.IsNullOrEmpty(district) && district != "All") query = query.Where(u => u.District.Trim() == district.Trim());

            var users = query.ToList();
            var results = new List<object>();

            foreach (var user in users)
            {
                string userName = user.UserName.Trim();
                var userDeposits = db.DepositPlans.Where(d => d.User == userName);

                if (start.HasValue && end.HasValue)
                    userDeposits = userDeposits.Where(d => d.CreatedDate >= start && d.CreatedDate <= end);

                var depositsList = userDeposits.ToList();
                decimal rawDeposited = depositsList.Sum(d => d.Amount);
                decimal achieved = 0;

                if (depositsList.Any())
                {
                    var accounts = depositsList.Select(d => d.AccountNumber).Distinct();

                    foreach (var acc in accounts)
                    {
                        var accDeposits = db.DepositPlans
                            .Where(d => d.AccountNumber == acc)
                            .OrderBy(d => d.RefDate)
                            .ToList();

                        if (!accDeposits.Any()) continue;

                        // Get the first RefDate
                        var firstRefDate = accDeposits.Min(d => d.RefDate ?? d.CreatedDate);

                        // Filter deposits that have that same RefDate
                        var sameDateDeposits = accDeposits
                            .Where(d => (d.RefDate ?? d.CreatedDate) == firstRefDate)
                            .ToList();

                        //int dupCount = accDeposits.Count(x => x.ReferenceNumber == sameDateDeposits.ReferenceNumber);
                        //decimal effectiveFirst = dupCount > 1
                        //    ? accDeposits.Where(x => x.ReferenceNumber == first.ReferenceNumber).Sum(x => x.Amount)
                        //    : first.Amount;

                        decimal totalDep = accDeposits.Sum(d => d.Amount);
                        decimal initBal = sameDateDeposits.Min(d => d.Prev_Ini_Bal ?? 0);
                        decimal finalBal = accDeposits.Last().AccountBalance;
                        decimal withdrawal = Math.Max(0, (initBal + totalDep) - finalBal);

                        decimal userContrib = accDeposits.Where(d => d.User == userName).Sum(d => d.Amount);
                        if (userContrib <= 0) continue;

                        decimal userShare = withdrawal * (userContrib / totalDep);
                        decimal achievedOnAcc = userContrib - userShare;
                        if (achievedOnAcc < 0) achievedOnAcc = 0;

                        achieved += achievedOnAcc;
                    }
                }

                decimal target = user.DepositTargetAmount ?? 0;
                decimal percentage = target > 0 ? (achieved / target) * 100 : 0;

                results.Add(new
                {
                    No = results.Count + 1,
                    FullName = user.FullName ?? "N/A",
                    Process = user.Process ?? "-",
                    District = user.District ?? "-",
                    Branch = user.Branch ?? "-",
                    Target = target,
                    Deposited = rawDeposited,
                    Achieved = achieved,
                    Percentage = percentage.ToString("F2") + "%"
                });
            }

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Achievement Report");

                // === 1. TITLE ===
                ws.Cell(1, 1).Value = "Deposit Achievement Report";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;

                // === 2. INFO LINE ===
                ws.Cell(2, 1).Value =
                    $"Period: {startDate ?? "All"} to {endDate ?? "Today"} | " +
                    $"Scope: {GetScopeName(filterMode, fixedProcess, fixedDistrict, fixedBranch, fixedUserName)}";
                ws.Cell(2, 1).Style.Font.Italic = true;

                // === 3. HEADER ROW ===
                var headers = new[] { "NO", "Full Name", "Process", "District", "Branch",
                          "Target", "Deposited", "Achieved", "% Achieved" };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(4, i + 1).Value = headers[i];

                var headerRange = ws.Range(4, 1, 4, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // === 4. DATA ROWS ===
                int row = 5;

                foreach (var r in results)
                {
                    ws.Cell(row, 1).Value = r.GetType().GetProperty("No").GetValue(r)?.ToString();
                    ws.Cell(row, 2).Value = r.GetType().GetProperty("FullName").GetValue(r)?.ToString();
                    ws.Cell(row, 3).Value = r.GetType().GetProperty("Process").GetValue(r)?.ToString();
                    ws.Cell(row, 4).Value = r.GetType().GetProperty("District").GetValue(r)?.ToString();
                    ws.Cell(row, 5).Value = r.GetType().GetProperty("Branch").GetValue(r)?.ToString();
                    ws.Cell(row, 6).Value = Convert.ToDecimal(r.GetType().GetProperty("Target").GetValue(r));
                    ws.Cell(row, 7).Value = Convert.ToDecimal(r.GetType().GetProperty("Deposited").GetValue(r));
                    ws.Cell(row, 8).Value = Convert.ToDecimal(r.GetType().GetProperty("Achieved").GetValue(r));
                    ws.Cell(row, 9).Value = r.GetType().GetProperty("Percentage").GetValue(r)?.ToString();

                    row++;
                }

                // === 5. TOTAL ROW ===
                int totalRow = row;

                ws.Cell(totalRow, 1).Value = "TOTAL:";
                ws.Range(totalRow, 1, totalRow, 5).Merge();
                ws.Cell(totalRow, 1).Style.Font.Bold = true;
                ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Sum numeric columns
                ws.Cell(totalRow, 6).FormulaA1 = $"=SUM(F5:F{row - 1})"; // Target total
                ws.Cell(totalRow, 7).FormulaA1 = $"=SUM(G5:G{row - 1})"; // Deposited total
                ws.Cell(totalRow, 8).FormulaA1 = $"=SUM(H5:H{row - 1})"; // Achieved total

                // Average percentage
                ws.Cell(totalRow, 9).FormulaA1 = $"=AVERAGE(I5:I{row - 1})";

                // Style total row
                ws.Range(totalRow, 1, totalRow, 9).Style.Font.Bold = true;
                ws.Range(totalRow, 1, totalRow, 9).Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Range(totalRow, 1, totalRow, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

                // === 6. FORMATTING ===
                ws.Columns(6, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Column(9).Style.NumberFormat.Format = "0.00%";          // % Achieved

                ws.Columns().AdjustToContents();
                ws.Column(1).Width = 8;   // Perfect for numbering (1–9999)
                // === 7. EXPORT ===
                var stream = new MemoryStream();
                wb.SaveAs(stream);
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Achievement_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }

        }

        private string GetScopeName(string mode, string process, string district, string branch, string fullName)
        {
            if (mode == null) mode = "";
            mode = mode.ToLower();

            switch (mode)
            {
                case "process":
                    return $"Process: {process}";

                case "subprocess":
                    return $"District: {district}";

                case "branch":
                    return $"Branch: {branch}";

                case "self":
                    return $"User: {fullName}";

                default:
                    return "Bank-Wide";
            }
        }


    }
}