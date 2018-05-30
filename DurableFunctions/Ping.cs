using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace DurableFunctions
{
    public static class Ping
    {
        [FunctionName("ping")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequestMessage req, 
            TraceWriter log)
        {
            log.Info("Ping function processed a request.");
            return req.CreateResponse(HttpStatusCode.OK, "pong");
        }
    }
}
