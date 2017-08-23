using System.Security.Cryptography.X509Certificates;

namespace DirectoryCertChecker
{
    internal class Certificate : X509Certificate2
    {
        public Certificate(X509Certificate cert, int warningPeriodInDays)
            : base(cert)
        {
        }
    }
}