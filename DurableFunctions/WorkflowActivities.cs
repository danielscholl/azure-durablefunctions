using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace DurableFunctions
{
    public static class WorkflowActivities
    {
        [FunctionName("A_Activity1")]
        public static async Task<EventInfo> ActivityFunction1(
            [ActivityTrigger]
            int eventId,
            TraceWriter log)
        {
            log.Info($"Activity 1 is being Processing for Event #{eventId}");

            // Simulate doing the activity
            await Task.Delay(1000);

            return new EventInfo
            {
                EventId = eventId,
                Activity = "Executed Activity 1"
            };
        }

        [FunctionName("A_Activity2")]
        public static async Task<EventInfo> ActivityFunction2(
            [ActivityTrigger]
            EventInfo info,
            TraceWriter log)
        {
            log.Info($"Activity 2 is being Processing for Event #{info.EventId}");

            // Simulate doing the activity
            await Task.Delay(1000);

            info.Activity = "Executed Activity 2";

            return info;
        }

        [FunctionName("A_Approval")]
        public static async Task<EventInfo> ActivityApproval(
            [ActivityTrigger]
            int eventId,
            TraceWriter log)
        {
            log.Info($"Approval Activity is being Processed for Event #{eventId}");

            // Simulate doing the activity
            await Task.Delay(1000);

            return new EventInfo
            {
                EventId = eventId,
                Activity = "Executed Approval Activity"
            };
        }

        [FunctionName("A_Rejected")]
        public static async Task<EventInfo> ActivityRejected(
            [ActivityTrigger]
            int eventId,
            TraceWriter log)
        {
            log.Info($"Rejected Activity is being Processed for Event #{eventId}");

            // Simulate doing the activity
            await Task.Delay(1000);

            return new EventInfo
            {
                EventId = eventId,
                Activity = "Executed Rejected Activity"
            };
        }

        [FunctionName("A_GetActionCodes")]
        public static async Task<int[]> GetActionCodes(
            [ActivityTrigger] object input,
            TraceWriter log)
        {
            log.Info($"Getting Activity Codes out of Configuration");
            return ConfigurationManager.AppSettings["ActivityCodes"]
                .Split(',')
                .Select(int.Parse)
                .ToArray();
        }

        [FunctionName("A_SendApproval")]
        public static void ApprovalRequest(
            [ActivityTrigger] ApprovalInfo approvalInfo,
            [Table("Approvals", "AzureWebJobsStorage")] out Approval approval,
            TraceWriter log)
        {
            var approvalCode = Guid.NewGuid().ToString("N");
            approval = new Approval
            {
                PartitionKey = "Approval",
                RowKey = approvalCode,
                OrchestrationId = approvalInfo.OrchestrationId
            };

            log.Info($"Executed approval activity {approvalInfo.EventId}");
            var host = ConfigurationManager.AppSettings["Host"];

            var functionAddress = $"{host}/api/Approval/{approvalCode}";
            var approvedLink = functionAddress + "?result=APPROVED";
            var rejectedLink = functionAddress + "?result=REJECTED";
            var message = $"Event #{approvalInfo.EventId} ready to be approved.  Approve={approvedLink}  Reject={rejectedLink}";
            log.Info(message);
        }


        [FunctionName("A_Cleanup")]
        public static async Task Cleanup(
            [ActivityTrigger] int eventId,
            TraceWriter log)
        {
            log.Info($"Executed Cleanup Activity #{eventId}");

            // Simulate doing the activity
            await Task.Delay(1000);
        }

        [FunctionName("A_PeriodicActivity")]
        public static async Task PeriodicActivity(
            [ActivityTrigger] int timesRun,
            TraceWriter log)
        {
            log.Warning($"Running Periodic Activity, times run = {timesRun}");

        }
    }
}
