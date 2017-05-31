using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Canopy.Cli.Executable
{
    public static class Utilities
    {
        public static void WriteTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> values, int padding = 1)
        {
            var lines = new List<List<string>>
            {
                headers.ToList(),
                headers.Select(v => string.Concat(Enumerable.Repeat("-", v?.Length ?? 0))).ToList()
            }
            .Concat(values.Select(v => v.ToList())).ToList();

            // Calculate maximum numbers for each element accross all lines
            var numElements = lines[0].Count;
            var maxValues = new int[numElements];
            for (int i = 0; i < numElements; i++)
            {
                maxValues[i] = lines.Max(x => (x.Count > i + 1 && x[i] != null ? x[i].Length : 0)) + padding;
            }

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine();
                sb.Append(" ");
                for (int i = 0; i < line.Count; i++)
                {
                    var value = line[i];
                    // Append the value with padding of the maximum length of any value for this element
                    if (value != null)
                    {
                        sb.Append(value.PadRight(maxValues[i]));
                    }
                }
            }

            Console.WriteLine(sb.ToString());
        }        
    }
}
