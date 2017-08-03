// Copyright © 2017 Phil Ratcliffe
// 
// This file is part of ExpiringCerts program.
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
using System.Security.Cryptography.X509Certificates;

namespace DirectoryCertChecker
{
    internal class DirectoryCertChecker
    {
        private static void Main(string[] args)
        {
            // var server = "192.168.1.213";
            var server = ConfigurationManager.AppSettings["server"];
            var baseDN = ConfigurationManager.AppSettings["baseDN"];

            // TODO
            // Add logging
            // Handle no server available exception
            // Multiple search roots
            // Page searches so can handle many results e.g., 10k plus

            
            var cp = new CertProcessor(server, baseDN);
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
            var searchRoot = new DirectoryEntry("LDAP://" + server + "/" + baseDN);
            searchRoot.AuthenticationType = AuthenticationTypes.None;

            var findCerts = new DirectorySearcher(searchRoot);
            findCerts.SearchScope = SearchScope.Subtree;
            findCerts.Filter = "(userCertificate=*)";
            findCerts.PropertiesToLoad.Add("userCertificate");

            foreach (SearchResult result in findCerts.FindAll())
                ProcessSearchResult(result);

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

            // Intialise expiry date
            var latestExpiryDate = new DateTime(1970, 1, 1);

            var certificatesAsBytes = result.Properties["UserCertificate"];
            foreach (byte[] certificateBytes in certificatesAsBytes)
            {
                var certificate = new X509Certificate2(certificateBytes);
                Console.WriteLine("DEBUG ----> " + certificate.NotAfter);
                var difference = certificate.NotAfter - latestExpiryDate;
                if (difference.TotalMilliseconds > 0)
                    latestExpiryDate = certificate.NotAfter;
                certCount += 1;
            }
            Console.WriteLine("CERTIFICATE EXPIRY: " + latestExpiryDate);
        }
    }
}