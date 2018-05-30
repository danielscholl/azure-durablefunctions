using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions
{
    public class EventInfo
    {
        public string OrchestrationId { get; set; }
        public int EventId { get; set; }
        public int Action { get; set; }
        public string Activity { get; set; }
    }
}
