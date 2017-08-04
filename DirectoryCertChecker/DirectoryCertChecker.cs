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
using System.Configuration;
using System.DirectoryServices;
using System.Net.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static void Main(string[] args)
        {
            var server = ConfigurationManager.AppSettings["server"];
            var baseDN = ConfigurationManager.AppSettings["baseDN"];
            
            try
            {
                var cp = new CertProcessor(server, baseDN);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DEBUG " + ex);
            }
        }
    }

    internal class CertProcessor
    {
        private int certCount;

        /// <summary>
        ///     Searches Microsoft Active Directory for certificates in the userCertificate attribute.
        /// </summary>
        /// <param name="server">
        ///     The server to search.
        /// </param>
        /// ///
        /// <param name="baseDN">
        ///     The DN to start the search at.
        /// </param>
        public CertProcessor(string server, string baseDN)


        {
            using (var searchRoot = new DirectoryEntry("LDAP://" + server + "/" + baseDN))
            {
                searchRoot.AuthenticationType = AuthenticationTypes.None;

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
                        var msg = $"DEBUG: Directory search returned {results.Count} directory entries.";
                        Console.WriteLine(msg);
                        foreach (SearchResult result in results)
                            ProcessSearchResult(result);
                    }
                }
            }

            Console.WriteLine("Founds " + certCount + " certs");
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
            Console.WriteLine(result.Path);
            Console.WriteLine("DEBUG: Number of certs in this entry: " + result.Properties["UserCertificate"].Count);

            // Intialise expiry date
            var latestExpiryDate = new DateTime(1970, 1, 1);

            var certificatesAsBytes = result.Properties["UserCertificate"];
            foreach (byte[] certificateBytes in certificatesAsBytes)
                try
                {
                    var certificate = new X509Certificate2(certificateBytes);
                    Console.WriteLine("DEBUG ----> " + certificate.NotAfter);
                    var difference = certificate.NotAfter - latestExpiryDate;
                    if (difference.TotalMilliseconds > 0)
                        latestExpiryDate = certificate.NotAfter;
                    certCount += 1;
                }
                catch (CryptographicException ce)
                {
                    Console.WriteLine("There was a problem with this certificate attribure: " + ce);
                }
            // TODO if latestExpiryDate 1970, message should state problem with certificate
            Console.WriteLine("CERTIFICATE EXPIRY: " + latestExpiryDate);
        }
    }
}