using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace DurableFunctions
{
    public static class WorkflowOrchestration
    {
        [FunctionName("O_ProcessWorkflow")]
        public static async Task<object> Workflow(
            [OrchestrationTrigger]
            DurableOrchestrationContext ctx,
            TraceWriter log)
        {
            log.Info("Orchestrator function processed a request.");

            EventInfo first = null;
            EventInfo[] second = null;
            EventInfo last = null;
            string approval = "UNKNOWN";


            // Retry up to 4 times with 5 second delay between each attempt
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 4) { Handle = ex => ex is InvalidOperationException };

            // Retrieve Input Argument;
            var eventId = ctx.GetInput<int>();

            try
            {
                // Activity 1
                if (!ctx.IsReplaying) log.Info("Execute Activity 1 in serial");
                first = await ctx.CallActivityWithRetryAsync<EventInfo>("A_Activity1", retryOptions, eventId);
                first.OrchestrationId = ctx.InstanceId;


                // SubOrchestration 1
                second = await ctx.CallSubOrchestratorWithRetryAsync<EventInfo[]>("O_SubWorkFlow1", retryOptions, eventId);


                //SubOrchestration 2
                last = await ctx.CallSubOrchestratorWithRetryAsync<EventInfo>("O_SubWorkFlow2", retryOptions, eventId);
            }
            catch (Exception e)
            {
                // Cleanup
                if (!ctx.IsReplaying) log.Info($"Caught an error from an activity: { e.Message}");
                await ctx.CallActivityAsync<string>("A_Cleanup", new[] { eventId });

                return new
                {
                    Error = "Failed to process Workflow",
                    e.Message
                };
            }

            return new
            {
                First = first,
                Second = second,
                Last = last
            };
        }

        [FunctionName("O_SubWorkFlow1")]
        public static async Task<EventInfo[]> SubWorkFlow1(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            TraceWriter log)
        {
            // Retrieve Input Argument;
            var eventId = ctx.GetInput<int>();


            // Get Configuration Data
            if (!ctx.IsReplaying) log.Info("Execute Configuration Get Activity Codes");
            var actionCodes = await ctx.CallActivityAsync<int[]>("A_GetActionCodes", null);


            // Fan Out Pattern for Activity 2.
            if (!ctx.IsReplaying) log.Info("Execute Activity 2 in parallel");
            var actionTasks = new List<Task<EventInfo>>();
            foreach (var action in actionCodes)
            {
                var info = new EventInfo { EventId = eventId, Action = action, OrchestrationId = ctx.InstanceId };
                var task = ctx.CallActivityAsync<EventInfo>("A_Activity2", info);
                actionTasks.Add(task);
            }

            var results = await Task.WhenAll(actionTasks);
            return results;
        }

        [FunctionName("O_SubWorkFlow2")]
        public static async Task<EventInfo> SubWorkFlow2(
           [OrchestrationTrigger] DurableOrchestrationContext ctx,
           TraceWriter log)
        {
            string approvalResult = "UNKNOWN";
            // Retry up to 4 times with 5 second delay between each attempt
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 4) { Handle = ex => ex is InvalidOperationException };

            // Retrieve Input Argument;
            var eventId = ctx.GetInput<int>();

            // Event Interaction
            if (!ctx.IsReplaying) log.Info("Execute Activity 3 with Event Waiting");
            await ctx.CallActivityAsync("A_SendApproval", new ApprovalInfo()
            {
                OrchestrationId = ctx.InstanceId,
                EventId = eventId
            });

            // Implement Timeout
            using (var cts = new CancellationTokenSource())
            {
                var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(30);
                var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
                var approvalTask = ctx.WaitForExternalEvent<string>("EVENT_APPROVAL");

                var winner = await Task.WhenAny(approvalTask, timeoutTask);
                if (winner == approvalTask)
                {
                    approvalResult = approvalTask.Result;
                    cts.Cancel(); // we should cancel the timeout task
                }
                else
                {
                    approvalResult = "TIMEOUT";
                }
            }

            // Approval or Rejected Activities
            if (approvalResult == "APPROVED")
            {
                if (!ctx.IsReplaying) log.Info("APPROVAL - Execute Approval Function");
                var result = await ctx.CallActivityWithRetryAsync<EventInfo>("A_Approval", retryOptions, eventId);
                result.OrchestrationId = ctx.InstanceId;
                return result;
            }
            else
            {
                if (!ctx.IsReplaying) log.Info("REJECTED - Execute Rejected Function");
                var result = await ctx.CallActivityWithRetryAsync<EventInfo>("A_Rejected", retryOptions, eventId);
                result.OrchestrationId = ctx.InstanceId;

                if (approvalResult == "TIMEOUT") result.Action = -1;
                return result;
            }
        }

        [FunctionName("O_PeriodicTask")]
        public static async Task<int> ScheduledTask(
            [OrchestrationTrigger]
            DurableOrchestrationContext ctx,
            TraceWriter log)
        {
            var timesRun = ctx.GetInput<int>();
            timesRun++;
            if (!ctx.IsReplaying) log.Info($"Starting the PeriodicTask activity {ctx.InstanceId}, {timesRun}");

            await ctx.CallActivityAsync("A_PeriodicActivity", timesRun);
            var nextRun = ctx.CurrentUtcDateTime.AddSeconds(30);
            await ctx.CreateTimer(nextRun, CancellationToken.None);
            ctx.ContinueAsNew(timesRun);
            return timesRun;
        }
    }
}
