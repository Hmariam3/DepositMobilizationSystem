using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TRMS.Models
{
    public class loginVM
    {
        [Key]
        public int UserID { get; set; }
        [Display(Name = "UserName")]
        [Required(ErrorMessage = "*")]
        public string LoginName { get; set; }
        [Display(Name = "Password")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
    public class CreateUserVM
    {
        [Key]
        public int UserID { get; set; }
        [Required(ErrorMessage = "The {0} field is required")]
        [StringLength(15, ErrorMessage = "The {0} exceded the maximum characters allowed.")]
        [Display(Name = "UserName")]
        public string LoginName { get; set; }
        [Required(ErrorMessage = "The {0} field is required")]
        public int DepartmentID { get; set; }
        [Required(ErrorMessage = "The {0} field is required")]
        public string Role { get; set; }
        [Display(Name = "Email Adress")]
        public string MailAdress { get; set; }
        public string Postion { get; set; }
    }
}