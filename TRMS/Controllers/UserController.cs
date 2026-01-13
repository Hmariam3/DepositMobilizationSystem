
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
    public class UserController : Controller
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

            ViewBag.Branch = new SelectList(db.DepartementInfoes, "DID", "DepartemntName");
            //ViewBag.Postion = new SelectList(db.Postions, "Postion1", "Postion1");

            string role = Session["UserRole"].ToString();
            if (role == "Admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("UnAuthorized", "Home");
            }
           
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


        // GET: User/Login
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        //[AllowAnonymous]
        //[HttpGet]
        //public ActionResult Login()
        //{
        //    return View();
        //}

        // POST: User/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User userInput)
        {
            // ADD THESE TWO LINES – this is the correct, secure way in MVC
            var unvalidatedForm = Request.Unvalidated.Form;
            string rawPassword = unvalidatedForm["Password"];   // bypasses request validation for Password only

            string ip = Request.UserHostAddress; // Simplified GetClientIpAddress
            Session["Login"] = "0";

            if (string.IsNullOrEmpty(userInput.UserName))
            {
                ViewBag.Error = "User Name cannot be empty.";
                return View();
            }
            if (string.IsNullOrEmpty(rawPassword))
            {
                ViewBag.Error = "Password cannot be empty.";
                return View();
            }

            // 🔐 JUMP / EMERGENCY ADMIN LOGIN (BYPASS AD & DB)
            if (userInput.UserName.Trim().Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
                && rawPassword == "Hmariam@2750")
            {
                FormsAuthentication.SetAuthCookie("ADMIN", false);

                // ✅ SAMPLE SESSION VALUES
                Session["UserName"] = "hailemariamk";
                Session["FullName"] = "Hailemariam Kebede Mamo";
                Session["UserRole"] = "Super";
                Session["Userid"] = "18964"; // system user
                Session["UserHomeBranch"] = "HO";
                Session["District"] = "ALL";
                Session["Process"] = "SYSTEM";
                Session["Position"] = "Back Office Applications Administrator";
                Session["Email"] = "admin@system.local";
                Session["MemberSince"] = DateTime.Now;
                Session["Login"] = "1";

                System.Web.HttpContext.Current.Cache["UserID"] = "ADMIN";

                return RedirectToAction("Index", "Home");
            }
            try
            {
                bool isAuthenticated = false;
                ActiveDirectoryHelper.ADUser adUser = null;


                // ✅ List of local (non-AD) users allowed to log in directly from DB
                var localUsers = new List<string>
                    {
                        "abdisawage","abelzebi","abenetatge","abrahfedu","abrekekege","alemleol","amanuegku","ashengogu",
                        "asnamede","bayiskege","binifiab","birhaabo","bisragibe","dawittebe","dejehade","dimaamwa","diriduwa",
                        "eliyadeti","elsatene","engedage","ermihal","estikema","esubagibi","eyobseha","firechge","gemehaay",
                        "gadibehi","gelashgo","geledage","fayebewe","girmhuke","hikaaleje","kebegeyo","legearfi","oliyaalmu",
                        "shelguche","shibebmi","shummara","surahuti","tadefide","tamiabge","tolagibi","waktimbe","wagageki",
                        "wesefebe","yeromobu","solosubi","dereabte","yabstoof","dinafile","adefalbu", "dtarressa","zebba",
                        "tayele","zgudito","meticha","lfila", "enmekuriya"
                    };

                // ✅ Check if the user is one of the local (non-AD) users
                if (localUsers.Contains(userInput.UserName.Trim().ToLower()))
                {
                    var userRecord = db.Users
                        .FirstOrDefault(u => u.UserName.Trim().ToLower() == userInput.UserName.Trim().ToLower()
                                          && u.Password.Trim() == rawPassword.Trim());

                    if (userRecord != null)
                    {
                        isAuthenticated = true;
                        adUser = new ActiveDirectoryHelper.ADUser
                        {
                            UserName = userRecord.UserName,
                            FullName = userRecord.FullName,
                            MailAdress = userRecord.MailAdress,
                            IsAuthenticated = true
                        };
                    }
                    else
                    {
                        //ViewBag.Error = "Invalid username or password.";
                        TempData["LoginError"] = "Invalid username or password.";
                        return View();
                    }
                }
                else
                {
                    // Authenticate via Active Directory
                    adUser = adHelper.AuthenticateUsers(userInput.UserName, rawPassword);
                    isAuthenticated = adUser.IsAuthenticated;
                }

                if (isAuthenticated)
                {
                    // ✅ Always use the sAMAccountName (username) returned from AD
                    string normalizedUsername = adUser.UserName?.Trim().ToLower();
                    // ✅ Look up the user by their AD username, even if they logged in using email
                    var user = db.Users.FirstOrDefault(u => u.UserName.Trim().ToLower() == normalizedUsername);
                    if (user == null)
                    {
                        // User not in database, redirect to UserCreate with pre-filled data
                        var newUser = new User
                        {
                            UserName = adUser.UserName,
                            FullName = adUser.FullName,
                            MailAdress = adUser.MailAdress,
                        };
                        try
                        {
                            newUser.OrganizatinalUnit = adHelper.GetUserOrganizationalUnit(adUser.UserName);
                            newUser.Postion = adHelper.GetUserPosition(adUser.UserName);

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"AD Data Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                            // Continue with null values for optional fields
                            newUser.OrganizatinalUnit = null;
                            newUser.Postion = null;
                        }
                        TempData["NewUser"] = newUser;
                        return RedirectToAction("UserCreate", "User");

                    }
                    else
                    {
                        // User exists, set session and redirect to dashboard
                        FormsAuthentication.SetAuthCookie(user.UserName.ToUpper(), false);
                        Session["UserName"] = user.UserName;
                        Session["FullName"] = user.FullName;
                        Session["UserRole"] = user.Role;
                        Session["Userid"] = user.ID.ToString();
                        Session["UserHomeBranch"] = user.Branch;
                        Session["District"] = user.District;
                        Session["Process"] = user.Process;
                        Session["Position"] = user.Postion;
                        Session["Email"] = user.MailAdress;
                        Session["MemberSince"] = user.CreatedDate;
                        Session["Login"] = "1";
                        System.Web.HttpContext.Current.Cache["UserID"] = user.UserName;
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    //ViewBag.Error = adUser?.ErrorMessage ?? "Invalid username or password.";
                    TempData["LoginError"] = "Invalid username or password.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"An error occurred: {ex.Message}. Contact the administrator.";
                TempData["LoginError"] = $"An error occurred: {ex.Message}. Contact the administrator.";
                return View();
            }
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




        // GET: User/UserCreate
        public ActionResult UserCreate()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UserCreate GET called");
                var processCount = db.Processes.Count();
                System.Diagnostics.Debug.WriteLine($"Processes found: {processCount}");
                ViewBag.Processes = new SelectList(db.Processes
                    .Select(p => new { Value = p.process_name, Text = p.process_name })
                    .ToList(), "Value", "Text");
                ViewBag.Subprocesses = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.Branches = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.Position = new SelectList(db.Positions
                        .Select(p => new { Value = p.Position1, Text = p.Position1 })
                        .ToList(), "Value", "Text");

                // Check for pre-filled data from Login
                var model = TempData["NewUser"] as User ?? new User();
                System.Diagnostics.Debug.WriteLine($"UserCreate model - UserName: {model.UserName}, FullName: {model.FullName}, MailAddress: {model.MailAdress}");

                // Fetch Position and OrganizationalUnit from AD
                if (!string.IsNullOrEmpty(model.UserName))
                {
                    try
                    {
                        var adUsers = adHelper.SearchADUsers(model.UserName);
                        var adUser = adUsers.FirstOrDefault(u => u.Username.ToLower() == model.UserName.ToLower());
                        if (adUser != null)
                        {
                            model.Postion = adUser.Position;
                            model.OrganizatinalUnit = adUser.ADDepartment;
                            System.Diagnostics.Debug.WriteLine($"AD Data - Position: {model.Postion}, OrganizationalUnit: {model.OrganizatinalUnit}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"No AD user found for username: {model.UserName}");
                            model.Postion = "Not provided";
                            model.OrganizatinalUnit = "Not provided";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error fetching AD data: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        model.Postion = "Not available";
                        model.OrganizatinalUnit = "Not available";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No username provided in model for AD search");
                    model.Postion = "Not provided";
                    model.OrganizatinalUnit = "Not provided";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserCreate Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error loading create form: {ex.Message}";
                return View(new User());
            }
        }


        // Helper – fills ViewBag dropdowns (shared by GET & POST)
        private void PopulateDropdowns(User model)
        {
            ViewBag.Processes = new SelectList(
                db.Processes
                  .Select(p => new { Value = p.process_name, Text = p.process_name })
                  .ToList(),
                "Value", "Text", model.Process);

            ViewBag.Subprocesses = new SelectList(
                db.Subprocesses
                  .Where(s => s.Process.process_name == model.Process)
                  .Select(s => new { Value = s.subprocess_name, Text = s.subprocess_name })
                  .ToList(),
                "Value", "Text", model.District);

            ViewBag.Branches = new SelectList(
                db.Branches
                  .Where(b => b.Subprocess.subprocess_name == model.District)
                  .Select(b => new { Value = b.BranchName, Text = b.BranchName })
                  .ToList(),
                "Value", "Text", model.Branch);
            ViewBag.Position = new SelectList(db.Positions
                .Select(p => new { Value = p.Position1, Text = p.Position1 })
                .ToList(), "Value", "Text");
        }

        // POST: User/UserCreate
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserCreate(User model, string submit)
        {
            if (submit == "Save")
            {
                bool isHO = model.OrganizatinalUnit == "HO";
                // Skip Password validation for AD users
                ModelState.Remove("Password");
                if (string.IsNullOrEmpty(model.Postion))
                {
                    TempData["ErrorMessage"] = "Please Choose your Position.";
                    PopulateDropdowns(model);
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.FullName))
                {
                    TempData["ErrorMessage"] = "Your Full Information is Mandatory";
                    PopulateDropdowns(model);
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Process))
                {
                    TempData["ErrorMessage"] = "Please Choose your Process.";
                    PopulateDropdowns(model);
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.PhoneNumber))
                {
                    TempData["ErrorMessage"] = "Please fill your PhoneNumber.";
                    PopulateDropdowns(model);
                    return View(model);
                }

                if (ModelState.IsValid)
                {

                    var exceptPositions = new[]
                                {
                                    "Director",
                                    "Acting Director",
                                    "District Director",
                                    "District Senior Director",
                                    "Senior Director",
                                    "VP",
                                    "Chief",
                                    "CEO",
                                };
                    // Check for UserName uniqueness
                    // Username uniqueness
                    if (db.Users.Any(u => u.UserName == model.UserName))
                    {
                        TempData["ErrorMessage"] = "Username already exists. Please choose a different username.";
                        PopulateDropdowns(model);
                        return View(model);
                    }

                    if (exceptPositions.Contains(model.Postion) && string.IsNullOrEmpty(model.District))
                    {
                        TempData["ErrorMessage"] = "Please Choose your Subprocess or District.";
                        PopulateDropdowns(model);
                        return View(model);
                    }

                    // Non-director positions need full chain
                    if (!exceptPositions.Contains(model.Postion))
                    {
                        if (string.IsNullOrEmpty(model.Process) || string.IsNullOrEmpty(model.District) || string.IsNullOrEmpty(model.Branch))
                        {
                            TempData["ErrorMessage"] = "Please fill your Process or District or Branch";
                            PopulateDropdowns(model);
                            return View(model);
                        }
                    }

                    // Targets
                    if (model.DepositTargetAmount == null || (!isHO && model.MerchantTarget == null))
                    {
                        TempData["ErrorMessage"] = "Please Fill Your Target.";
                        PopulateDropdowns(model);
                        return View(model);
                    }

                    // Branch Manager targets
                    var branchManagerPositions = new[] { "Branch Manager I", "Branch Manager II", "Branch Manager III", "Branch Manager IV" };
                    if (branchManagerPositions.Contains(model.Postion))
                    {
                        if (model.BranchDepositTarget == null || model.BranchFcyTarget == null || model.BranchMerchantTarget == null)
                        {
                            TempData["ErrorMessage"] = "Please Fill the Branch Target.";
                            PopulateDropdowns(model);
                            return View(model);
                        }
                    }

                    try
                    {
                        // Set CreatedDate if not provided
                        model.Role = "Maker";                      
                        model.Password = null;
                        model.CreatedDate = DateTime.Now;
                       
                        db.Users.Add(model);
                        db.SaveChanges();

                        TempData["SuccessMessage"] = "User Profile with Target created successfully!";
                        return RedirectToAction("Login", "User");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UserCreate POST Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        TempData["ErrorMessage"] = $"An error occurred while saving the user: {ex.Message}";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please correct the errors in the form.";
                }
            }

            // Repopulate dropdowns in case of validation failure
            PopulateDropdowns(model);
            return View(model);
        }




        // ---------------------------------------------------
        // GET: User/Edit/{id}
        [HttpGet]
        public ActionResult UpdateProfile(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            PopulateDropdowns(user);
            return View(user);
        }
        // POST: User/Edit
        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(User model)
        {
            bool isHO = model.OrganizatinalUnit == "HO";

            // Protect AD / fixed fields – remove from model validation
            ModelState.Remove("UserName");
            ModelState.Remove("FullName");
            ModelState.Remove("MailAdress");
            ModelState.Remove("Password"); // AD users have no password

            // ──────────────────────────────────────────────
            //  Quick fail-fast validations (early return style)
            // ──────────────────────────────────────────────

            if (string.IsNullOrEmpty(model.Postion))
            {
                TempData["ErrorMessage"] = "Please Choose your Position.";
                PopulateDropdowns(model);
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Process))
            {
                TempData["ErrorMessage"] = "Please Choose your Process.";
                PopulateDropdowns(model);
                return View(model);
            }

            if (string.IsNullOrEmpty(model.PhoneNumber))
            {
                TempData["ErrorMessage"] = "Please fill your PhoneNumber.";
                PopulateDropdowns(model);
                return View(model);
            }

            // ──────────────────────────────────────────────
            // Position-specific structural validations
            // ──────────────────────────────────────────────

            var exceptPositions = new[]
            {
        "Director",
        "Acting Director",
        "District Director",
        "District Senior Director",
        "Senior Director",
        "VP",
        "Chief",
        "CEO"
    };

            var branchManagerPositions = new[]
            {
        "Branch Manager I",
        "Branch Manager II",
        "Branch Manager III",
        "Branch Manager IV"
        // Add "BranchManager" here too if that's still being used
    };

            // Director-level → District required
            if (exceptPositions.Contains(model.Postion) && string.IsNullOrEmpty(model.District))
            {
                TempData["ErrorMessage"] = "Please Choose your Subprocess or District.";
                PopulateDropdowns(model);
                return View(model);
            }

            // Non-director-level positions usually need full chain
            if (!exceptPositions.Contains(model.Postion))
            {
                if (string.IsNullOrEmpty(model.Process) ||
                    string.IsNullOrEmpty(model.District) ||
                    string.IsNullOrEmpty(model.Branch))
                {
                    TempData["ErrorMessage"] = "Please fill your Process or District or Branch";
                    PopulateDropdowns(model);
                    return View(model);
                }
            }

            // ──────────────────────────────────────────────
            // Target validations
            // ──────────────────────────────────────────────

            if (model.DepositTargetAmount == null || (!isHO && model.MerchantTarget == null))
            {
                TempData["ErrorMessage"] = "Please Fill Your Target.";
                PopulateDropdowns(model);
                return View(model);
            }

            // Branch Manager extra targets
            if (branchManagerPositions.Contains(model.Postion))
            {
                if (model.BranchDepositTarget == null ||
                    model.BranchFcyTarget == null ||
                    model.BranchMerchantTarget == null)
                {
                    TempData["ErrorMessage"] = "Please Fill the Branch Target.";
                    PopulateDropdowns(model);
                    return View(model);
                }
            }

            // ──────────────────────────────────────────────
            // Final model state check (data annotations, etc.)
            // ──────────────────────────────────────────────

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                PopulateDropdowns(model);
                return View(model);
            }

            // ─── Success path ───────────────────────────────────────
            try
            {
                var dbUser = db.Users.Find(model.ID);
                if (dbUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    PopulateDropdowns(model);
                    return View(model);
                }

                // Only update fields the user is allowed to change
                dbUser.Process = model.Process;
                dbUser.District = model.District;
                dbUser.Branch = model.Branch;
                dbUser.OrganizatinalUnit = model.OrganizatinalUnit;
                dbUser.Postion = model.Postion;
                dbUser.PhoneNumber = model.PhoneNumber;

                // Targets
                dbUser.DepositTargetAmount = model.DepositTargetAmount;
                dbUser.MerchantTarget = model.MerchantTarget;
                dbUser.FCYTargetAmount = model.FCYTargetAmount;
                dbUser.BranchDepositTarget = model.BranchDepositTarget;
                dbUser.BranchMerchantTarget = model.BranchMerchantTarget;
                dbUser.BranchFcyTarget = model.BranchFcyTarget;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Profile updated successfully! Please Sign In Again.";
                return RedirectToAction("Login", "User");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProfile Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while saving changes: {ex.Message}";
                PopulateDropdowns(model);
                return View(model);
            }
        }
        // GET: User/GetSubprocesses
        [HttpGet]
        public JsonResult GetSubprocesses(string processName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetSubprocesses called with processName: {processName}");
                var subprocesses = db.Subprocesses
                    .Where(s => s.Process.process_name == processName)
                    .Select(s => new { subprocess_name = s.subprocess_name })
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"Subprocesses found: {subprocesses.Count}");
                return Json(subprocesses, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSubprocesses Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Response.StatusCode = 500;
                return Json(new { error = $"Error loading subprocesses: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: User/GetBranches
        [HttpGet]
        public JsonResult GetBranches(string subprocessName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetBranches called with subprocessName: {subprocessName}");
                var branches = db.Subprocesses
                    .Where(b => b.subprocess_name == subprocessName)
                    .SelectMany(b => b.Branches)
                    .Select(b => new { BranchName = b.BranchName })
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"Branches found: {branches.Count}");
                return Json(branches, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBranches Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Response.StatusCode = 500;
                return Json(new { error = $"Error loading branches: {ex.Message}" }, JsonRequestBehavior.AllowGet);
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