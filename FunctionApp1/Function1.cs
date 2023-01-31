using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp1
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //// Tell the Durable Entity to increase a counter
            //await client.SignalEntityAsync<ICounter>("aKey", e => e.IncreaseCount(1));

            return new OkResult();
        }
    }

    //public interface ICounter
    //{
    //    void IncreaseCount(int amount);
    //}

    //[JsonObject(MemberSerialization.OptIn)]
    //public class Counter : ICounter
    //{
    //    private readonly ILogger _log;

    //    public Counter(ILogger<Counter> log)
    //    {
    //        _log = log;
    //    }

    //    [JsonProperty("value")]
    //    public int CurrentValue { get; set; }

    //    public void IncreaseCount(int amount)
    //    {
    //        this.CurrentValue += amount;

    //        if (this.CurrentValue > 10)
    //            return;

    //        // Schedule a call to this entity to IncreaseCount after 5 seconds. 
    //        // Once the method is called, schedule it again to create the effect of a timer
    //        Entity.Current.SignalEntity<ICounter>(Entity.Current.EntityId, DateTime.Now.AddSeconds(5), e => e.IncreaseCount(1));
    //        _log.LogInformation(this.CurrentValue.ToString());
    //    }

    //    [FunctionName(nameof(Counter))]
    //    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
    //    {
    //        return ctx.DispatchAsync<Counter>();
    //    }
    //}
}