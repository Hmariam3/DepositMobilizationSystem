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
    public class DepositDistrictReportController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        [AuthorizeRoles("Admin", "Maker", "District","SuperUser")]
        // GET: Transactions with Filters
        public ActionResult DistrictPlanReport(DateTime? startDate, DateTime? endDate)
        {

            string districtUser = Session["District"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositPlans.AsQueryable();
            decimal totalAmount = 0;

            if (userPosition == "Director" && !string.IsNullOrEmpty(districtUser))
            {
                   transactions = transactions.Where(t => t.District.Trim() == districtUser.Trim());
                   totalAmount = transactions.Where(t => t.District.Trim() == districtUser.Trim()).Select(t => (decimal?)t.AccountBalance -t.IntialAccountBalance + t.Amount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u=> (decimal?)u.AccountBalance-u.IntialAccountBalance + u.Amount) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }
        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult DistrictAgenetReport(DateTime? startDate, DateTime? endDate)
        {
            string districtUser = Session["District"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositMerchantAgenets.AsQueryable();
            decimal totalAmount = 0;

            if (userPosition == "Director" && !string.IsNullOrEmpty(districtUser))
            {
                transactions = transactions.Where(t => t.District.Trim() == districtUser.Trim());
                totalAmount = transactions.Where(t => t.District.Trim() == districtUser.Trim()).Select(t => (decimal?)t.AccountBalance- t.IntialAccountBalance + t.Target).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.AccountBalance- u.IntialAccountBalance + u.Target) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }

        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult DistrictFcyReport(DateTime? startDate, DateTime? endDate)
        {
            string districtUser = Session["District"].ToString();
            string userPosition = Session["Position"].ToString();
            var transactions = db.DepositFCies.AsQueryable();
            decimal totalAmount = 0;

            if ( userPosition == "Director" && !string.IsNullOrEmpty(districtUser))
            {
                transactions = transactions.Where(t => t.District.Trim() == districtUser.Trim());
                totalAmount = transactions.Where(t => t.District.Trim() == districtUser.Trim()).Select(t => (decimal?)t.TransactionAmount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.TransactionAmount) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }




        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // Export to Excel
        public ActionResult ExportDepositPlan(DateTime? startDate, DateTime? endDate)
        {

            string district = Session["District"].ToString();
            var transactions = db.DepositPlans.AsQueryable();
            decimal totalAmount = 0;
            decimal totalAmountintialBalance = 0;
            decimal totalAmountCurrentBalance = 0;
            if (!string.IsNullOrEmpty(district))
            {
                transactions = transactions.Where(t => t.District.Trim() == district.Trim());
                totalAmount = transactions.Where(t => t.District.Trim() == district.Trim()).Select(t => (decimal?)t.Amount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.Amount) ?? 0m;
            }
            ViewBag.totalAmount = totalAmount;

            if (transactions.Count() > 0)
            {
                totalAmountintialBalance = transactions.Sum(t => t.IntialAccountBalance);
                totalAmountCurrentBalance = transactions.Sum(t => t.AccountBalance);
            }

            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("District Deposit Report"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "District";
                ws.Cell(1, 3).Value = "Branch";
                ws.Cell(1, 4).Value = "Target";
                ws.Cell(1, 5).Value = "Account Number";
                ws.Cell(1, 6).Value = "Reference Number";
                ws.Cell(1, 7).Value = "Account Holder";
                ws.Cell(1, 8).Value = "Intial Account Balance";
                ws.Cell(1, 9).Value = "Account Balance";
                ws.Cell(1, 10).Value = "Maker";
                ws.Cell(1, 11).Value = "Transaction Date";

                int row = 2;
                int no = 1;
                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.District;
                    ws.Cell(row, 3).Value = item.Branch;
                    ws.Cell(row, 4).Value = item.Amount;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 5).Value = item.AccountNumber;
                    ws.Cell(row, 6).Value = item.ReferenceNumber;
                    ws.Cell(row, 7).Value = item.AccountHolder;

                    ws.Cell(row, 8).Value = item.IntialAccountBalance;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 10).Value = item.AccountBalance;
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 12).Value = item.User;
                    ws.Cell(row, 13).Value = item.CreatedDate.ToString("yyyy-MM-dd"); // Prevents null error
                    row++;
                    no++;
                }

                // Add total amount at the end of the table
                ws.Cell(row, 3).Value = "Total Amount";  // Label for the total
                ws.Cell(row, 4).Value = totalAmount;     // Total amount sum
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency

                // intial balance
                ws.Cell(row, 7).Value = totalAmountintialBalance;     // Total amount sum
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency
                                                                        // currenet balance
                ws.Cell(row, 8).Value = totalAmountCurrentBalance;     // Total amount sum
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency


                ws.Columns().AdjustToContents(); // Autofit column widths

                var stream = new MemoryStream(); // Create memory stream
                wb.SaveAs(stream, false); // **Fix: Prevent stream from closing**
                stream.Position = 0; // Reset stream position

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "District Deposit Report.xlsx");
            }


        }

        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // Export to Excel
        public ActionResult ExportDepositAgent(DateTime? startDate, DateTime? endDate)
        {

            string district = Session["District"].ToString();
            var transactions = db.DepositMerchantAgenets.AsQueryable();
            decimal totalAmount = 0;
            decimal totalAmountintialBalance = 0;
            decimal totalAmountCurrentBalance = 0;
            if (!string.IsNullOrEmpty(district))
            {
                transactions = transactions.Where(t => t.District.Trim() == district.Trim());
                totalAmount = transactions.Where(t => t.District.Trim() == district.Trim()).Select(t => (decimal?)t.Target).Sum() ?? 0m;

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
                var ws = wb.Worksheets.Add("District Merchant Report"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "District";
                ws.Cell(1, 3).Value = "Branch";
                ws.Cell(1, 4).Value = "Target";
                ws.Cell(1, 5).Value = "Account Number";
                ws.Cell(1, 6).Value = "Reference Number";
                ws.Cell(1, 7).Value = "Account Holder";
                ws.Cell(1, 8).Value = "Intial Account Balance";
                ws.Cell(1, 9).Value = "Account Balance";
                ws.Cell(1, 10).Value = "Maker";
                ws.Cell(1, 11).Value = "Transaction Date";

                int row = 2;
                int no = 1;
                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.District;
                    ws.Cell(row, 3).Value = item.Branch;
                    ws.Cell(row, 4).Value = item.Target;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 5).Value = item.AccountNumber;
                    ws.Cell(row, 6).Value = item.ReferenceNumber;
                    ws.Cell(row, 7).Value = item.AccountHolder;

                    ws.Cell(row, 8).Value = item.IntialAccountBalance;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 10).Value = item.AccountBalance;
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 12).Value = item.CreatedBY;
                    ws.Cell(row, 13).Value = item.CreatedDate.ToString("yyyy-MM-dd"); // Prevents null error
                    row++;
                    no++;
                }

                // Add total amount at the end of the table
                ws.Cell(row, 3).Value = "Total Amount";  // Label for the total
                ws.Cell(row, 4).Value = totalAmount;     // Total amount sum
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency

                // intial balance
                ws.Cell(row, 7).Value = totalAmountintialBalance;     // Total amount sum
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency
                                                                        // currenet balance
                ws.Cell(row, 8).Value = totalAmountCurrentBalance;     // Total amount sum
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency


                ws.Columns().AdjustToContents(); // Autofit column widths

                var stream = new MemoryStream(); // Create memory stream
                wb.SaveAs(stream, false); // **Fix: Prevent stream from closing**
                stream.Position = 0; // Reset stream position

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "District Merchant Report.xlsx");
            }


        }

        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
        // Export to Excel
        public ActionResult ExportDepositFcy(DateTime? startDate, DateTime? endDate)
        {
            string district = Session["District"].ToString();
            var transactions = db.DepositFCies.AsQueryable();
            decimal totalAmount = 0;

            if (!string.IsNullOrEmpty(district))
            {
                transactions = transactions.Where(t => t.District.Trim() == district.Trim());
                totalAmount = transactions.Where(t => t.District.Trim() == district.Trim()).Select(t => (decimal?)t.TransactionAmount).Sum() ?? 0m;

            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);
                totalAmount = transactions.Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate).Sum(u => (decimal?)u.TransactionAmount) ?? 0m;
            }

            ViewBag.totalAmount = totalAmount;
  
            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("District  Fcy Report"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "District";
                ws.Cell(1, 3).Value = "Branch";
                ws.Cell(1, 4).Value = "Transaction Amount";
                ws.Cell(1, 5).Value = "Account Number";
                ws.Cell(1, 6).Value = "Transaction Refernce";
                ws.Cell(1, 7).Value = "Maker";
                ws.Cell(1, 8).Value = "Transaction Date";

                int row = 2;
                int no = 1;
                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.District;
                    ws.Cell(row, 3).Value = item.Branch;
                    ws.Cell(row, 4).Value = item.TransactionAmount;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 5).Value = item.AccountNumber;
                    ws.Cell(row, 6).Value = item.RefernceNumber;
                    ws.Cell(row, 9).Value = item.User;
                    ws.Cell(row, 10).Value = item.CreatedDate.ToString("yyyy-MM-dd"); // Prevents null error
                    row++;
                    no++;
                }

                // Add total amount at the end of the table
                ws.Cell(row, 3).Value = "Total Amount";  // Label for the total
                ws.Cell(row, 4).Value = totalAmount;     // Total amount sum
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency
                ws.Columns().AdjustToContents(); // Autofit column widths

                var stream = new MemoryStream(); // Create memory stream
                wb.SaveAs(stream, false); // **Fix: Prevent stream from closing**
                stream.Position = 0; // Reset stream position

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "District Fcy Report.xlsx");
            }


        }


        [AuthorizeRoles("Admin", "Maker", "District", "SuperUser")]
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
