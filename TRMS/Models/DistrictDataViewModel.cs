using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class DistrictDataViewModel
    {
        public string District { get; set; }
        public decimal AmountCollected { get; set; }
        public decimal TotalTarget { get; set; }
    }

    public class DistrictCombinedReportViewModel
    {
        public List<DistrictDataViewModel> DepositPlanData { get; set; }
        public List<DistrictDataViewModel> DepositMerchantAgentData { get; set; }
        public List<DistrictDataViewModel> DepositFcy { get; set; }
    }

}