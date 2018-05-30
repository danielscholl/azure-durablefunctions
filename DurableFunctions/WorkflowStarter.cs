using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace DurableFunctions
{
    public static class WorkflowStarter
    {
        [FunctionName("WorkflowStarter")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Workflow/Start")]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            TraceWriter log)
        {

            // parse query parameter
            var eventId = req.RequestUri.ParseQueryString()["eventId"];

            var instanceId = await starter.StartNewAsync("O_ProcessWorkflow", Convert.ToInt32(eventId));

            log.Info($"Started  Workflow orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Approval")]
        public static async Task<HttpResponseMessage> Approve(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Approval/{id}")]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            [Table("Approvals", "Approval", "{id}", Connection = "azureWebJobsStorage")] Approval approval,
            TraceWriter log)
        {
            string result = req.RequestUri.ParseQueryString()["result"];

            if (result == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Need an approval result");

            log.Warning($"Sending approval result to {approval.OrchestrationId} of {result}");

            await client.RaiseEventAsync(approval.OrchestrationId, "EVENT_APPROVAL", result);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [FunctionName("StartPeriodic")]
        public static async Task<HttpResponseMessage> StartPeriodicTask(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            var instanceId = await client.StartNewAsync("O_PeriodicTask", 0);
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
