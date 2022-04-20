using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Common
{
    public class WebClientWithTimeout : HttpClient
    {
        public WebClientWithTimeout() : this(30000) { }

        public WebClientWithTimeout(int timeout)
        {
            base.Timeout = new TimeSpan(0, 0, timeout/1000);
        }

        static WebClientWithTimeout()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain,
                                                      SslPolicyErrors error)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (error == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
                cert.Subject,
                error.ToString());

            return false;
        }
    }
}