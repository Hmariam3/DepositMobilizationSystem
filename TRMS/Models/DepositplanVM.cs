using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    [MetadataType(typeof(DepositplanVM))]
    public partial class DepositPlan
    {
    }
    public class DepositplanVM
    {
        [Required(ErrorMessage = "The {0} Field Is Required")]
        [Display(Name = "Target Amount")]
        public decimal Amount { get; set; }
       
        [Required(ErrorMessage = "The {0} Field Is Required")]
        [StringLength(30, ErrorMessage = "The {0} Exceded The Maximum Characters Allowed.")]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }
        
    }
}