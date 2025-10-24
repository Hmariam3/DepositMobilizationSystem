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
    public class ISReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        public ActionResult Index()
        {
            var result = (from u in db.Users
                          join t in db.Transactions on
                              new { u.UserName, Process = u.Process.Trim() }
                              equals new { t.UserName, Process = t.Process.Trim() } into transactions
                          from t in transactions.DefaultIfEmpty()
                          where u.Process.Trim() == "Information System Office"
                          group t by new { u.UserName, u.FullName, u.Branch, u.Process, u.DepositTargetAmount } into g
                          select new
                          {
                              UserName = g.Key.UserName,
                              FullName = g.Key.FullName,
                              Branch = g.Key.Branch,
                              Process = g.Key.Process,
                              TotalTransactionAmount = g.Sum(x => x.TransactionAmount) ?? 0,
                              TargetAmount = g.Key.DepositTargetAmount,
                              AmountLeft = g.Key.DepositTargetAmount - (g.Sum(x => x.TransactionAmount) ?? 0),
                              PercentageValue = g.Key.DepositTargetAmount == 0
                                  ? 0
                                  : (double)(g.Sum(x => x.TransactionAmount) ?? 0) / (double)g.Key.DepositTargetAmount * 100
                          }).AsEnumerable() // 🔹 Fetch data from DB before formatting
                    .Select(x => new TransactionReportViewModel
                    {
                        UserName = x.UserName,
                        FullName = x.FullName,
                        Branch = x.Branch,
                        Process = x.Process,
                        TotalTransactionAmount = x.TotalTransactionAmount,
                        TargetAmount = x.TargetAmount.Value,
                        AmountLeft = x.AmountLeft.Value,
                        PercentageAchieved = $"{x.PercentageValue:0.00}%" // 🔹 Format AFTER fetching data
              })
                    .ToList();


            return View(result);
        }

        //[AuthorizeRoles("Admin", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcel()
        {
            var result = (from u in db.Users
                          join t in db.Transactions on
                              new { u.UserName, Process = u.Process.Trim() }
                              equals new { t.UserName, Process = t.Process.Trim() } into transactions
                          from t in transactions.DefaultIfEmpty()
                          where u.Process.Trim() == "Information System Office"
                          group t by new { u.UserName, u.FullName, u.Branch, u.Process, u.DepositTargetAmount } into g
                          select new
                          {
                              UserName = g.Key.UserName,
                              FullName = g.Key.FullName,
                              Branch = g.Key.Branch,
                              Process = g.Key.Process,
                              TotalTransactionAmount = g.Sum(x => x.TransactionAmount) ?? 0,
                              TargetAmount = g.Key.DepositTargetAmount,
                              AmountLeft = g.Key.DepositTargetAmount - (g.Sum(x => x.TransactionAmount) ?? 0),
                              PercentageValue = g.Key.DepositTargetAmount == 0
                                  ? 0
                                  : (double)(g.Sum(x => x.TransactionAmount) ?? 0) / (double)g.Key.DepositTargetAmount * 100
                          }).AsEnumerable() // 🔹 Fetch data from DB before formatting
                     .Select(x => new TransactionReportViewModel
                     {
                         UserName = x.UserName,
                         FullName = x.FullName,
                         Branch = x.Branch,
                         Process = x.Process,
                         TotalTransactionAmount = x.TotalTransactionAmount,
                         TargetAmount = x.TargetAmount.Value,
                         AmountLeft = x.AmountLeft.Value,
                         PercentageAchieved = $"{x.PercentageValue:0.00}%" // 🔹 Format AFTER fetching data
                    })
                     .ToList();
            // Calculate the total amount
            decimal totalAmount = result.Sum(t => t.TargetAmount);
            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Transactions"); // Add sheet

                // Apply styles to headers
                var headerRange = ws.Range("A1:I1"); // Adjust range based on your column count
                headerRange.Style.Font.Bold = true;  // Make text bold
                headerRange.Style.Font.FontColor = XLColor.White; // White text color
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00AEEF"); // Dark blue background
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Center text
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; // Vertically center text


                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "Full Name";
                ws.Cell(1, 3).Value = "Full Name";
             
                ws.Cell(1, 4).Value = "Sub Proccess";
                ws.Cell(1, 5).Value = "Process";
                ws.Cell(1, 6).Value = "Total Transaction Amount";
                ws.Cell(1, 7).Value = "Target Amount";
                ws.Cell(1, 8).Value = "Amount Left";
                ws.Cell(1, 9).Value = "Percentage Achieved %";
                int row = 2;
                int no = 1;
                foreach (var item in result)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.UserName;
                    ws.Cell(row, 3).Value = item.FullName;
                    ws.Cell(row, 4).Value = item.Branch;
                    ws.Cell(row, 5).Value = item.Process;
                    ws.Cell(row, 6).Value = item.TotalTransactionAmount;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 7).Value = item.TargetAmount;
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 8).Value = item.AmountLeft;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 9).Value = item.PercentageAchieved;
                  
                    row++;
                    no++;
                }
                // Add total amount at the end of the table
                ws.Cell(row, 6).Value = "Total Amount";  // Label for the total
                ws.Cell(row, 7).Value = totalAmount;     // Total amount sum
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency

                ws.Columns().AdjustToContents(); // Autofit column widths

                var stream = new MemoryStream(); // Create memory stream
                wb.SaveAs(stream, false); // **Fix: Prevent stream from closing**
                stream.Position = 0; // Reset stream position

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Information System  Report" + "-" + "Transactions.xlsx");
            }

        }
    }
}
