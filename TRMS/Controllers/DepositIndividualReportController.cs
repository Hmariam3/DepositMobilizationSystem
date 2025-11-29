using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRMS.Models;
using TRMS.Security;

namespace TRMS.Controllers
{
    public class DepositIndividualReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();


        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndividualDeposit(DateTime? startDate, DateTime? endDate)
        {
            string individualUser = Session["UserName"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositPlans.AsQueryable();
            decimal totalAmount = 0;

            if (!string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.User.Trim() == individualUser.Trim());
                totalAmount = transactions.Where(t => t.User.Trim() == individualUser.Trim()).Select(t => (decimal?)t.AccountBalance - t.IntialAccountBalance + t.Amount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.AccountBalance - u.IntialAccountBalance + u.Amount) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }

        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndividualMerchant(DateTime? startDate, DateTime? endDate)
        {
            string individualUser = Session["UserName"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositMerchantAgenets.AsQueryable();
            decimal totalAmount = 0;

            if ((userPosition == "Individual") && !string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.CreatedBY.Trim() == individualUser.Trim());
                totalAmount = transactions.Where(t => t.CreatedBY.Trim() == individualUser.Trim()).Select(t => (decimal?)t.AccountBalance - t.IntialAccountBalance + t.Target).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.AccountBalance - u.IntialAccountBalance + u.Target) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }


        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndividualFCY(DateTime? startDate, DateTime? endDate)
        {
            string individualUser = Session["UserName"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositFCies.AsQueryable();
            decimal totalAmount = 0;

            if ((userPosition == "Individual") && !string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.User.Trim() == individualUser.Trim());
                totalAmount = transactions.Where(t => t.User.Trim() == individualUser.Trim()).Select(t => (decimal?)t.TransactionAmount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.TransactionAmount) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }

        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcel(DateTime? startDate, DateTime? endDate)
        {

            string individualUser = Session["UserName"].ToString();
            var transactions = db.DepositPlans.AsQueryable();
            decimal totalAmountintialBalance = 0;
            decimal totalAmountCurrentBalance = 0;
            decimal totalAmount = 0;
            if (!string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.User == individualUser);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
            }

            if (transactions.Count() > 0)
            {
                totalAmount = transactions.Sum(t => t.Amount);
                totalAmountintialBalance = transactions.Sum(t => t.IntialAccountBalance);
                totalAmountCurrentBalance = transactions.Sum(t => t.AccountBalance);

            }


            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Individual Deposit Report"); // Add worksheet

                // ==============================
                // 1️⃣ HEADERS
                // ==============================
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "Process";
                ws.Cell(1, 3).Value = "District/Subprocess";
                ws.Cell(1, 4).Value = "Branch/Team";
                ws.Cell(1, 5).Value = "Deposited Amount";
                ws.Cell(1, 6).Value = "Account Number";
                ws.Cell(1, 7).Value = "Account Holder";
                ws.Cell(1, 8).Value = "Initial Account Balance";
                ws.Cell(1, 9).Value = "Current Account Balance";
                ws.Cell(1, 10).Value = "Collected Amount";
                ws.Cell(1, 11).Value = "Narrative";
                ws.Cell(1, 12).Value = "Maker";
                ws.Cell(1, 13).Value = "Transaction Date";
                ws.Cell(1, 14).Value = "Deposit Type";

                // Style header row
                var headerRange = ws.Range("A1:N1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // ==============================
                // 2️⃣ ROWS
                // ==============================
                int row = 2;
                int no = 1;
                decimal collectedAmount;

                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.Process;
                    ws.Cell(row, 3).Value = item.District;
                    ws.Cell(row, 4).Value = item.Branch;

                    ws.Cell(row, 5).Value = item.Amount;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 6).Value = item.AccountNumber;
                    ws.Cell(row, 7).Value = item.AccountHolder;

                    ws.Cell(row, 8).Value = item.IntialAccountBalance;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 9).Value = item.AccountBalance;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                    collectedAmount = item.AccountBalance - item.IntialAccountBalance + item.Amount;
                    ws.Cell(row, 10).Value = collectedAmount;
                    ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 11).Value = item.Narative;
                    ws.Cell(row, 12).Value = item.User;
                    ws.Cell(row, 13).Value = item.CreatedDate.ToString("yyyy-MM-dd");
                    ws.Cell(row, 14).Value = item.DepositType;

                    row++;
                    no++;
                }

                // ==============================
                // 3️⃣ TOTAL ROW
                // ==============================
                ws.Cell(row, 4).Value = "TOTALS:";
                ws.Cell(row, 4).Style.Font.Bold = true;

                ws.Cell(row, 5).Value = transactions.Sum(x => x.Amount);
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.Font.Bold = true;

                ws.Cell(row, 8).Value = transactions.Sum(x => x.IntialAccountBalance);
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.Font.Bold = true;

                ws.Cell(row, 9).Value = transactions.Sum(x => x.AccountBalance);
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Style.Font.Bold = true;

                ws.Cell(row, 10).Value = transactions.Sum(x => x.AccountBalance - x.IntialAccountBalance + x.Amount);
                ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 10).Style.Font.Bold = true;

                // ==============================
                // 4️⃣ FORMATTING & EXPORT
                // ==============================
                ws.Columns().AdjustToContents(); // Auto-fit columns
                ws.SheetView.FreezeRows(1);      // Freeze header row

                var stream = new MemoryStream();
                wb.SaveAs(stream, false); // Prevent closing stream
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Individual Deposit Report.xlsx");
            }

        }


        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // Export to Excel
        public ActionResult ExportDepositAgent(DateTime? startDate, DateTime? endDate)
        {

            string individualUser = Session["UserName"].ToString();
            var transactions = db.DepositMerchantAgenets.AsQueryable();
            decimal totalAmount = 0;
            decimal totalAmountintialBalance = 0;
            decimal totalAmountCurrentBalance = 0;
            if (!string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.CreatedBY.Trim() == individualUser.Trim());
                totalAmount = transactions.Where(t => t.CreatedBY.Trim() == individualUser.Trim()).Select(t => (decimal?)t.Target).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.Target) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;
            if (transactions.Count() > 0)
            {
                totalAmountintialBalance = transactions.Sum(t => t.IntialAccountBalance);
                totalAmountCurrentBalance = transactions.Sum(t => t.AccountBalance);
            }

            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Individual Merchant Report"); // Add worksheet

                // ==============================
                // 1️⃣ HEADERS
                // ==============================
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "Process";
                ws.Cell(1, 3).Value = "District/Subprocess";
                ws.Cell(1, 4).Value = "Branch/Team";
                ws.Cell(1, 5).Value = "Amount";
                ws.Cell(1, 6).Value = "Merchant ID";
                ws.Cell(1, 7).Value = "Account Number";
                ws.Cell(1, 8).Value = "Account Holder";
                ws.Cell(1, 9).Value = "Initial Account Balance";
                ws.Cell(1, 10).Value = "Current Account Balance";
                ws.Cell(1, 11).Value = "Actual Collected Amount";
                ws.Cell(1, 12).Value = "Merchant Type";
                ws.Cell(1, 13).Value = "Business Type";
                ws.Cell(1, 14).Value = "QR";
                ws.Cell(1, 15).Value = "Narrative";
                ws.Cell(1, 16).Value = "Created By";
                ws.Cell(1, 17).Value = "Created Date";
                ws.Cell(1, 18).Value = "Deposit Type";

                // Style header row (bold, centered, background)
                var headerRange = ws.Range("A1:R1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // ==============================
                // 2️⃣ ROWS
                // ==============================
                int row = 2;
                int no = 1;
                decimal collectedAmount;

                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.Process;
                    ws.Cell(row, 3).Value = item.District;
                    ws.Cell(row, 4).Value = item.Branch;

                    ws.Cell(row, 5).Value = item.Target;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 6).Value = item.LinkAccount;
                    ws.Cell(row, 7).Value = item.AccountNumber;
                    ws.Cell(row, 8).Value = item.AccountHolder;

                    ws.Cell(row, 9).Value = item.IntialAccountBalance;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 10).Value = item.AccountBalance;
                    ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                    collectedAmount = item.AccountBalance - item.IntialAccountBalance + item.Target;
                    ws.Cell(row, 11).Value = collectedAmount;
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 12).Value = item.Merchant_Type;
                    ws.Cell(row, 13).Value = item.Business_Type;
                    ws.Cell(row, 14).Value = item.QR;
                    ws.Cell(row, 15).Value = item.Narative;
                    ws.Cell(row, 16).Value = item.CreatedBY;
                    ws.Cell(row, 17).Value = item.CreatedDate.ToString("yyyy-MM-dd");
                    ws.Cell(row, 18).Value = item.DepositType;

                    row++;
                    no++;
                }

                // ==============================
                // 3️⃣ TOTAL ROW
                // ==============================
                ws.Cell(row, 4).Value = "TOTALS:";
                ws.Cell(row, 4).Style.Font.Bold = true;

                ws.Cell(row, 5).Value = transactions.Sum(x => x.Target);
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.Font.Bold = true;

                ws.Cell(row, 9).Value = transactions.Sum(x => x.IntialAccountBalance);
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Style.Font.Bold = true;

                ws.Cell(row, 10).Value = transactions.Sum(x => x.AccountBalance);
                ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 10).Style.Font.Bold = true;

                ws.Cell(row, 11).Value = transactions.Sum(x => x.AccountBalance - x.IntialAccountBalance + x.Target);
                ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 11).Style.Font.Bold = true;

                // ==============================
                // 4️⃣ FORMAT & EXPORT
                // ==============================
                ws.Columns().AdjustToContents(); // Auto-fit column widths
                ws.SheetView.FreezeRows(1);      // Freeze header row

                var stream = new MemoryStream();
                wb.SaveAs(stream, false);
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Individual Merchant Report.xlsx");
            }


        }

        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // Export to Excel
        public ActionResult ExportDepositFcy(DateTime? startDate, DateTime? endDate)
        {
            string individualUser = Session["UserName"].ToString();
            var transactions = db.DepositFCies.AsQueryable();
            decimal totalAmount = 0;

            if (!string.IsNullOrEmpty(individualUser))
            {
                transactions = transactions.Where(t => t.User.Trim() == individualUser.Trim());
                totalAmount = transactions.Where(t => t.User.Trim() == individualUser.Trim()).Select(t => (decimal?)t.TransactionAmount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.TransactionAmount) ?? 0m;
            }

            ViewBag.totalAmount = totalAmount;

            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Individual FCY Report"); // Add worksheet

                // ==============================
                // 1️⃣ HEADERS
                // ==============================
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "Process";
                ws.Cell(1, 3).Value = "District/Subprocess";
                ws.Cell(1, 4).Value = "Branch/Team";
                ws.Cell(1, 5).Value = "Currency";
                ws.Cell(1, 6).Value = "Account Number";
                ws.Cell(1, 7).Value = "Reference Number";
                ws.Cell(1, 8).Value = "Transaction Amount";
                ws.Cell(1, 9).Value = "Narrative";
                ws.Cell(1, 10).Value = "Maker";
                ws.Cell(1, 11).Value = "Created Date";
                ws.Cell(1, 12).Value = "Deposit Type";

                // Style header row
                var headerRange = ws.Range("A1:M1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // ==============================
                // 2️⃣ ROWS
                // ==============================
                int row = 2;
                int no = 1;

                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.Process;
                    ws.Cell(row, 3).Value = item.District;
                    ws.Cell(row, 4).Value = item.Branch;

                    ws.Cell(row, 5).Value = item.Currecy;
                    ws.Cell(row, 6).Value = item.AccountNumber;
                    ws.Cell(row, 7).Value = item.RefernceNumber;

                    ws.Cell(row, 8).Value = item.TransactionAmount;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 9).Value = item.Narative;
                    ws.Cell(row, 10).Value = item.User;
                    ws.Cell(row, 11).Value = item.CreatedDate.ToString("yyyy-MM-dd");
                    ws.Cell(row, 12).Value = item.DepositType;

                    row++;
                    no++;
                }

                // ==============================
                // 3️⃣ TOTAL ROW
                // ==============================
                ws.Cell(row, 7).Value = "TOTAL TRANSACTION AMOUNT:";
                ws.Cell(row, 7).Style.Font.Bold = true;

                ws.Cell(row, 8).Value = transactions.Sum(x => x.TransactionAmount);
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.Font.Bold = true;

                // ==============================
                // 4️⃣ FORMATTING & EXPORT
                // ==============================
                ws.Columns().AdjustToContents(); // Auto-fit all columns
                ws.SheetView.FreezeRows(1);      // Freeze header row

                var stream = new MemoryStream();
                wb.SaveAs(stream, false); // Prevents stream from closing
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Individual FCY Report.xlsx");
            }


        }
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // Export to PDF
        public ActionResult ExportToPdf(DateTime? startDate, DateTime? endDate)
        {
            string UserName = Session["UserName"].ToString();
            var transactions = db.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(UserName))
            {
                transactions = transactions.Where(t => t.UserName == UserName);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
            }

            Document document = new Document(PageSize.A4, 10, 10, 10, 10);
            MemoryStream stream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            PdfPTable table = new PdfPTable(7);
            table.AddCell("NO");
            table.AddCell("User Name");
            table.AddCell("Branch/Departement");
            table.AddCell("Reference Number");
            table.AddCell("Slip");
            table.AddCell("Amount");
            table.AddCell("Channel");
            decimal totalAmount = transactions.Sum(t => t.TransactionAmount ?? 0);
            int no = 1;
            foreach (var item in transactions)
            {
                table.AddCell(no.ToString());
                table.AddCell(item.UserName);
                table.AddCell(item.UserBranch);
                table.AddCell(item.ReferenceNumber);
                table.AddCell(item.Slip);
                table.AddCell(item.TransactionAmount.ToString());
                table.AddCell(item.Channal);


            }
            // Add total row at the end of the table
            table.AddCell("");  // Empty cell for NO column
            table.AddCell("");  // Empty cell for User Name column
            table.AddCell("");  // Empty cell for Branch/Department column
            table.AddCell("");  // Empty cell for Reference Number column
            table.AddCell("Total Amount");  // Label for the total
            table.AddCell(totalAmount.ToString("0.00"));  // Total amount value
            table.AddCell("");  // Empty cell for Channel column
            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "Indvisual Transactions Report.pdf");
        }
    }
}
