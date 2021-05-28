using System;
using System.Diagnostics.CodeAnalysis;

namespace Canopy.Cli.Executable
{
    public static class Guard
    {
        public static void Argument([DoesNotReturnIf(false)] bool requirement, string? message = null, params object?[] args)
        {
            if (!requirement) 
            {
                throw new ArgumentException(message != null ? string.Format(message, args) : "Argument guard failed.");
            }
        }

        public static void Operation([DoesNotReturnIf(false)] bool requirement, string? message = null, params object?[] args)
        {
            if (!requirement) 
            {
                throw new InvalidOperationException(message != null ? string.Format(message, args) : "Operation guard failed.");
            }
        }
    }
}