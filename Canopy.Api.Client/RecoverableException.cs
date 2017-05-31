using System;
namespace Canopy.Api.Client
{
    public class RecoverableException : Exception
    {
        public RecoverableException(string message, Exception t = null)
            : base(message, t)
        {
        }
    }
}
