using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using log4net;

namespace DirectoryCertChecker
{

    
    internal class EmailUtils
    {
        /// <summary>
        ///     Provides a set of methods for sending emails.
        /// </summary>
        
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static void SendEmail(string subject, string message)
        {
            var pri = MailPriority.Normal;
            SendEmail(subject, message, pri);
        }

        internal static void SendEmail(string subject, string message, MailPriority pri)
        {
            var recips = Config.GetListAppSetting("MailTo");
            SendEmail(subject, message, recips, pri);
        }

        internal static void SendEmail(string subject, string message, List<string> recips)
        {
            var pri = MailPriority.Normal;
            SendEmail(subject, message, recips, pri);
        }

        internal static void SendEmail(string subject, string message, List<string> recips, MailPriority pri)
        {
            SendEmail(subject, message, recips, pri, null);
        }

        internal static void SendEmail(string subject, string message, List<string> recips, MailPriority pri,
            string csvReportFilename)
        {
            using (var mail = new MailMessage())
            {
                var fromEmailAddress = Config.GetAppSetting("MailFrom", "noreply@directorycertchecker.info");
                var fromDisplayName = Config.GetAppSetting("MailFromDisplayName", "Directory Cert Checker");
                mail.From = new MailAddress(fromEmailAddress, fromDisplayName);
                foreach (var recip in recips)
                {
                    mail.To.Add(recip);
                }

                mail.Subject = subject;
                mail.Priority = pri;
                mail.BodyEncoding = Encoding.UTF7;
                mail.IsBodyHtml = false;
                var strEmailBody = new StringBuilder();

                strEmailBody.AppendLine(message);

                mail.Body = strEmailBody.ToString();
                if (csvReportFilename != null)
                {
                    Log.Debug("Attaching: " + csvReportFilename);
                    if (!File.Exists(csvReportFilename))
                    {
                        Log.Error("Unable to attach report: " + csvReportFilename);
                    }
                    var data = new Attachment(csvReportFilename, MediaTypeNames.Application.Octet);
                    mail.Attachments.Add(data);
                }

                var client = GetSmtpClient();

                client.Send(mail);
                Log.Debug("Sent email: " + subject);
            }
        }

        internal static void EmailReport(string emailText, string csvReportFilename)
        {
            var recips = Config.GetListAppSetting("MailTo");
            var emailSubject = "Directory Cert Checker Report";
            var pri = MailPriority.High;
            SendEmail(emailSubject, emailText, recips, pri, csvReportFilename);
        }

        private static SmtpClient GetSmtpClient()
        {
            SmtpClient smtpClient;
            var smtpPort = Config.GetAppSetting("SmtpPort", "25");
            var smtpServer = Config.GetAppSetting("SmtpServer");
            if (!string.IsNullOrEmpty(smtpPort))
            {
                try
                {
                    var port = int.Parse(smtpPort);
                    smtpClient = new SmtpClient(smtpServer, port);
                }
                catch
                {
                    smtpClient = new SmtpClient(smtpServer);
                }
            }
            else
            {
                smtpClient = new SmtpClient(smtpServer);
            }

            if (Config.GetBoolAppSetting("SmtpUseSsl"))
            {
                smtpClient.EnableSsl = true;
            }

            if (Config.GetBoolAppSetting("SmtpRequiresAuthentication"))
            {
                var smtpUser = Config.GetAppSetting("SmtpUser");
                var smtpPassword = Config.GetAppSetting("SmtpPassword");
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            }

            return smtpClient;
        }
    }
}