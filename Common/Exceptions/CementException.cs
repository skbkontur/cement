using System;

namespace Common
{
    public class CementException : Exception
    {
        public CementException(string message)
            : base(message)
        {
        }

        protected CementException()
        {
        }
    }
}
