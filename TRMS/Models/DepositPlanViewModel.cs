using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace TRMS.Models
{
    public class DepositPlanViewModel
    {
        public DepositPlan DepositPlan { get; set; }

        public string DepositType { get; set; } // "Individual" or "Shared"

        // Shared user list (max 3)
        public string SharedUser1 { get; set; }
        public decimal? SharedAmount1 { get; set; }

        public string SharedUser2 { get; set; }
        public decimal? SharedAmount2 { get; set; }

        public string SharedUser3 { get; set; }
        public decimal? SharedAmount3 { get; set; }

        public bool ShareDeposit { get; set; } // <-- NEW
    }
}
