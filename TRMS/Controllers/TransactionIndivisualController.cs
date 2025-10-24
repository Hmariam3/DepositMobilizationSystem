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
    public class TransactionIndivisualController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndivsualReport(DateTime? startDate, DateTime? endDate)
        {
            string UserName = Session["UserName"].ToString();
            var transactions = db.Transactions.AsQueryable();
            decimal totalAmount = 0;

            if (!string.IsNullOrEmpty(UserName))
            {
                transactions = transactions.Where(t => t.UserName == UserName);
                totalAmount = transactions.Where(t => t.UserName == UserName).Sum(t => t.TransactionAmount) ?? 0;
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
                totalAmount = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate).Sum(u=>u.TransactionAmount) ?? 0;
            }
            ViewBag.totalAmount = totalAmount;

            return View(transactions.ToList());
        }
        [AuthorizeRoles("Admin", "Maker", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcel(DateTime? startDate, DateTime? endDate)
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
            decimal totalAmount = transactions.Sum(t => t.TransactionAmount ?? 0);
            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Indvisual Transactions Report"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "User Name";
                ws.Cell(1, 3).Value = "Branch/SubProcces";
                ws.Cell(1, 4).Value = "Reference Number";
                ws.Cell(1, 5).Value = "Credit Account";
                ws.Cell(1, 6).Value = "Amount";
                ws.Cell(1, 7).Value = "Channel";
                ws.Cell(1, 8).Value = "Type";
                ws.Cell(1, 9).Value = "Remark";
                ws.Cell(1, 10).Value = "Transaction Date";

                int row = 2;
                int no = 1;
                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.UserName;
                    ws.Cell(row, 3).Value = item.UserBranch;
                    ws.Cell(row, 4).Value = item.ReferenceNumber;
                    ws.Cell(row, 5).Value = item.CreditAccount;
                    ws.Cell(row, 6).Value = item.TransactionAmount;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 7).Value = item.Channal;
                    ws.Cell(row, 8).Value = item.TransactionType;
                    ws.Cell(row, 9).Value = item.Remark;
                    ws.Cell(row, 10).Value = item.TranDate?.ToString("yyyy-MM-dd"); // Prevents null error
                    row++;
                    no++;
                }

                // Add total amount at the end of the table
                ws.Cell(row, 5).Value = "Total Amount";  // Label for the total
                ws.Cell(row, 6).Value = totalAmount;     // Total amount sum
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00"; // Formatting for currency

                ws.Columns().AdjustToContents(); // Autofit column widths

                var stream = new MemoryStream(); // Create memory stream
                wb.SaveAs(stream, false); // **Fix: Prevent stream from closing**
                stream.Position = 0; // Reset stream position

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Indvisual Transactions Report.xlsx");
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
