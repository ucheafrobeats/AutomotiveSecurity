using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomotiveWorld.DataAccess
{
    public class VehicleRepository
    {
        protected ILogger Logger { get; }

        public VehicleRepository(
            ILogger<Driver> logger)
        {

        }

        public async Task<VehicleDto> GetAvailableVehicle(IDurableEntityClient client)
        {
            VehicleDto vehicle = null;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = 100,
                FetchState = true,
                EntityName = typeof(Vehicle).Name
            };

            do
            {
                // Paginate over all entities
                var result = await client.ListEntitiesAsync(query, source.Token);
                if (result?.Entities == null)
                {
                    break;
                }

                foreach (var durableEntityStatus in result.Entities)
                {
                    var entityId = durableEntityStatus.EntityId.EntityKey;

                    if (durableEntityStatus.State == null)
                    {
                        // entity state might be null for instances marked as deleted and before being purged
                        continue;
                    }

                    try
                    {
                        vehicle = durableEntityStatus.State.ToObject<VehicleDto>();

                        if (!vehicle.IsAvailable)
                        {
                            continue;
                        }

                        return vehicle;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize object, type=[{nameof(VehicleDto)}], entityId=[{entityId}], error=[{ex}]");
                    }
                }

                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);
            return null;
        }
    }
}
