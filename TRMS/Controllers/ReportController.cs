using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;

namespace TRMS.Controllers
{
    public class ReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        // ONE SMART VIEW FOR ALL USERS
        public ActionResult UserAchievementReport()
        {
            // Populate districts for Super users
            ViewBag.District = new List<string> { "All" }
                .Concat(db.Users.Where(u => u.District != null)
                    .Select(u => u.District.Trim())
                    .Distinct()
                    .OrderBy(d => d))
                .ToList();

            // Inject current user info from Session
            ViewBag.CurrentUserRole = Session["UserRole"]?.ToString();
            ViewBag.CurrentUserPosition = Session["Position"]?.ToString() ?? "";
            ViewBag.CurrentUserProcess = Session["Process"]?.ToString() ?? "";
            ViewBag.CurrentUserDistrict = Session["District"]?.ToString() ?? "";
            ViewBag.CurrentUserBranch = Session["UserHomeBranch"]?.ToString() ?? "";
            ViewBag.CurrentUserFullName = Session["FullName"]?.ToString() ?? "";
            ViewBag.CurrentUserName = Session["UserName"]?.ToString() ?? "";

            return View();
        }

        // SMART AJAX HANDLER – ONE FOR ALL ROLES
        [HttpPost]
        public ActionResult SmartAchievementReportAjax()
        {
            var draw = int.Parse(Request.Form["draw"]);
            var start = int.Parse(Request.Form["start"]);
            var length = int.Parse(Request.Form["length"]);
            var searchValue = (Request.Form["search[value]"] ?? "").Trim();

            var startDateStr = Request.Form["startDate"];
            var endDateStr = Request.Form["endDate"];
            var districtFilter = Request.Form["district"]; // from dropdown

            var filterMode = Request.Form["filterMode"];
            var fixedProcess = Request.Form["fixedProcess"];
            var fixedDistrict = Request.Form["fixedDistrict"];
            var fixedBranch = Request.Form["fixedBranch"];
            var fixedUserName = Request.Form["fixedUserName"];

            DateTime? startDate = null, endDate = null;
            if (DateTime.TryParse(startDateStr, out DateTime s)) startDate = s;
            if (DateTime.TryParse(endDateStr, out DateTime e)) endDate = e.Date.AddDays(1).AddSeconds(-1);

            // Base query: users with target and deposits
            var baseQuery = db.Users
                .Where(u => u.DepositTargetAmount > 0 && u.DepositPlans.Any());

            // APPLY FIXED FILTERS BASED ON ROLE
            if (filterMode == "process" && !string.IsNullOrEmpty(fixedProcess))
                baseQuery = baseQuery.Where(u => u.Process == fixedProcess);

            if (filterMode == "subprocess" && !string.IsNullOrEmpty(fixedDistrict))
                baseQuery = baseQuery.Where(u => u.District == fixedDistrict);

            if (filterMode == "branch" && !string.IsNullOrEmpty(fixedBranch))
                baseQuery = baseQuery.Where(u => u.Branch == fixedBranch);

            if (filterMode == "self" && !string.IsNullOrEmpty(fixedUserName))
                baseQuery = baseQuery.Where(u => u.UserName == fixedUserName);

            // APPLY DYNAMIC FILTERS (Super or Director)
            if (!string.IsNullOrEmpty(districtFilter) && districtFilter != "All")
                baseQuery = baseQuery.Where(u => u.District.Trim() == districtFilter.Trim());

            if (startDate.HasValue && endDate.HasValue)
                baseQuery = baseQuery.Where(u => u.DepositPlans.Any(d => d.CreatedDate >= startDate && d.CreatedDate <= endDate));

            var filteredUsers = baseQuery.ToList();

            // Search
            if (!string.IsNullOrEmpty(searchValue))
            {
                filteredUsers = filteredUsers.Where(u =>
                    (u.FullName ?? "").IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (u.UserName ?? "").IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (u.Process ?? "").IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (u.District ?? "").IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (u.Branch ?? "").IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            int totalRecords = filteredUsers.Count;

            var pagedUsers = filteredUsers
                .OrderBy(u => u.District).ThenBy(u => u.Branch).ThenBy(u => u.FullName)
                .Skip(start)
                .Take(length)
                .ToList();

            var results = new List<object>();

            foreach (var user in pagedUsers)
            {
                string userName = user.UserName.Trim();
                var userDeposits = db.DepositPlans.Where(d => d.User == userName);

                if (startDate.HasValue && endDate.HasValue)
                    userDeposits = userDeposits.Where(d => d.CreatedDate >= startDate && d.CreatedDate <= endDate);

                var depositsList = userDeposits.ToList();
                decimal rawDeposited = depositsList.Sum(d => d.Amount);
                decimal achieved = 0;

                if (depositsList.Any())
                {
                    var accounts = depositsList.Select(d => d.AccountNumber).Distinct().ToList();

                    foreach (var acc in accounts)
                    {
                        var accDeposits = db.DepositPlans
                            .Where(d => d.AccountNumber == acc)
                            .OrderBy(d => d.RefDate ?? d.CreatedDate)
                            .ToList();

                        if (!accDeposits.Any()) continue;

                        var first = accDeposits.First();
                        int dupCount = accDeposits.Count(x => x.ReferenceNumber == first.ReferenceNumber);
                        decimal totalDep = accDeposits.Sum(d => d.Amount);
                        decimal initBal = first.Prev_Ini_Bal ?? 0;
                        decimal finalBal = accDeposits.Last().AccountBalance;
                        decimal withdrawal = Math.Max(0, (initBal + totalDep) - finalBal);

                        decimal userContrib = accDeposits.Where(d => d.User == userName).Sum(d => d.Amount);
                        if (userContrib <= 0) continue;

                        decimal userShare = totalDep > 0 ? withdrawal * (userContrib / totalDep) : 0;
                        decimal achievedOnAcc = userContrib - userShare;
                        if (achievedOnAcc < 0) achievedOnAcc = 0;

                        achieved += achievedOnAcc;
                    }
                }

                decimal target = user.DepositTargetAmount ?? 0;
                decimal percentage = target > 0 ? (achieved / target) * 100 : 0;

                results.Add(new
                {
                    FullName = user.FullName ?? "N/A",
                    Process = user.Process ?? "-",
                    District = user.District ?? "-",
                    Branch = user.Branch ?? "-",
                    Target = target,
                    Deposited = rawDeposited,
                    Achieved = achieved,
                    Percentage = percentage
                });
            }

            decimal grandTotal = results.Sum(x => (decimal)((dynamic)x).Achieved);

            return Json(new
            {
                draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = results,
                grandTotalAchieved = grandTotal.ToString("N2")
            }, JsonRequestBehavior.AllowGet);
        }


        // SMART EXCEL EXPORT – Works for ALL roles
        public ActionResult ExportSmartAchievementToExcel(string startDate, string endDate, string district,
            string filterMode, string fixedProcess, string fixedDistrict, string fixedBranch, string fixedUserName)
        {
            DateTime? start = null, end = null;
            if (DateTime.TryParse(startDate, out DateTime s)) start = s;
            if (DateTime.TryParse(endDate, out DateTime e)) end = e.Date.AddDays(1).AddSeconds(-1);

            var query = db.Users.Where(u => u.DepositTargetAmount > 0 && u.DepositPlans.Any());

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

                        var first = accDeposits.First();
                        int dupCount = accDeposits.Count(x => x.ReferenceNumber == first.ReferenceNumber);
                        decimal effectiveFirst = dupCount > 1
                            ? accDeposits.Where(x => x.ReferenceNumber == first.ReferenceNumber).Sum(x => x.Amount)
                            : first.Amount;

                        decimal totalDep = accDeposits.Sum(d => d.Amount);
                        decimal initBal = first.Prev_Ini_Bal ?? 0;
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