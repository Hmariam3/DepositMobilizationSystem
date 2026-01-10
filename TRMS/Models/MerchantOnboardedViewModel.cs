using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class MerchantOnboardedViewModel
    {
        public string LinkAccount { get; set; }
        public string AccountHolder { get; set; }
        public string AccountNumber { get; set; }
        public decimal TotalDepositedByUser { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}