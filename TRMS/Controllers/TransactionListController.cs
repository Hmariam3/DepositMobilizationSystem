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
    public class TransactionListController : Controller
    {
        private TRMSEntities db = new TRMSEntities();

        //[AuthorizeRoles("Admin", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndexReport(DateTime? startDate, DateTime? endDate, string branch)
        {
            var transactions = db.Transactions.AsQueryable();
            decimal totalAmount = transactions.Sum(s => s.TransactionAmount).Value;

            if (!string.IsNullOrEmpty(branch))
            {
                if (branch == "All")
                {

                    totalAmount = transactions.Sum(s => s.TransactionAmount).Value;
                }
                else
                {
                    transactions = transactions.Where(t => t.UserBranch == branch);
                    totalAmount = transactions.Where(t => t.UserBranch == branch).Sum(s => s.TransactionAmount).Value;
                }
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                var exist = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate).ToList();
                if (exist.Count() > 0)
                {
                    transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
                    totalAmount = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate).Sum(s => s.TransactionAmount.Value);
                }
            }
            ViewBag.totalAmount = totalAmount;
            ViewBag.Branches = db.Transactions.Select(t => t.UserBranch).Distinct().ToList();
            return View(transactions.ToList());
        }
        // by proccess and branch 
        public ActionResult reportbysubProccess(DateTime? startDate, DateTime? endDate, string suproccess)
        {

            List<Transaction> tran = new List<Transaction>();
            string UserName = "";
            if (Session["UserName"] != null)
            {
                UserName = Session["UserName"].ToString();
            }
            else
            {
                return RedirectToAction("login", "User");
            }
            decimal totalAmount = 0;
            ViewBag.Branches = db.Users.Where(t => t.UserName.Trim() == UserName.Trim()).Select(t => t.Branch).Distinct().ToList();
            if (!string.IsNullOrEmpty(suproccess))
            {
                var transactions = db.Transactions.Where(t => t.UserBranch == suproccess);
                totalAmount = transactions.Where(t => t.UserBranch == suproccess).Sum(s => s.TransactionAmount).Value;


                if (startDate.HasValue && endDate.HasValue)
                {
                    var exist = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate && t.UserBranch == suproccess).ToList();
                    if (exist.Count() > 0)
                    {
                        exist = exist.Where(t => t.TranDate >= startDate && t.TranDate <= endDate && t.UserBranch == suproccess).ToList();
                        totalAmount = exist.Where(t => t.TranDate >= startDate && t.TranDate <= endDate && t.UserBranch == suproccess).Sum(s => s.TransactionAmount.Value);
                    }
                    ViewBag.totalAmount = totalAmount;
                    return View(exist.ToList());
                }
                else
                {
                    ViewBag.totalAmount = totalAmount;
                    return View(transactions.ToList());
                }

            }

            return View(tran.ToList());
        }


        //[AuthorizeRoles("Admin", "SuperUser")]
        // GET: Transactions with Filters
        public ActionResult IndexReportByprocess(DateTime? startDate, DateTime? endDate, string Process)
        {
            var transactions = db.Transactions.AsQueryable();
            decimal totalAmount = transactions.Sum(s => s.TransactionAmount).Value;
            if (!string.IsNullOrEmpty(Process))
            {
                if (Process == "All")
                {

                    totalAmount = transactions.Sum(s => s.TransactionAmount).Value;
                }
                else
                {
                    transactions = transactions.Where(t => t.Process.Trim() == Process.Trim());
                    totalAmount = transactions.Where(t => t.Process.Trim() == Process.Trim()).Sum(s => s.TransactionAmount).Value;
                }
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                var exist = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate).ToList();
                if (exist.Count() > 0)
                {
                    transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
                    totalAmount = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate).Sum(s => s.TransactionAmount.Value);
                }
            }
            ViewBag.totalAmount = totalAmount;
            ViewBag.Process = db.Transactions.Select(t => t.Process).Distinct().ToList();
            return View(transactions.ToList());
        }





        //[AuthorizeRoles("Admin", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcel(DateTime? startDate, DateTime? endDate, string branch)
        {

            var transactions = db.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(branch))
            {
                transactions = transactions.Where(t => t.UserBranch == branch);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
            }
            // Calculate the total amount
            decimal totalAmount = transactions.Sum(t => t.TransactionAmount ?? 0);
            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Transactions"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "User Name";
                ws.Cell(1, 3).Value = "Process";
                ws.Cell(1, 4).Value = "Branch/SubProccess";
                ws.Cell(1, 5).Value = "Reference Number";
                ws.Cell(1, 6).Value = "Credit Account";
                ws.Cell(1, 7).Value = "Amount";
                ws.Cell(1, 8).Value = "Channel";
                ws.Cell(1, 9).Value = "Type";
                ws.Cell(1, 10).Value = "Remark";
                ws.Cell(1, 11).Value = "Transaction Date";

                int row = 2;
                int no = 1;
                foreach (var item in transactions)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = item.UserName;
                    ws.Cell(row, 3).Value = item.Process;
                    ws.Cell(row, 4).Value = item.UserBranch;
                    ws.Cell(row, 5).Value = item.ReferenceNumber;
                    ws.Cell(row, 6).Value = item.CreditAccount;
                    ws.Cell(row, 7).Value = item.TransactionAmount;
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 8).Value = item.Channal;
                    ws.Cell(row, 9).Value = item.TransactionType;
                    ws.Cell(row, 10).Value = item.Remark;
                    ws.Cell(row, 11).Value = item.TranDate?.ToString("yyyy-MM-dd"); // Prevents null error
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

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", branch + "-" + "Transactions.xlsx");
            }

        }


        //[AuthorizeRoles("Admin", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcelByprocess(DateTime? startDate, DateTime? endDate, string process)
        {

            var transactions = db.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(process))
            {
                transactions = transactions.Where(t => t.Process == process);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
            }
            // Calculate the total amount
            decimal totalAmount = transactions.Sum(t => t.TransactionAmount ?? 0);
            using (var wb = new XLWorkbook()) // Create workbook
            {
                var ws = wb.Worksheets.Add("Transactions list"); // Add sheet

                // Table headers
                ws.Cell(1, 1).Value = "NO";
                ws.Cell(1, 2).Value = "User Name";
                ws.Cell(1, 3).Value = "Branch/Proccess";
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

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", process + "-" + "Transactions.xlsx");
            }

        }
        //
        //[AuthorizeRoles("Admin", "SuperUser")]
        // Export to Excel
        public ActionResult ExportToExcelBySubprocess(DateTime? startDate, DateTime? endDate, string suproccess)
        {

            var transactions = db.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(suproccess))
            {
                transactions = transactions.Where(t => t.UserBranch == suproccess);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = transactions.Where(t => t.TranDate >= startDate && t.TranDate <= endDate);
            }
            // Calculate the total amount
            decimal totalAmount = transactions.Sum(t => t.TransactionAmount ?? 0);
            using (var wb = new XLWorkbook()) // Create workbook
                {
                    var ws = wb.Worksheets.Add("Transactions list"); // Add sheet

                    // Table headers
                    ws.Cell(1, 1).Value = "NO";
                    ws.Cell(1, 2).Value = "User Name";
                    ws.Cell(1, 3).Value = "Branch/Proccess";
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

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", suproccess + "-" + "Transactions.xlsx");
                }
          
         
              
        } 


        //[AuthorizeRoles("Admin", "SuperUser")]
        // Export to PDF
        public ActionResult ExportToPdf(DateTime? startDate, DateTime? endDate, string branch)
        {
            var transactions = db.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(branch))
            {
                transactions = transactions.Where(t => t.UserBranch == branch);
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
            // Calculate the total amount
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

            return File(stream.ToArray(), "application/pdf", branch + "-"+"Transactions.pdf");
        }
    }
}
