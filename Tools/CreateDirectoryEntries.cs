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
using System.IO;

namespace CreateTestDirectoryEntries
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DirectoryEntry rootEntry =
                new DirectoryEntry(
                    "LDAP://192.168.1.213/OU = Test, OU = Research, O = Acme Test");
            rootEntry.Username = "CN=admin1, OU = Research, O = Acme Test";
            rootEntry.Password = "admin1 password here";
            rootEntry.AuthenticationType = AuthenticationTypes.None;

            for (var i = 0; i < 10000; i++)
                try
                {
                    var data = File.ReadAllBytes("test-cert.cer");

                    DirectoryEntry userEntry = rootEntry.Children.Add("CN=Test--" + i, "user");
                    userEntry.Properties["userCertificate"].Insert(0, data);
                    userEntry.CommitChanges();
                    rootEntry.CommitChanges();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
        }



    }
}