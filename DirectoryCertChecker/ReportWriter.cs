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

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CsvHelper;

namespace DirectoryCertChecker
{
    internal class ReportWriter
    {
        private static readonly string reportFilename = @"certificates.csv";

        internal void RemoveReportFile()
        {
            // Remove any previous results
            File.Delete(reportFilename);
        }

        internal void WriteHeader()
        {
            using (TextWriter writer = new StreamWriter(reportFilename, true))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.WriteHeader<ReportRecord>();
            }
        }

        internal void WriteRecord(ReportRecord record)
        {
            using (TextWriter writer = new StreamWriter(reportFilename, true))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.WriteRecord(record);
            }
        }


        internal void WriteRecord(string entryDn, X509Certificate2 cert)
        {
            var record = new ReportRecord
            {
                EntryDn = entryDn
            };
            if (cert != null)
            {
                record.CertificateDn = cert.Subject;
                record.SerialNumber = cert.SerialNumber;
                record.ExpiryDate = cert.NotAfter.ToShortDateString();
            }

            WriteRecord(record);
        }
    }
}