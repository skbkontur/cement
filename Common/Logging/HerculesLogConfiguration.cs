using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Logging
{
    public sealed class HerculesLogConfiguration
    {
        public string Stream { get; set; }
        public string ApiKey { get; set; }
        public string ServerUrl { get; set; }
        public string Project { get; set; }
        public string Environment { get; set; } 
        public long MaximumMemoryConsumptionInBytes { get; set; }
    }
}
