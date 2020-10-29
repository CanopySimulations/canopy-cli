namespace Canopy.Cli.Shared.StudyProcessing
{
    using System;

    public class AbortProcessingException : Exception
    {
        public AbortProcessingException(string message, Exception inner)
            : base(message, inner)
        {
            
        }
    }
}