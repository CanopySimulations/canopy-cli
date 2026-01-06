using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public class ChannelDataFiles
    {
        private readonly Dictionary<string, List<VectorResultsDomain>> items = new Dictionary<string, List<VectorResultsDomain>>();

        public IEnumerable<string> SimTypes => this.items.Keys;

        public void Add(VectorResultsDomain domain)
        {
            if (!this.items.TryGetValue(domain.SimType, out var domains))
            {
                domains = new List<VectorResultsDomain>();
                this.items[domain.SimType] = domains;
            }

            domains.Add(domain);
        }

        public IReadOnlyList<VectorResultsDomain> GetColumns(string simType)
        {
            return this.items[simType];
        }

        public int Count() => items.Count;

        public bool Any() => items.Count > 0;
    }
}
