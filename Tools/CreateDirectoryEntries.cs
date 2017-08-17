using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace CreateTestDirectoryEntries
{
    internal class Program
    {
        
        private static void Main(string[] args)
        {
            const int maxValidityDays = 365;
            const int warningPeriodInDays = 90;
            const int numberOfCertsToWrite = 100;

            int certCount = 0;
            int expiringCertCount = 0;

            var rootEntry = new DirectoryEntry("LDAP://192.168.1.230/OU=Test Users,O=Red Kestrel");
            rootEntry.Username = "CN=admin,O=Red Kestrel";
            rootEntry.Password = "Top111Secret";
            rootEntry.AuthenticationType = AuthenticationTypes.None;

            for (var i = 0; i < numberOfCertsToWrite; i++)
                try
                {
                    var name = GenerateRandomName();

                    Random r = new Random();
                    int validityPeriodInDays = r.Next(0, maxValidityDays);
                    
                    

                    var cert = GenerateSelfSignedCertificate(name, name, validityPeriodInDays);
                    var data = cert.RawData;

                    var userEntry = rootEntry.Children.Add($"CN=Test--{name}-{i}", "user");
                    userEntry.Properties["userCertificate"].Insert(0, data);
                    userEntry.CommitChanges();
                    rootEntry.CommitChanges();
                    certCount += 1;
                    if (validityPeriodInDays <= warningPeriodInDays)
                    {
                        expiringCertCount += 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Console.WriteLine($"Wrote {certCount} certs to AD");
                Console.WriteLine($"Number of certs with validity period greater than 0 and <= {warningPeriodInDays} is {expiringCertCount} (EXPIRING CERTS)");
        }

        public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string issuerName, int validityPeriodInDays)

        {
            const string signatureAlgorithm = "SHA256withRSA";


            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            var subjectKeyPair = GeneratePublicPrivateKeyPair();
            var issuerKeyPair = subjectKeyPair;

            ISignatureFactory signatureFactory =
                new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private, random);


            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, true,
                new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));


            var serialNumber =
                BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);


            var subjectDn = new X509Name("CN=" + subjectName);
            var issuerDn = new X509Name("CN=" + issuerName);
            certificateGenerator.SetIssuerDN(issuerDn);
            certificateGenerator.SetSubjectDN(subjectDn);

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddDays(validityPeriodInDays);
            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);
            
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);


            var certificate = certificateGenerator.Generate(signatureFactory);
            
            
            var x509 = new X509Certificate2(DotNetUtilities.ToX509Certificate(certificate));
            x509.FriendlyName = subjectName;

            return x509;
        }

        public static AsymmetricCipherKeyPair GeneratePublicPrivateKeyPair()
        {
            var keyGenerationParameters =
                new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 2048);
            var rsaKeyPairGenerator = new RsaKeyPairGenerator();
            rsaKeyPairGenerator.Init(keyGenerationParameters);
            var keyPair = rsaKeyPairGenerator.GenerateKeyPair();
            return keyPair;
        }

        public static string GenerateRandomName()
        {
            var random = new Random();

            var givenName = new List<string>();
            givenName.Add("Paul");
            givenName.Add("Mikhail");
            givenName.Add("Alexander");
            givenName.Add("Jose");
            givenName.Add("Wilhelm");
            givenName.Add("Emanuel");
            givenName.Add("Albert");
            givenName.Add("Otto");
            givenName.Add("Jimmy");
            givenName.Add("Ada");

            var surname = new List<string>();
            surname.Add("Turtle");
            surname.Add("Nutters");
            surname.Add("Rattlebag");
            surname.Add("Cornfoot");
            surname.Add("Jelly");
            surname.Add("Piggs");
            surname.Add("Demon");
            surname.Add("Legg");
            surname.Add("Dwyer");
            surname.Add("Smart");

            return $"{givenName[random.Next(0, givenName.Count)]} {surname[random.Next(0, givenName.Count)]}";
        }
    }
}