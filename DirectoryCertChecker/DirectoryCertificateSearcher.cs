﻿#region Copyright and license information
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

using System.Collections.Generic;
using System.DirectoryServices;

namespace DirectoryCertChecker
{
    internal class DirectoryCertificateSearcher
    {

        /// <summary>
        ///     Searches Microsoft Active Directory for entries with a userCertificate attribute.
        /// </summary>
        /// <param name="server">
        ///     The server to search.
        /// </param>
        /// ///
        /// <param name="searchBase">
        ///     The DN of the top of the tree we want the search to begin. An empty string equals root.
        /// </param>
        public IEnumerable<SearchResult> Search(string server, string searchBase)
        {
            
            using (var searchBaseEntry = new DirectoryEntry("LDAP://" + server + "/" + searchBase))
            {
                searchBaseEntry.AuthenticationType =
                    AuthenticationTypes.None; // Use this for anonymous simple authentication and simple authentication.
                searchBaseEntry.Username = Config.GetAppSetting("ldapUsername", null);
                searchBaseEntry.Password = Config.GetAppSetting("ldapPassword", null);
                using (var findCerts = new DirectorySearcher(searchBaseEntry))
                {
                    findCerts.SearchScope = SearchScope.Subtree;

                    // Specify that only entries with a userCertificate attributee
                    // should be returned.
                    findCerts.Filter = "(userCertificate=*)";

                    // Specify the attribute to be returned. We are only interested in
                    // the certificate.
                    findCerts.PropertiesToLoad.Add("userCertificate");

                    //
                    // If there is a possibility that Active Directory has more than 1000 entries with certificates, 
                    // you must set PageSize to a non zero value, preferably 1000, otherwise DirectorySearcher.FindAll() only 
                    // returns the first 1000 records and other entries will be missed without any warning.
                    //
                    const int necessaryPageSize = 1000;
                    findCerts.PageSize = necessaryPageSize;

                    using (var results = findCerts.FindAll())
                    {
                        foreach (SearchResult result in results)
                            yield return result;
                    }
                }
            }
        }
    }
}