using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions
{
    public class Approval
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string OrchestrationId { get; set; }
    }

    public class ApprovalInfo
    {
        public string OrchestrationId { get; set; }
        public int EventId { get; set; }
    }
}
