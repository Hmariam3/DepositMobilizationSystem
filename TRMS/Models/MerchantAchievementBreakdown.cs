using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class MerchantAchievementBreakdown
    {
        public string LinkAccount { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal TotalTarget { get; set; }
        public decimal UserTarget { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal EstimatedWithdrawal { get; set; }
        public decimal AchievedAmount { get; set; }
    }
}