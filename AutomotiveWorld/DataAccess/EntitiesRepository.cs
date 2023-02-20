using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutomotiveWorld.DataAccess
{
    public class EntitiesRepository
    {
        private const int UpgradeFirmwarePageDelay = 20 * 1000;

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

        public async Task<int> UpgradeFirmware(IDurableEntityClient client, VehiclePartType part, string vendor, double version, int pageSize = 100, bool fetchState = true)
        {
            int count = 0;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = pageSize,
                FetchState = fetchState,
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
                        VehicleDto vehicleDto = durableEntityStatus.State.ToObject<VehicleDto>();
                        PartDto partDto = null;

                        if (part == VehiclePartType.Multimedia)
                        {
                            if (vehicleDto.TryGetPart(VehiclePartType.Multimedia, out Multimedia multimedia) && multimedia.Firmware.Vendor == vendor)
                            {
                                multimedia.Firmware.Version = version;

                                partDto = new()
                                {
                                    Type = VehiclePartType.Multimedia,
                                    Part = multimedia
                                };


                            }
                        }
                        else if (part == VehiclePartType.Computer)
                        {
                            if (vehicleDto.TryGetPart(VehiclePartType.Computer, out Computer computer) && computer.Firmware.Vendor == vendor)
                            {
                                computer.Firmware.Version = version;

                                partDto = new()
                                {
                                    Type = VehiclePartType.Computer,
                                    Part = computer
                                };
                            }
                        }

                        if (partDto is null)
                        {
                            continue;
                        }

                        count++;
                        await client.SignalEntityAsync<IVehicle>(vehicleDto.Id, proxy => proxy.SetPart(partDto));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize object, type=[{nameof(VehicleDto)}], entityId=[{entityId}], error=[{ex}]");
                    }
                }

                await Task.Delay(UpgradeFirmwarePageDelay);
                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);

            return count;
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
