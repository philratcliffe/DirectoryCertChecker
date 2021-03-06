#region Copyright and license information

// Copyright � 2017 Phil Ratcliffe
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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CsvHelper;

namespace DirectoryCertChecker
{
    /// <summary>
    ///     Provides a set of methods for deleting and creating reports.
    /// </summary>
    internal class ReportWriter
    {
        private readonly int _warningPeriodInDays;

        internal ReportWriter(int warningPeriodInDays)
        {
            _warningPeriodInDays = warningPeriodInDays;
            ReportFilename = @"certificates.csv";
        }

        public int CertsWritten { get; private set; }
        public int ExpiredCerts { get; private set; }
        public int ExpiringCerts { get; private set; }
        public string ReportFilename { get; private set; }
        

        internal void RemoveReportFile()
        {
            // Remove any previous results
            File.Delete(ReportFilename);
        }

        /// <summary>
        ///     Writes the header line to the CSV report using the CsvHelper
        ///     library.
        /// </summary>
        internal void WriteHeader()
        {
            using (TextWriter writer = new StreamWriter(ReportFilename, true))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.WriteHeader<ReportRecord>();
            }
        }

        /// <summary>
        ///     Writes a record to the CSV report using the private WriteRecord method.
        /// </summary>
        /// ///
        /// <param name="entryDn">
        ///     The distinguished name of the record to write.
        /// </param>
        /// ///
        /// <param name="cert">
        ///     A certificate represented in an X509Certificate2 object.
        /// </param>
        internal void WriteRecord(string entryDn, X509Certificate2 cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException(nameof(cert));
            }
            var record = new ReportRecord
            {
                EntryDn = entryDn
            };
            var daysToExpiry = cert.NotAfter.ToUniversalTime().Subtract(DateTime.UtcNow).Days;
            record.CertificateDn = cert.Subject;
            record.SerialNumber = cert.SerialNumber;
            record.ExpiryDate = cert.NotAfter.ToShortDateString();
            record.Days = daysToExpiry.ToString();
            if (cert.NotAfter.ToUniversalTime() < DateTime.UtcNow)
            {
                record.ExpiryStatus = "EXPIRED";
                ExpiredCerts += 1;
            }
            else if (daysToExpiry <= _warningPeriodInDays)
            {
                record.ExpiryStatus = "EXPIRING";
                ExpiringCerts += 1;
            }
            else
            {
                record.ExpiryStatus = "OK";
            }
            WriteRecord(record);
            CertsWritten += 1;
        }

        /// <summary>
        ///     Writes a line to the CSV report using the CsvHelper
        ///     library.
        /// </summary>
        /// <param name="record">
        ///     The record to write.
        /// </param>
        private void WriteRecord(ReportRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            using (TextWriter writer = new StreamWriter(ReportFilename, true))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.WriteRecord(record);
            }
        }
    }
}