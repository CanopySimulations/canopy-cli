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
            List<VectorResultsDomain> domains = null;
            if (!this.items.TryGetValue(domain.SimType, out domains))
            {
                domains = new List<VectorResultsDomain>();
                this.items[domain.SimType] = domains;
            }

            domains.Add(domain);
        }

        public IReadOnlyList<VectorResultsDomain> GetColumns(string simType)
        {
            return Enumerable.ToList(this.items[simType]);
        }
    }
}
