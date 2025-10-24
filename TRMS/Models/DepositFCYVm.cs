using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    [MetadataType(typeof(DepositFCYVm))]
    public partial class DepositFCY
    {
    }
    public class DepositFCYVm
    {
        [Required(ErrorMessage = "The {0} Field Is Required")]
        [Display(Name = "Account Number")]
        [StringLength(13, ErrorMessage = "The {0} Exceded The Maximum Characters Allowed.")]
        public string AccountNumber { get; set; }
        [Required(ErrorMessage = "The {0} Field Is Required")]
        [Display(Name = "Refernce Number")]
        [StringLength(12, ErrorMessage = "The {0} Exceded The Maximum Characters Allowed.")]
        public string RefernceNumber { get; set; }
    }
}