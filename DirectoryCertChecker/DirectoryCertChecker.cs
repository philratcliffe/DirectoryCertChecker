#region Copyright and license information

// Copyright © 2017 Phil Ratcliffe
// 
// This file is part of DirectoryCertChecker program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using log4net;


namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main()
        {
            const int defaultWarningPeriodInDays = 90;

            try
            {
                Log.Info("DirectoryCertChecker has started.");
                
                var baseDNs = Config.GetListAppSetting("searchBaseDNs");
                var warningPeriodInDays = Config.GetIntAppSetting("warningPeriodInDays", defaultWarningPeriodInDays);

                var reportWriter = new ReportWriter(warningPeriodInDays);
                reportWriter.RemoveReportFile();
                reportWriter.WriteHeader();

                var server = Config.GetAppSetting("server");
                foreach (var baseDn in baseDNs)
                {
                    try
                    {
                        
                        ProcessSearchBaseDn(server, baseDn, reportWriter);
                    }
                    catch (COMException ce)
                    {
                        var msg = $"There was a problem trying to connect to your LDAP server at {server}.";
                        Console.WriteLine($"{msg} See the DirectoryCertChecker.log file for more details.");
                        Log.Error(msg, ce);
                    }
                }

                WriteSummaryToConsole(reportWriter);
                EmailUtils.EmailReport("Directory Cert Checker Report", reportWriter.ReportFilename);


            }
            catch (ConfigurationErrorsException cee)
            {
                Console.WriteLine($"Error reading the config file - {cee.Message}");
                Log.Error("Error reading the config file.", cee);
            }
            // Last chance exception handler
            catch (Exception ex)
            {
                Console.WriteLine("There was an error. Check the DirectoryCertChecker.log file for more details.");
                Log.Error("Top level exception caught. ", ex);
            }
        }

        private static void WriteSummaryToConsole(ReportWriter reportWriter)
        {
            Console.WriteLine($"{reportWriter.CertsWritten} certs written to the report.");
            Console.WriteLine($"{reportWriter.ExpiredCerts} EXPIRED certs.");
            Console.WriteLine($"{reportWriter.ExpiringCerts} EXPIRING certs.");
        }

        private static void ProcessSearchBaseDn(string server, string baseDn, ReportWriter reportWriter)
        {
            var directoryCertSearcher = new DirectoryCertificateSearcher();
            var searchResultProcessor = new SearchResultProcessor();

            foreach (var result in directoryCertSearcher.Search(server, baseDn))
            {
                var entryDn = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());
                try
                {
                    var cert = searchResultProcessor.GetCertificate(result);
                    reportWriter.WriteRecord(entryDn, cert);
                }
                catch (CryptographicException)
                {
                    Log.Error($"There was a problem getting a certificate for {entryDn}");
                }
            }
        }
    }
}