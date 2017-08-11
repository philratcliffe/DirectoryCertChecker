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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace DirectoryCertChecker
{
    internal class SearchResultProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public X509Certificate2 GetCertificate(SearchResult result)
        {
            var numberOfCerts = result.Properties["UserCertificate"].Count;
            var entryDn = Uri.UnescapeDataString(new Uri(result.Path).Segments.Last());
            var certCount = 0;
            X509Certificate2 latestCertificate = null;

            Log.Debug($"{entryDn} has: {numberOfCerts} certs.");

            Console.Write($"{entryDn}");

            // Intialise expiry date
            var latestExpiryDate = Epoch;

            var certificatesAsBytes = result.Properties["UserCertificate"];
            foreach (byte[] certificateBytes in certificatesAsBytes)
                try
                {
                    var certificate = new X509Certificate2(certificateBytes);
                    var timeDifference = certificate.NotAfter - latestExpiryDate;
                    if (timeDifference.TotalMilliseconds > 0)
                        latestExpiryDate = certificate.NotAfter;
                    latestCertificate = certificate;
                    certCount += 1;
                    Log.Debug($"Expiry: {certificate.NotAfter} ({certCount} of {numberOfCerts} certs processed.)");
                }
                catch (CryptographicException ce)
                {
                    Console.WriteLine("Error: There was a problem reading a certificate.");
                    Log.Error("There was a problem with reading a certificate.", ce);
                }
            if (Epoch >= latestExpiryDate)
            {
                Console.WriteLine($"Error: Certificate expiry date is invalid: {latestExpiryDate}");
                Log.Error($"Epiry date {latestExpiryDate} is invalid for {result.Path}.");
            }
            else
            {
                Console.WriteLine($" (Expires: {latestExpiryDate.ToShortDateString()})");
            }
            return latestCertificate;
        }
    }
}