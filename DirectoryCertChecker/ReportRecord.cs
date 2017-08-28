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

namespace DirectoryCertChecker
{
    internal class ReportRecord
    {
        //Should have properties which correspond to the Column Names in the file
        public string EntryDn { get; set; }

        public string CertificateDn { get; set; }
        public string SerialNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string ExpiryStatus { get; set; }
        public string Days { get; set; }
    }
}