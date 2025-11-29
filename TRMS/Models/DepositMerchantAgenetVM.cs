using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    [MetadataType(typeof(DepositMerchantAgenetVM))]
    public partial class DepositMerchantAgenet
    {
    }
    public class DepositMerchantAgenetVM
    {
        [Required(ErrorMessage = "The {0} Field Is Required")]
        [Display(Name = "Target Amount")]
        public decimal Target { get; set; }

        [Required(ErrorMessage = "The {0} Field Is Required")]
        [StringLength(30, ErrorMessage = "The {0} Exceded The Maximum Characters Allowed.")]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }
    }
}