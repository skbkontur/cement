using System;

namespace Common
{
    public class CementException : Exception
    {
        protected CementException()
        {
        }

        public CementException(string message)
            : base(message)
        {
        }
    }
}
