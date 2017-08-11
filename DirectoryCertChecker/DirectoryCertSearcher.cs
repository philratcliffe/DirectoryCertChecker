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

using System.Collections.Generic;
using System.DirectoryServices;

namespace DirectoryCertChecker
{
    internal class DirectoryCertSearcher
    {
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
                    findCerts.Filter = "(userCertificate=*)";
                    findCerts.PropertiesToLoad.Add("userCertificate");

                    //
                    // If there is a possibility that Active Directory has more than 1000 entries with certificates, 
                    // you must set PageSize to a non zero value, preferably 1000, otherwise DirectorySearcher.FindAll() only 
                    // returns the first 1000 records and other entries will be missed without any warning.
                    //
#if DEBUG
                    findCerts.PageSize = 0;
#else
                    findCerts.PageSize = 1000;
#endif

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