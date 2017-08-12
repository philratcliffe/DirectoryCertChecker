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
using log4net;


namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    Log.Warn("Unexpected argument passed.");
                    Console.WriteLine("This program does not expect any arguments.");
                }

                Log.Info("DirectoryCertChecker has started.");

                var server = Config.GetAppSetting("server");
                var baseDn = Config.GetAppSetting("searchBaseDn");
                var reportWriter = new ReportWriter();
                var certCount = 0;
                     

                reportWriter.RemoveReportFile();
                reportWriter.WriteHeader();

                try
                {
                    var directoryCertSearcher = new DirectoryCertSearcher();
                    var searchResultProcessor = new SearchResultProcessor();

                    foreach (var result in directoryCertSearcher.Search(server, baseDn))
                    {
                        var cert = searchResultProcessor.GetCertificate(result);
                        var entryDn = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());

                        if (cert != null)
                        {
                            reportWriter.WriteRecord(entryDn, cert);
                            certCount += 1;
                        }
                        else
                        {
                            Log.Error($"There was a problem getting a certificate for {entryDn}");
                        }
                        
                    }
                    Console.WriteLine($"There were {certCount} certs written to the report.");
                }
                catch (COMException ce)
                {
                    var msg = $"There was a problem trying to connect to your LDAP server at {server}.";
                    Console.WriteLine($"{msg} See the DirectoryCertChecker.log file for more details.");
                    Log.Error(msg, ce);
                }
                
            }
            catch (ConfigurationErrorsException cee)
            {
                Console.WriteLine($"Error reading the config file - {cee.Message}");
                Log.Error("Error reading the config file.", cee);
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error. Check the DirectoryCertChecker.log file for more details.");
                Log.Error("Top level exception caught. ", ex);
            }
        }
    }
}