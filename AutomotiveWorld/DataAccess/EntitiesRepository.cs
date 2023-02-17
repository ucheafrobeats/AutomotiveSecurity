using AutomotiveWorld.Entities;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
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
            Logger = logger;
        }

        public async Task<int> Count<TEntity>(IDurableEntityClient client) where TEntity : EntityBase
        {
            int count = 0;

            Type type = typeof(TEntity);
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

        public async Task<TDto> GetFirst<TEntity, TDto>(IDurableEntityClient client, Predicate<TDto> predicate, int pageSize = 100, bool fetchState = true)
            where TEntity : EntityBase
            where TDto : EntityDtoBase
        {
            TDto dto = null;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = pageSize,
                FetchState = fetchState,
                EntityName = typeof(TEntity).Name
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
                        dto = durableEntityStatus.State.ToObject<TDto>();

                        if (!predicate(dto))
                        {
                            continue;
                        }

                        return dto;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize object, type=[{nameof(TDto)}], entityId=[{entityId}], error=[{ex}]");
                    }
                }

                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);
            return null;
        }

        public static bool PredicateIsAvailable(EntityDtoBase dto)
        {
            return dto.IsAvailable;
        }

        public static bool PredicateHasMultimediaAndAvailable(EntityDtoBase dto)
        {
            if (typeof(VehicleDto) != dto.GetType())
            {
                return false;
            }

            VehicleDto vehicleDto = (VehicleDto)dto;

            return dto.IsAvailable && vehicleDto.TryGetPart(VehiclePartType.Multimedia, out Multimedia _);
        }
    }
}
