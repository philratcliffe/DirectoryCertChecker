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
using System.DirectoryServices;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Log.Warn("Unexpected argument passed.");
                Console.WriteLine("This program does not expect any arguments");
            }

            var server = Config.GetAppSetting("server");
            var baseDn = Config.GetAppSetting("baseDn");


            Log.Info("DirectoryCertChecker has started.");

            try
            {
                var cp = new CertProcessor(server, baseDn);
                cp.ProcessCerts();
            }
            catch (COMException ce)
            {
                var msg = $"There was a problem trying to connect to your LDAP server at {server}.";
                Console.WriteLine($"{msg} See the DirectoryCertChecker.log file for more details.");
                Log.Error(msg, ce);
            }
            catch (Exception ex)
            {
                Log.Info("There was an error. Check the DirectoryCertChecker.log file for more details.");
                Log.Error("Top level exception caught. ", ex);
            }
        }
    }

    internal class CertProcessor
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _baseDn;
        private readonly string _server;

        private int _certCount;

        /// <summary>
        ///     Class constructor.
        /// </summary>
        /// <param name="server">
        ///     The server to search.
        /// </param>
        /// ///
        /// <param name="baseDn">
        ///     The DN to start the search at.
        /// </param>
        public CertProcessor(string server, string baseDn)
        {
            _server = server;
            _baseDn = baseDn;
            _certCount = 0;
        }

        /// <summary>
        ///     Searches Microsoft Active Directory for certificates in the userCertificate attribute and processes them.
        /// </summary>
        
        internal void ProcessCerts()
        {
            using (var searchRoot = new DirectoryEntry("LDAP://" + _server + "/" + _baseDn))
            {
                searchRoot.AuthenticationType =
                    AuthenticationTypes.None; // Use for Anonymous and Username+Password bind
                searchRoot.Username = Config.GetAppSetting("ldapUsername", null);
                searchRoot.Password = Config.GetAppSetting("ldapPassword", null);

                using (var findCerts = new DirectorySearcher(searchRoot))
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
                        Log.Debug($"Directory search returned {results.Count} directory entries.");
                        foreach (SearchResult result in results)
                            ProcessSearchResult(result);
                    }
                }
            }

            Console.WriteLine("Found " + _certCount + " certs");
        }

        /// <summary>
        ///     Takes an Active Directory search result and writes out the certificate expiry information.
        ///     If an entry has multiple certificate entries (I assume this is because old certs are sometimes
        ///     left in the directory entry), it writes out only the most recent expiry date.
        /// </summary>
        /// <param name="result">
        ///     The result param encapsulates a node in the Active Directory Domain Services hierarchy
        ///     that is returned during a search through DirectorySearcher.
        /// </param>
        private void ProcessSearchResult(SearchResult result)
        {
            var distinguisjedName = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());
            Log.Debug($"{distinguisjedName} has: {result.Properties["UserCertificate"].Count} certs.");
            Console.Write($"{distinguisjedName}");


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
                    _certCount += 1;
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
            }
        }
    }
}