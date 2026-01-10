using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class DepositAchievementBreakdown
    {
        public string AccountNumber { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal TotalDeposited { get; set; }
        public decimal UserContribution { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal TotalWithdrawal { get; set; }
        public decimal AvailableFund { get; set; }
        public decimal UserAchieved { get; set; }
    }
}