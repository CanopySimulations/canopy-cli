using System;
namespace Canopy.Api.Client
{
    public class RecoverableException : Exception
    {
        public RecoverableException(string message)
            : base(message)
        {
        }
    }
}
