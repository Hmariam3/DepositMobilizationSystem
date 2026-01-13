
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web.Mvc;
using System.Web.Security;
using TRMS.ADautho;
using TRMS.Models;
using TRMS.Security;

namespace UserProfile.Controllers
{
    public class UserProfileController : Controller
    {
        private TRMSEntities db = new TRMSEntities();
        ActiveDirectoryHelper adHelper = new ActiveDirectoryHelper();
        ActiveDirectoryHelperEmail admail = new ActiveDirectoryHelperEmail();
   
        public string Username { get; private set; }

        // GET: Users

        public ActionResult Index()
        {
            string role = "";
            if (Session["UserRole"] != null)
            {
               role = Session["UserRole"].ToString();

                string dbrole = db.Users.Where(u => u.Role == "Admin").FirstOrDefault().Role;
                if (role.Trim()== "Admin")
                {
                    return View(db.Users.ToList());
                }
                else
                {
                    return RedirectToAction("UnAuthorized", "Home");
                }
                
            }
            //else
            //{
            //    // Handle the case where the session is null
            //    return RedirectToAction("UnAuthorized", "Home");
            //}
            //if (role == "Admin")
            //{
            //    return View(db.Users.ToList());
            //}
            //else
            //{
            //    return RedirectToAction("UnAuthorized", "Home");
            //}
            return View(db.Users.ToList());

        }

        // GET: Users/Details/5
        [AuthorizeRolesAdmin("Admin")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
        [AuthorizeRolesAdmin("Admin")]
        [HttpGet]
        public ActionResult AddUser()
        {

            ViewBag.departmentID = new SelectList(db.DepartementInfoes, "DID", "DepartemntName");
            return View();
        }
        [AuthorizeRolesAdmin("Admin")]
        public ActionResult Detailsuser(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Example:
            List<CreateUserVM> userlistlist = new List<CreateUserVM>();
            string depNeme = db.DepartementInfoes.Where(d => d.DID == id).FirstOrDefault().DepartemntName;
            List<string> user = adHelper.GetUserListInDepartment(depNeme);
            for (int i = 0; i < user.Count; i++)
            {
                var UserName = user[i];
                var UserId = i + 1;

                var userinfo = new CreateUserVM
                {
                    UserID = UserId,
                    LoginName = UserName
                };

                userlistlist.Add(userinfo);


            }
            var data1 = userlistlist.Select(c => new SelectListItem
            {
                Value = c.LoginName.ToString(),
                Text = c.LoginName
            });
            // Return the data as JSON
            return Json(data1, JsonRequestBehavior.AllowGet);


        }


        [AuthorizeRoles("Admin")]
        [HttpPost]
        public ActionResult AddUser(User userInput)
        {
            ViewBag.departmentID = new SelectList(db.DepartementInfoes, "DID", "DepartemntName");
            userInput.MailAdress = admail.AuthenticateUserGetemail(userInput.UserName);
            if (ModelState.IsValid)
            {
                if (!loginNameExist(userInput.UserName))
                {
                    User user = new User();
                    user.UserName = userInput.UserName;
                    user.Role = userInput.Role;
                    user.MailAdress = Username;
                    user.MailAdress = userInput.MailAdress;
                    db.Users.Add(user);
                    db.SaveChanges();
                    TempData["userCreated"] = "User Has Been Created with Name  " + user.UserName;
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    ModelState.AddModelError("", "This username already exists.");
                    // ViewBag.departmentID = new SelectList(db.Departments, "ID", "DepartmentName");
                }
            }
            return View();
        }
        [NonAction]
        public bool loginNameExist(string loginName)
        {
            return db.Users.Where(m => m.UserName.Equals(loginName)).Any();
        }
        //[AuthorizeRoles("Admin")]
        public ActionResult Create()
        {

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");
            //ViewBag.Postion = new SelectList(db.Postions, "Postion1", "Postion1");
            ViewBag.Processes = new SelectList(db.Processes, "proces_id", "process_name");
            ViewBag.SubProcesses = new SelectList(db.Subprocesses, "subprocess_Id", "subprocess_name");
            string role = Session["UserRole"].ToString();
         
                return View();
       

           
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[AuthorizeRolesAdmin("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Create(User userInput)
        {
            ViewBag.Branch = new SelectList(db.DepartementInfoes, "DID", "DepartemntName");
            //ViewBag.Postion = new SelectList(db.Postions, "Postion1", "Postion1");


            if (ModelState.IsValid)
            {
                int exist = db.Users.Where(u => u.UserName == userInput.UserName).Count();
                if (exist > 0)
                {
                    TempData["errormessage"] = "User Name   Is Exist";
                    return View(userInput);
                }
                if (userInput.Role == null)
                {
                    TempData["errormessage"] = "Please Select Role";
                    return View(userInput);
                }
                if (userInput.Branch == null)
                {
                    TempData["errormessage"] = "Please Select Branch Proccess";
                    return View(userInput);
                }
                else
                {
                    //userInput.MailAdress = admail.AuthenticateUserGetemail(userInput.UserName);
                    if (ModelState.IsValid)
                    {
                        if (!loginNameExist(userInput.UserName))
                        {
                            User user = new User();
                            user.UserName = userInput.UserName;
                            user.FullName = userInput.FullName;
                          
                            user.Password = "123456";
                            user.Role = userInput.Role;

                            //decimal userlimit = db.Postions.Where(p => p.Postion1 == userInput.Postion).FirstOrDefault().LimitAmout.Value;
                            //user.DepositTargetAmount = userlimit;
                            if (userInput.Branch == "")
                            {
                                user.Branch = "";
                            }
                            else
                                {
                                string branchName = db.DepartementInfoes.Where(b => b.DID.ToString() == userInput.Branch).FirstOrDefault().DepartemntName;
                                user.Branch = branchName;
                            }
                            if (userInput.Branch == null)
                            {
                                user.Branch = "";
                            }
                            else
                            {
                                string branchName = db.DepartementInfoes.Where(b => b.DID.ToString() == userInput.Branch).FirstOrDefault().DepartemntName;
                                user.Branch = branchName;
                            }
                            user.Postion = userInput.Postion;
                            user.MailAdress = userInput.MailAdress;
                            user.CreatedDate = DateTime.Now;
                            db.Users.Add(user);
                            db.SaveChanges();
                            admail.emailRegisterd(user.MailAdress);
                            TempData["userCreated"] = "User Has Been Created with Name  " + user.UserName;
                            return RedirectToAction("Index", "User");
                        }
                        else
                        {
                            ModelState.AddModelError("", "This username already exists.");
                            // ViewBag.departmentID = new SelectList(db.Departments, "ID", "DepartmentName");
                        }
                    }
          
                    return RedirectToAction("Index");
                }
            }

            return View(userInput);
        }

        public string GetClientIpAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1"; // Optionally map to the IPv4 equivalent
            }

            return ipAddress;
        }

        public ActionResult Edit(int? id)
        {

            ViewBag.Branch = new SelectList(
            db.Users.Select(t => new { Branch = t.Branch }).Distinct().ToList(),
            "Branch",
            "Branch"
        );

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
          
         
            string role = Session["UserRole"].ToString();
            if (role == "Admin")
            {
                return View(user);
            }
            else
            {
                return RedirectToAction("UnAuthorized", "Home");
            }
    
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User user)
        {
            ViewBag.Branch = new SelectList(
         db.Users.Select(t => new { Branch = t.Branch }).Distinct().ToList(),
         "Branch",
         "Branch"
     );
            if (ModelState.IsValid)
            {
                if (user.Role == null)
                {
                    TempData["errormessage"] = "Please Select Role";
                    return View(user);
                }
                if (user.Branch == null)
                {
                    TempData["errormessage"] = "Please Select Sub Proccess";
                    return View(user);
                }
                else
                {
                    if (user.Branch == "")
                    {
                        user.Branch = "";
                    }
                    else
                    {
                        user.Branch = user.Branch;
                    }
                    if (user.Branch == null)
                    {
                        user.Branch = "";
                    }
                    else
                    {
                        user.Branch = user.Branch;
                    }
                    User user1 = db.Users.Where(u=>u.ID==user.ID).FirstOrDefault();

                    user1.FullName = user.FullName;
                    user1.UserName = user.UserName;
                    user1.Role = user.Role;
                    user1.Branch = user.Branch;
                    user1.Password = "123456";
                    db.Entry(user1).State = EntityState.Modified;
                    db.SaveChanges();
                    //admail.emailRegisterd(user.MailAdress);
                    TempData["userUpdated"] = "User Has Been Updated with Name  " + user.UserName;
                    return View(user);
                }
            }
           
            
            return View(user);
        }
       
       

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(User userInput)
        {
            string ip = GetClientIpAddress();
            Session["Login"] = "0";
            if (userInput.UserName == null)
            {
                ViewBag.Error = "User Name Not Null.";
                return View();
            }
            if (userInput.Password == null)
            {
                ViewBag.Error = "Passsword Not Null.";
                return View();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        string isAuthenticated = "";
                        if (userInput.UserName == "shimelislw1" || userInput.UserName == "DriversH1" || userInput.UserName == "DriversHBI1" || userInput.UserName == "DriversHFE1" || userInput.UserName == "bikiladch" || userInput.UserName == "hailemariamkM")
                        {
                            var userid = db.Users.Where(id => id.UserName.Trim() == userInput.UserName.Trim() && id.Password.Trim() == userInput.Password.Trim());
                            if (userid.Count() > 0)
                            {
                                isAuthenticated = "true";
                            }
                            else
                            {
                                ViewBag.Error = "invalid user or password";
                            }
                        }
                        else
                        {
                            //isAuthenticated = adHelper.AuthenticateUsers(userInput.UserName, userInput.Password);
                        }

                        //string isAuthenticated = "true";
                        if (isAuthenticated == "true")
                        {
                            string userName = "";
                            var user = db.Users.FirstOrDefault(u => u.UserName.Trim() == userInput.UserName.Trim());
                            if (user == null)
                            {
                                var useremail = db.Users.FirstOrDefault(u => u.MailAdress == userInput.UserName);
                                if (useremail == null)
                                {
                                    ViewBag.Error = "User Does't Exist Contact the administrator.";
                                }
                                else
                                {
                                    userName= useremail.UserName;
                                }
                            }
                            else
                            {
                                userName = user.UserName;
                            }
                            if (userName == "")
                            {
                             ViewBag.Error = "User Does't Exist or Incorrect User Please Contact the administrator.";
                            }
                            else
                            {
                                FormsAuthentication.SetAuthCookie(userInput.UserName.ToUpper(), false);
                                Session["UserName"] = userName;

                                Session["UserRole"] = db.Users.Where(u => u.UserName == userName).FirstOrDefault().Role.ToString();
                                //string rol = Session["UserRole"].ToString();
                                System.Web.HttpContext.Current.Cache["UserID"] = userName;
                                //
                                FormsAuthentication.SetAuthCookie(userInput.UserName.ToUpper(), false);
                                Session["username"] = userName.ToUpper();
                                int userprocc = db.Users.Where(u => u.UserName == userName.Trim() && u.Branch != null).Count();


                                Session["UserRole"] = db.Users.Where(u => u.UserName == userName.Trim()).FirstOrDefault().Role.ToString();
                                Session["Userid"] = db.Users.Where(u => u.UserName == userName.Trim()).FirstOrDefault().ID.ToString();
                                Session["Login"] = "1";
                                //HttpContext.CurrentHandler. = HttpContext.Session["CurrentUser"];
                                Session["UserHomeBranch"] = db.Users.Where(u => u.UserName == userName.Trim()).FirstOrDefault().Branch.ToString();
                                Session["District"] = db.Users.Where(u => u.UserName == userName.Trim()).FirstOrDefault().District.ToString();
                                Session["Process"] = db.Users.Where(u => u.UserName == userName.Trim()).FirstOrDefault().Process.ToString();
                                System.Web.HttpContext.Current.Cache["UserID"] = userName;
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            TempData["LoginError"] = isAuthenticated;
                        }

                    }
                    catch(Exception ex)
                    {
                        ViewBag.Error = ex.Message+"Contact the administrator.";
                        return View();
                    }
                }



                //if (ModelState.IsValid)
                //{
                //    try
                //    {
                //        var u = db.Users.Any();
                //    }
                //    catch
                //    {
                //        ViewBag.Error = "DB error.Please contact the administrator.";
                //        return View();
                //    }



                //    var userid = db.Users.Where(id => id.Username.Trim() == userInput.Username.Trim());

                //    string pass = EncryptPassword(userInput.Password, userInput.Username.Trim());
                //    if (loginAllowed(userInput.Username.Trim(), EncryptPassword(userInput.Password, userInput.Username.Trim())))
                //    {


                //        FormsAuthentication.SetAuthCookie(userInput.Username.ToUpper(), false);
                //        Session["username"] = userInput.Username.ToUpper();
                //        Session["UserRole"] = db.Users.Where(u => u.Username == userInput.Username.Trim()).FirstOrDefault().Role.ToString();
                //        Session["Userid"] = db.Users.Where(u => u.Username == userInput.Username.Trim()).FirstOrDefault().Id.ToString();
                //        Session["Login"] = "1";
                //        //HttpContext.CurrentHandler. = HttpContext.Session["CurrentUser"];

                //        Session["UserHomeBranch"] = db.Users.Where(u => u.Username == userInput.Username.Trim()).FirstOrDefault().Branch.ToString();

                //        System.Web.HttpContext.Current.Cache["UserID"] = userInput.Username;
                //        string isfirstLogin = db.Users.Where(u => u.Username == userInput.Username.Trim()).FirstOrDefault().Islogin.ToString();
                //        bool firstLogin = Convert.ToBoolean(isfirstLogin);

                //        if (firstLogin)
                //        {
                //            return RedirectToAction("Index", "Home");

                //        }

                //        else
                //        {
                //            return RedirectToAction("ChangePassword", "User");

                //        }
                //    }
                //    else
                //    {
                //        ViewBag.Error = "invalid user or password";
                //    }

                //}
                //else
                //{
                //    ViewBag.Error = "Invalid Login. Try Again";
                //}


            }

            return View();
        }


        [NonAction]
        public bool loginAllowed(string loginName, string password)
        {
            return db.Users.Where(m => m.UserName.Equals(loginName.ToUpper()) && m.Password.Equals(password)).Any();
        }
        [NonAction]
        public static string EncryptPassword(string password, string username)

        {
            string salt = "16characterslong12345" + username.ToUpper();

            SHA1CryptoServiceProvider hasher = new SHA1CryptoServiceProvider();

            byte[] textWithSaltBytes = System.Text.Encoding.UTF8.GetBytes(string.Concat(password, salt));

            byte[] hashedBytes = hasher.ComputeHash(textWithSaltBytes);

            hasher.Clear();

            return Convert.ToBase64String(hashedBytes);

        }
        [AllowAnonymous]
        public ActionResult Logout()
        {

            FormsAuthentication.SignOut();
            Session["username"] = "";
            Session.Clear();
            Session.Abandon();
           
            return RedirectToAction("Login", "User");
        }
        [AllowAnonymous]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]

        public ActionResult ChangePassword(ChangePasswordVM changePassInput)
        {
            if (ModelState.IsValid)
            {
                string username = Session["username"].ToString().ToLower();
                User user = db.Users.Where(u => u.UserName.Equals(username)).FirstOrDefault();

                String encryptedPassword = EncryptPassword(changePassInput.OldPassword, username);

                if (user.Password.Equals(encryptedPassword))
                {
                    user.Password = EncryptPassword(changePassInput.Password, username);
                    //user.ModifiedBy = User.Identity.Name;
                    //user.ModifiedAt = DateTime.Now;
                
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    ViewBag.Confirm = "Password changed successfully.";
                }
                else
                {
                    ModelState.AddModelError("", "Old Password is incorrect.");
                }

            }
            return View(changePassInput);
        }

        // GET: Users/Delete/5
        [AuthorizeRoles("Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
      
        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        public JsonResult GetSubprocessesByProcessId(int processId)
        {
            var subprocesses = db.Subprocesses.Where(s => s.process_Id == processId).Select(s => new { s.subprocess_Id, s.subprocess_name }).ToList();
            return Json(subprocesses, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDepartmentsBySubprocessId(int subprocessId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Received subprocessId: {subprocessId}");
                var branches = db.Branches
                    .Where(b => b.SubProcess_id == subprocessId)
                    .Select(b => new { b.ID, b.BranchName })
                    .ToList();
                return Json(branches, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error in GetDepartmentsBySubprocessId: {ex.Message}");
                return Json(new { error = "An error occurred while fetching branches." }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}