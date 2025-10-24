using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices;
using System.Net.Mail;

namespace TRMS.ADautho
{
    public class ActiveDirectoryHelperEmail
    {

        private string Usernameeldap = "backofficeportal";
        private string Passwordldap = "xHzD>O)46=[0";
        private string eldapurl = "LDAP://10.1.72.10";

        public String AuthenticateUserGetemail(string username)
        {
            String email = "";

            try
            {
                using (var entry = new DirectoryEntry(eldapurl))
                {
                    entry.Username = Usernameeldap;
                    entry.Password = Passwordldap;

                    // entry.Password = password;

                    var searcher = new DirectorySearcher(entry)
                    {
                        Filter = $"(&(objectCategory=user)(sAMAccountName={username}))"
                    };

                    var result = searcher.FindOne();
                    if(result!=null)
                    {

                        if (result != null)
                        {
                            DirectoryEntry userEntry = result.GetDirectoryEntry();
                            email = userEntry.Properties["mail"].Value.ToString();
                        }
                    }

                    return email;
                }
            }
            catch (Exception ex)
            {
                email = "An error occurred during authentication: " + ex.Message;
                return email;
            }
        }

        public bool emailRegisterd(string toemailAddress)
        {

            if (String.IsNullOrEmpty(toemailAddress))
            {
                return false;
            }

            //string toemail = "Mekuanent.Lamsgne@coopbankoromiasc.com";
            //string fromemail = "Tesfahun.Semaw@coopbankoromiasc.com";
            string toemail = toemailAddress;
            string fromemail = "Andualem.Tesfaye@coopbankoromiasc.com";
            string Usernameeldap = "backofficeportal";
            string Passwordldap = "xHzD>O)46=[0";
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromemail.Trim());
                    mail.To.Add(new MailAddress(toemail.Trim()));

                    mail.Subject = "Registration Successful";
                    
                    string name = "";
                    int indexOfSpace = toemail.IndexOf(".") > 0 ? toemail.IndexOf(".") : toemail.Length;
                    name = toemail.Substring(0, indexOfSpace);
                    mail.Body = "Dear " + name + ",\n\nYour registration for online career opportunity examination has been successfully completed and you can access examination center accordingly" + ".\n\nNotice: Use your Active Directory Credentials as user name and also your outlook password as for password field" + ".\n\nRegards!" + "\nAndualem Tesfaye" + "\n\nHuman Capital Business Partner-Head Office";


                    // mail.Attachments.Add(new Attachment(filePath));
                    mail.IsBodyHtml = false;
                    SmtpClient smtp = new SmtpClient();
                    smtp.UseDefaultCredentials = true;
                    //smtp.Credentials = new System.Net.NetworkCredential("Mekuanent.Lamsgne@coopbankoromiasc.com", "Mekelame*32322");
                    smtp.Credentials = new System.Net.NetworkCredential(Usernameeldap, Passwordldap);
                    smtp.Port = 25;
                    smtp.Host = "10.1.150.150";
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = false;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {

                return false;
            }
            return true;
        }

    }


}