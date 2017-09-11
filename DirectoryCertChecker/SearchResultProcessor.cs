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
using System.DirectoryServices;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace DirectoryCertChecker
{
    internal class SearchResultProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        /// <summary>
        ///     Takes the search result for a specific Active Directory entry and returns the userCertificate 
        ///     attribute value of the entry. If an entry has multiple certificate entries (presumably this is 
        ///     because old certs are sometimes left in the directory entry), it returns the one with the most
        ///     recent expiry date.
        /// </summary>
        /// <param name="result">
        ///     The result param encapsulates a node in the Active Directory Domain Services hierarchy
        ///     that is returned during a search using .NET's DirectorySearcher.
        /// </param>
        internal X509Certificate2 GetCertificate(SearchResult result)
        {
            var numberOfCerts = result.Properties["UserCertificate"].Count;
            if (numberOfCerts == 0)
            {
                throw new Exception("There were no certificate found in the result.");
            }

            var entryDn = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());
            Log.Debug($"{entryDn} has: {numberOfCerts} certs.");
            Console.Write($"{entryDn}");
            
            var certificatesAsBytes = result.Properties["UserCertificate"];
            
            X509Certificate2 latestCertificate = GetLatestCertificate(certificatesAsBytes);

            if (Epoch >= latestCertificate.NotAfter)
            {
                Console.WriteLine($"Error: Certificate expiry date is invalid: {latestCertificate.NotAfter}");
                Log.Error($"Epiry date {latestCertificate.NotAfter} is invalid for {result.Path}.");
            }
            else
            {
                Console.WriteLine($" (Expires: {latestCertificate.NotAfter.ToShortDateString()})");
            }
            if (latestCertificate.RawData.Length == 0)
            {
                throw new Exception("There was no certificate found in the result.");
            }
            return latestCertificate;
        }

        private static X509Certificate2 GetLatestCertificate(ResultPropertyValueCollection certificatesAsBytes)
        {
            if (certificatesAsBytes.Count == 0)
            {
                throw new ArgumentException("There were no certificates in the collection passed");
            }
            DateTime latestExpiryDate = Epoch;
            int certCount = 0;
            X509Certificate2 latestCertificate = new X509Certificate2();
            foreach (byte[] certificateBytes in certificatesAsBytes)
                try
                {
                    var certificate = new X509Certificate2(certificateBytes);
                    TimeSpan timeDifference = certificate.NotAfter - latestExpiryDate;
                    if (timeDifference.TotalMilliseconds > 0)
                        latestExpiryDate = certificate.NotAfter;
                    latestCertificate = certificate;
                    certCount += 1;
                    Log.Debug($"Expiry: {certificate.NotAfter} ({certCount} certs processed.)");
                }
                catch (CryptographicException ce)
                {
                    Console.WriteLine("Error: There was a problem reading a certificate.");
                    Log.Error("There was a problem with reading a certificate.", ce);
                    throw;
                }
                return latestCertificate;

        }
    }
}