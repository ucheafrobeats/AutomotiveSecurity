using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomotiveWorld.DataAccess
{
    public class EntitiesRepository
    {
        protected ILogger Logger { get; }

        public EntitiesRepository(
            ILogger<Driver> logger)
        {

        }

        public async Task<int> Count<T>(IDurableEntityClient client) where T : EntityBase
        {
            int count = 0;

            Type type = typeof(T);
            string typeName = type.Name;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = 100,
                FetchState = true,
                EntityName = typeName
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

                    count++;
                }

                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);

            return count;
        }

        public async Task<DriverDto> GetAvailableDriver(IDurableEntityClient client)
        {
            DriverDto driverDto = null;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = 100,
                FetchState = true,
                EntityName = typeof(Driver).Name
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
                        driverDto = durableEntityStatus.State.ToObject<DriverDto>();

                        if (!driverDto.IsAvailable)
                        {
                            continue;
                        }

                        return driverDto;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize object, type=[{nameof(DriverDto)}], entityId=[{entityId}], error=[{ex}]");
                    }
                }

                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);
            return null;
        }

        public async Task<VehicleDto> GetAvailableVehicle(IDurableEntityClient client)
        {
            VehicleDto vehicleDto = null;

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
                        vehicleDto = durableEntityStatus.State.ToObject<VehicleDto>();

                        if (!vehicleDto.IsAvailable)
                        {
                            continue;
                        }

                        return vehicleDto;
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
