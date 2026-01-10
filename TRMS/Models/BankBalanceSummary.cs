using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class BankBalanceSummary
    {
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal Difference { get; set; }
    }
}