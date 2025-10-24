using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class TransactionReportViewModel
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Branch { get; set; }
        public string Process { get; set; }
        public decimal TotalTransactionAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal AmountLeft { get; set; }
        public string PercentageAchieved { get; set; }
    }
}