using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable
{
    public static class ExceptionUtilities
    {
        public static bool IsFromCancellation(Exception outerException)
        {
            foreach (var exception in Flatten(outerException))
            {
                if (IsCancellationException(exception))
                {
                    return true;
                };
            }

            return false;
        }

        private static bool IsCancellationException(Exception t)
        {
            return (t is TaskCanceledException || t is OperationCanceledException);
        }

        private static IReadOnlyList<Exception> Flatten(Exception exception)
        {
            var output = new List<Exception>();
            Flatten(exception, output);
            return output;
        }

        private static void Flatten(Exception exception, List<Exception> output)
        {
            output.Add(exception);

            var aggregateException = exception as AggregateException;
            if (aggregateException?.InnerExceptions != null)
            {
                foreach (var child in aggregateException.InnerExceptions)
                {
                    if (child != null)
                    {
                        Flatten(child, output);
                    }
                }
            }
            else if (exception.InnerException != null)
            {
                Flatten(exception.InnerException, output);
            }
        }
    }
}