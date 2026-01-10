using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models.Report
{
    public class EcoShareReportVM
    {
        public int DID { get; set; }
        public string FullName { get; set; }   // ★ NEW COLUMN
        public string Process { get; set; }
        public string District { get; set; }
        public string Branch { get; set; }
        public string AccountNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime? RefDate { get; set; }
        public string AccountHolder { get; set; }
        public string Narative { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}