using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public class VectorResultsDomain
    {
        public VectorResultsDomain(string domain, string simType, IFile file)
        {
            Domain = domain;
            SimType = simType;
            File = file;
        }

        public string Domain { get; }
        public string SimType { get; }

        public IFile File { get; }
    }
}
