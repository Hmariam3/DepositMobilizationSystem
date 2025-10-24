using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class DistrictDepositViewModel
    {
        public string District { get; set; }
        public decimal TotalTarget { get; set; }
        public decimal TotalDeposit { get; set; }
        public string Type { get; set; } // Deposit, FCY, Merchant
    }
}