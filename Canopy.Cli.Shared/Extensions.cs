using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared
{
    public static class Extensions
    {
        /// <summary>
        /// http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        /// http://stackoverflow.com/a/25877042/37725
        /// </summary>
        public static async Task ForEachAsync<T>(this IEnumerable<T> collection, int maxDegreeOfConcurrency, Func<T, Task> taskFactory)
        {
            var activeTasks = new List<Task>(maxDegreeOfConcurrency);
            foreach (var task in collection.Select(taskFactory))
            {
                activeTasks.Add(task);
                if (activeTasks.Count == maxDegreeOfConcurrency)
                {
                    var completedTask = await Task.WhenAny(activeTasks.ToArray());
                    await completedTask;
                    activeTasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(activeTasks.ToArray());
        }

        public static IReadOnlyList<IReadOnlyList<string>> ToCsvColumns(this string input)
        {
            return input.ToCsvRows().InvertCsvSelection();
        }

        public static IReadOnlyList<IReadOnlyList<string>> ToCsvRows(this string input)
        {
            var lines = input.SplitLines();
            var rowSets = lines.Select(v => v.SplitCsvLine().ToList()).ToList();
            return rowSets;
        }
        public static IEnumerable<string> SplitLines(this string input)
        {
            return input.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        }

        // http://stackoverflow.com/a/23888636/37725
        public static IEnumerable<string> SplitCsvLine(this string input)
        {
            var csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

            foreach (Match match in csvSplit.Matches(input))
            {
                yield return match.Value.TrimStart(',').WithoutQuotes();
            }
        }

        public static IReadOnlyList<IReadOnlyList<string>> InvertCsvSelection(this IReadOnlyList<IReadOnlyList<string>> input)
        {
            if(input.Count == 0)
            {
                return input;
            }

            var innerCount = input.First().Count;
            if(input.Any(v => v.Count != innerCount))
            {
                throw new InvalidOperationException("Every row must have the name number of columns.");
            }

            var result = new List<List<string>>();
            for (int innerIndex = 0; innerIndex < innerCount; innerIndex++)
            {
                result.Add(input.Select(v => v[innerIndex]).ToList());
            }

            return result;
        }

        public static string WithoutQuotes(this string input)
        {
            if (input == null || input.Length < 2)
            {
                return input;
            }

            if ((input.First() == '"' && input.Last() == '"')
                || (input.First() == '\'' && input.Last() == '\''))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        public static string WithQuotes(this string input)
        {
            if (input == null)
            {
                return null;
            }

            return "\"" + input.WithoutQuotes() + "\"";
        }
    }
}