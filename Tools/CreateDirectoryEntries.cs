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