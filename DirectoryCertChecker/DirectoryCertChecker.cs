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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CsvHelper;
using log4net;

namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static int _certCount;

        private static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    Log.Warn("Unexpected argument passed.");
                    Console.WriteLine("This program does not expect any arguments");
                }

                Log.Info("DirectoryCertChecker has started.");

                var server = Config.GetAppSetting("server");
                var baseDn = Config.GetAppSetting("searchBaseDn");

                // Remove any previous results
                File.Delete(@"certificates.csv");
                using (TextWriter writer = new StreamWriter(@"certificates.csv", true))
                {
                    
                    var csv = new CsvWriter(writer);
                    csv.Configuration.Encoding = Encoding.UTF8;
                    csv.WriteHeader<DataRecord>();
                }
                

                try
                {
                    var directoryCertSearcher = new DirectoryCertSearcher();
                    var searchResultProcessor = new SearchResultProcessor();

                    foreach (var result in directoryCertSearcher.Search(server, baseDn))
                        _certCount += searchResultProcessor.ProcessSearchResult(result);
                }
                catch (COMException ce)
                {
                    var msg = $"There was a problem trying to connect to your LDAP server at {server}.";
                    Console.WriteLine($"{msg} See the DirectoryCertChecker.log file for more details.");
                    Log.Error(msg, ce);
                }
                Console.WriteLine("Found " + _certCount + " certs");
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

    internal class DirectoryCertSearcher
    {
        public IEnumerable<SearchResult> Search(string server, string searchBase)
        {
            using (var searchBaseEntry = new DirectoryEntry("LDAP://" + server + "/" + searchBase))
            {
                searchBaseEntry.AuthenticationType =
                    AuthenticationTypes
                        .None; // Use this setting for anonymous simple authentication and simple authentication
                searchBaseEntry.Username = Config.GetAppSetting("ldapUsername", null);
                searchBaseEntry.Password = Config.GetAppSetting("ldapPassword", null);
                using (var findCerts = new DirectorySearcher(searchBaseEntry))
                {
                    findCerts.SearchScope = SearchScope.Subtree;
                    findCerts.Filter = "(userCertificate=*)";
                    findCerts.PropertiesToLoad.Add("userCertificate");

                    //
                    // If there is a possibility that Active Directory has more than 1000 entries with certificates, 
                    // you must set PageSize to a non zero value, preferably 1000, otherwise DirectorySearcher.FindAll() only 
                    // returns the first 1000 records and other entries will be missed without any warning.
                    //

                    
                    findCerts.PageSize = 1000;
                    
                    using (var results = findCerts.FindAll())
                    {
                        foreach (SearchResult result in results)
                            yield return result;
                    }
                }
            }
        } 
    }

    internal class SearchResultProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        public int ProcessSearchResult(SearchResult result)
        {
            var entryDn = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());
            Log.Debug($"{entryDn} has: {result.Properties["UserCertificate"].Count} certs.");
            Console.Write($"{entryDn}");
            var certCount = 0;
            X509Certificate2 latestCertificate = null;


            // Intialise expiry date
            var latestExpiryDate = Epoch;
            

            var certificatesAsBytes = result.Properties["UserCertificate"];
            foreach (byte[] certificateBytes in certificatesAsBytes)
                try
                {
                    var certificate = new X509Certificate2(certificateBytes);
                    Log.Debug(certificate.NotAfter);
                    var difference = certificate.NotAfter - latestExpiryDate;
                    if (difference.TotalMilliseconds > 0)
                        latestExpiryDate = certificate.NotAfter;
                        latestCertificate = certificate;
                    certCount += 1;
                }
                catch (CryptographicException ce)
                {
                    Console.WriteLine("There was a problem with a certificate attribute.");
                    Log.Error("There was a problem with a certificate attribute.", ce);
                }
            if (Epoch >= latestExpiryDate)
            {
                Console.WriteLine($"ERROR: Certificate Expiry is invalid: {latestExpiryDate}");
                Log.Error($"The latest expiry date is invalid for {result.Path}. ");
            }
            else
            {
                Console.WriteLine($" (Expires: {latestExpiryDate.ToShortDateString()})");

                var record = new DataRecord();
                record.EntryDn = entryDn;
                if (latestCertificate != null)
                {
                    record.CertificateDn = latestCertificate.Subject;
                    record.SerialNumber = latestCertificate.SerialNumber;
                }
                record.ExpiryDate = latestExpiryDate.ToShortDateString();
                using (TextWriter writer = new StreamWriter(@"certificates.csv", true))
                {
                    var csv = new CsvWriter(writer);
                    csv.Configuration.Encoding = Encoding.UTF8;
                    csv.WriteRecord(record); 
                }
            }
            return certCount;
            
        }
    }

    internal class DataRecord
    {
        //Should have properties which correspond to the Column Names in the file
        public String EntryDn { get; set; }
        public String CertificateDn { get; set; }
        public String SerialNumber { get; set; }
        public String ExpiryDate { get; set; }
    }
}