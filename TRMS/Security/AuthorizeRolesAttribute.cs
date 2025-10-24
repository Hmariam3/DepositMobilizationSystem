using TRMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;




namespace TRMS.Security
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        private readonly string[] AllowedRoles; // set of roles listed in [Authorize] annotation

        private TRMSEntities db = new TRMSEntities();
        private readonly string[] allowedroles;
        public AuthorizeRolesAttribute(params String[] roles)
        {
            this.allowedroles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
          
         
               string name = System.Web.HttpContext.Current.Cache["UserID"].ToString();
           
                bool authorize = false;
         
                foreach (var role in allowedroles)
                {
                    var userrole = db.Users.Where(m => m.UserName == name && m.Role == role).ToList();
                    if (userrole.Count() > 0)
                    {
                        authorize = true; /* return true if Entity has current user(active) with specific role */
                    }
                }
         
            return authorize;
        }

        //public void StoreUserPagesInSession(String userName)
        //{

        //    var role = from u in db.Users
        //               where u.LoginName == userName
        //               select new { u };
        //    String roles = "";
        //    foreach (var item in role.Distinct())
        //    {
        //        roles += item.u.UserRoles;
        //    }
        //    HttpContext.Current.Session["Role"] = roles;

        //}



        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Home/UnAuthorized");
        }




    }
}