using System;
using System.Net;

namespace Common
{
    public class WebClientWithTimeout : WebClient
    {
        private int Timeout { get; set; }

        public WebClientWithTimeout() : this(30000) { }

        public WebClientWithTimeout(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}