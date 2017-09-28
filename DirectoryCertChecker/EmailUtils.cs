using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace DirectoryCertChecker
{
    internal class EmailUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static void SendEmail(string subject, string message)
        {
            var pri = MailPriority.Normal;
            SendEmail(subject, message, pri);
        }

        internal static void SendEmail(string subject, string message, MailPriority pri)
        {
            List<string> recips = Config.GetListAppSetting("MailTo");
            SendEmail(subject, message, recips, pri);
        }

        internal static void SendEmail(string subject, string message, List<string> recips)
        {
            var pri = MailPriority.Normal;
            SendEmail(subject, message, recips, pri);
        }

        internal static void SendEmail(string subject, string message, List<string> recips, MailPriority pri)
        {
            SendEmail(subject, message, recips, pri, null, null);
        }

        internal static void SendEmail(string subject, string message, List<string> recips, MailPriority pri,
            string csvReportFilename, string pdfReportFilename)
        {
            using (var mail = new MailMessage())
            {
                string fromEmailAddress = Config.GetAppSetting("ConfigMailFrom", "dircertchecker@noreply.erehwon.com");
                string fromDisplayName = Config.GetAppSetting("ConfigMailFromDisplayName", "DirCertChecker Notifications");
                mail.From = new MailAddress(fromEmailAddress, fromDisplayName);
                foreach (string recip in recips)
                {
                    mail.To.Add(recip);
                }

                mail.Subject = subject;
                mail.Priority = pri;
                mail.BodyEncoding = Encoding.UTF7;
                mail.IsBodyHtml = false;
                var strEmailBody = new StringBuilder();

                strEmailBody.AppendLine(message);
                //strEmailBody.AppendLine();
                //strEmailBody.AppendLine(DateTime.Now.ToUniversalTime().ToString("r", DateTimeFormatInfo.InvariantInfo));

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
                if (pdfReportFilename != null)
                {
                    Log.Debug("Attaching: " + pdfReportFilename);
                    if (!File.Exists(pdfReportFilename))
                    {
                        Log.Error("Unable to attach report: " + pdfReportFilename);
                    }
                    var data = new Attachment(pdfReportFilename, MediaTypeNames.Application.Pdf);
                    //message.Attachments.Add(new Attachment(@"c:\pdftoattach.pdf"));
                    mail.Attachments.Add(data);
                }

                SmtpClient client = GetSmtpClient();
                //var client = new SmtpClient(Config.GetAppSetting(Constants.ConfigSmtpHost));
                client.Send(mail);
                Log.Debug("Sent email: " + subject);
            }
        }

        private static SmtpClient GetSmtpClient()
        {
            SmtpClient smtpClient;
            string smtpPort = Config.GetAppSetting(Constants.ConfigSmtpPort, "");
            string smtpServer = Config.GetAppSetting(Constants.ConfigSmtpServer);
            if (!string.IsNullOrEmpty(smtpPort))
            {
                try
                {
                    int port = int.Parse(smtpPort);
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

            if (Config.GetBoolAppSetting(Constants.ConfigSmtpUseSsl))
            {
                smtpClient.EnableSsl = true;
            }

            if (Config.GetBoolAppSetting(Constants.ConfigSmtpRequiresAuthentication))
            {
                string smtpUser = Config.GetAppSetting(Constants.ConfigSmtpUser);
                string smtpPassword = Config.GetAppSetting(Constants.ConfigSmtpPassword);
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            }

            return smtpClient;
        }

        internal static void SendEmailAlert(Server s, string subject)
        {
            List<string> recips = Config.GetAlertEmailRecips();

            string emailSubject = subject + " " + s.Hostname + ":" + s.Port;
            string emailBody = MessageFormatter.FormatLineForEmail(s);
            var pri = MailPriority.High;
            SendEmail(emailSubject, emailBody, recips, pri);
        }

        internal static void SendConnectionErrorEmailAlert(Server s)
        {
            Log.Debug("SendConnectionErrorEmailAlert Exit");
            string emailSubject = Constants.MsgCertCheckerError + " " + s.Hostname;
            string emailBody = Environment.NewLine + Environment.NewLine;
            emailBody += s.ShortErrorMsg;
            SendEmail(emailSubject, emailBody);
            Log.Debug("Back from EmailUtils.SendEmail");
            Log.Debug("SendConnectionErrorEmailAlert Exit");
        }

        internal static void EmailReport(string emailText, string csvReportFilename, string pdfReportFilename)
        {
            List<string> recips = Config.GetListAppSetting(Constants.ConfigMailTo);
            string emailSubject = "Red Kestrel CertAlert: " + Constants.MsgCertCheckerReport;
            var pri = MailPriority.High;
            SendEmail(emailSubject, emailText, recips, pri, csvReportFilename, pdfReportFilename);
        }
    }
}
