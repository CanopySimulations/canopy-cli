namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    using System.Collections.Generic;
    using System.Linq;

    public class ChannelDataColumns
    {
        private readonly Dictionary<string, List<CsvColumn>> items = new Dictionary<string, List<CsvColumn>>();

        public IEnumerable<string> SimTypes => this.items.Keys;

        public void Add(CsvColumn column)
        {
            List<CsvColumn> columns = null;
            if (!this.items.TryGetValue(column.Metadata.SimType, out columns))
            {
                columns = new List<CsvColumn>();
                this.items.Add(column.Metadata.SimType, columns);
            }

            columns.Add(column);
        }

        public IReadOnlyList<CsvColumn> GetColumns(string simType)
        {
            return Enumerable.ToList(this.items[simType]);
        }
    }
}