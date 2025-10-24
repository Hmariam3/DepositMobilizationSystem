using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class BranchReportViewModel
    {
        public string District { get; set; }
        public string Branch { get; set; }
        public decimal AmountCollected { get; set; }
        public decimal TotalTarget { get; set; }
    }
    public class BranchDataViewModel
    {
        public string Branch { get; set; }
        public decimal AmountCollected { get; set; }
        public decimal TotalTarget { get; set; }
    }
    public class CombinedReportViewModel
    {
        public List<BranchDataViewModel> DepositPlanData { get; set; }
        public List<BranchDataViewModel> DepositMerchantAgentData { get; set; }
        public List<BranchDataViewModel> Depositfcy { get; set; }
    }
}