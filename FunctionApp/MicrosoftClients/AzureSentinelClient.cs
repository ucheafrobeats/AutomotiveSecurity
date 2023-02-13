﻿using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.SecurityInsights;
using Azure.ResourceManager.SecurityInsights.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomotiveWorld.AzureClients
{
    public class MicrosoftSentinelClient
    {
        private readonly ILogger<MicrosoftSentinelClient> Logger;

        private readonly IDictionary<string, SecurityInsightsAlertRuleData> DefaultRules = new Dictionary<string, SecurityInsightsAlertRuleData>();

        private readonly ArmClient ArmClient;

        private readonly ResourceIdentifier ResourceIdentifier;

        public MicrosoftSentinelClient(ILogger<MicrosoftSentinelClient> log, string workspaceResourceId, string tableName)
        {
            Logger = log;
            DefaultAzureCredential defaultAzureCredential = new();
            ArmClient = new(defaultAzureCredential);

            ResourceIdentifier = new(workspaceResourceId);

            InitializeDefaultRules(tableName);
        }

        public async Task<bool> AddAllDefaultRules()
        {
            OperationalInsightsWorkspaceSecurityInsightsResource operationalInsightsWorkspaceSecurityInsightsResource = ArmClient.GetOperationalInsightsWorkspaceSecurityInsightsResource(ResourceIdentifier);
            SecurityInsightsAlertRuleCollection securityInsightsAlertRuleCollection = operationalInsightsWorkspaceSecurityInsightsResource.GetSecurityInsightsAlertRules();


            foreach (var (ruleId, rule) in DefaultRules)
            {
                try
                {
                    await securityInsightsAlertRuleCollection.CreateOrUpdateAsync(
                            waitUntil: WaitUntil.Completed,
                            ruleId: ruleId,
                            data: rule);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to create Microsoft Sentinel Rules, ruleId=[{ruleId}], error=[{ex.Message}]");
                    return false;
                }
            }

            return true;
        }

        private void InitializeDefaultRules(string tableName)
        {
            SecurityInsightsAlertRuleEntityMapping driverIdEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.Account
            };
            driverIdEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "AadUserId", ColumnName = "DriverId" });

            SecurityInsightsAlertRuleEntityMapping driverNameEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.Account
            };
            driverNameEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "FullName", ColumnName = "DriverName" });

            SecurityInsightsAlertRuleEntityMapping vehicleIdEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.File
            };
            vehicleIdEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "Name", ColumnName = "VehicleId" });

            SecurityInsightsAlertRuleEntityMapping vehicleModelEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.Host
            };
            vehicleModelEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "FullName", ColumnName = "VehicleModel" });

            SecurityInsightsGroupingConfiguration groupingConfiguration = new(
                true,
                false,
                TimeSpan.Parse("05:00:00"),
                SecurityInsightsGroupingMatchingMethod.AnyAlert);
            groupingConfiguration.GroupByCustomDetails.Add("SubType");
            groupingConfiguration.GroupByCustomDetails.Add("VehicleId");
            SecurityInsightsIncidentConfiguration incidentConfiguration = new(true)
            {
                GroupingConfiguration = groupingConfiguration
            };

            SecurityInsightsScheduledAlertRule vehicleMaintenance = new()
            {
                Query = $@"let Table = {tableName}_CL;
let MaintenanceEvents = Table
    | where type_s == ""Maintenance"";
let VehicleEvents = Table 
    | where type_s == ""Vehicle""
    | extend VehicleId = entityId_s
    | extend VehicleEventsJson = parse_json(jsonAsString_s)
    | extend VehicleModel = VehicleEventsJson[""make""];
let AssignedDriverEvents = Table
    | where type_s == ""Driver""
    | extend AssignedDriverEventsJson = parse_json(jsonAsString_s)
    | extend Assignment = AssignedDriverEventsJson[""assignment""]
    | where isnotempty(Assignment)
    | extend DriverId = tostring(Assignment[""driverDto""][""id""])
    | extend AssignmendVehicleId = tostring(Assignment[""vehicleDto""][""id""])
    | extend DriverName = AssignedDriverEventsJson[""name""];
let RequiredMaintenance = VehicleEvents
    | join MaintenanceEvents on entityId_s
    | join AssignedDriverEvents on $left.entityId_s == $right.AssignmendVehicleId
    | where isnotempty(subType_s1)
    | extend MaintenancePayload = jsonAsString_s1
    | where jsonAsString_s1 contains 'isFaulty"":true'
    | summarize arg_max(TimeGenerated, *) by entityId_s, subType_s1;
RequiredMaintenance
| project TimeGenerated, EntityId = entityId_s, Payload = MaintenancePayload, SubType = subType_s1, DriverId, DriverName, VehicleModel, VehicleId",
                QueryFrequency = TimeSpan.Parse("05:00:00"),
                QueryPeriod = TimeSpan.Parse("05:00:00"),
                DisplayName = $"Vehicle require maintenance",
                Description = "Vehicle require maintenance by part.",
                Severity = SecurityInsightsAlertSeverity.Low,
                TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                TriggerThreshold = 0,
                IncidentConfiguration = incidentConfiguration,
                EventGroupingAggregationKind = EventGroupingAggregationKind.AlertPerResult,
                IsEnabled = true,
                SuppressionDuration = TimeSpan.Parse("05:00:00"),
                IsSuppressionEnabled = false,
                AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                {
                    AlertDisplayNameFormat = "Vehicle require maintenance, part={{SubType}}",
                    AlertDescriptionFormat = "VehicleID: {{VehicleId}}\nPayload: {{Payload}}"
                }
            };
            vehicleMaintenance.CustomDetails.Add("VehicleId", "VehicleId");
            vehicleMaintenance.CustomDetails.Add("SubType", "SubType");
            vehicleMaintenance.CustomDetails.Add("Payload", "Payload");

            vehicleMaintenance.EntityMappings.Add(vehicleIdEntityMappings);
            DefaultRules.Add("c8794ea7-8648-4fb8-88ba-82197d52461b", vehicleMaintenance);


            SecurityInsightsScheduledAlertRule securityInsightsScheduledAlertRule = new SecurityInsightsScheduledAlertRule()
            {
                Query = $@"{tableName}_CL 
                            | where TimeGenerated > ago(1d)
                            | extend Vehicle = parse_json(vehicleJsonAsString_s)
                            | extend applications = Vehicle['parts']['Multimedia']['applications']
                            | where isnotempty(applications)| mv-expand Application = applications
                            | project Name = Application['name'], Version = Application['version'], Enabled = Application['enabled'], vin_s
                            | where Name in ('Youtube') and Version < 3.0
                            | distinct tostring(Name), tostring(Version)",
                QueryFrequency = TimeSpan.Parse("00:05:00"),
                QueryPeriod = TimeSpan.Parse("05:00:00"),
                DisplayName = $"tre Applications Vulnerabilities",
                Description = "",
                Severity = SecurityInsightsAlertSeverity.High,
                TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                TriggerThreshold = 0,
                EventGroupingAggregationKind = EventGroupingAggregationKind.SingleAlert,
                IsEnabled = true,
                SuppressionDuration = TimeSpan.Parse("05:00:00"),
                IsSuppressionEnabled = false,
                AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                {
                    AlertDisplayNameFormat = "QWE Update application {{Name}}",
                    AlertDescriptionFormat = "Alert generated by Analytics scheduled rule"
                }
            };
            securityInsightsScheduledAlertRule.CustomDetails.Add("Name", "Name");
            DefaultRules.Add("b8794ea7-8648-4fb8-88ba-82197d52461b", securityInsightsScheduledAlertRule);

            SecurityInsightsScheduledAlertRule a = new SecurityInsightsScheduledAlertRule()
            {
                Query = $@"demoURLMonitorY_CL 
                            | where TimeGenerated > ago(1d)
                            | extend Vehicle = parse_json(vehicleJsonAsString_s)
                            | extend applications = Vehicle['parts']['Multimedia']['applications']
                            | where isnotempty(applications)| mv-expand Application = applications
                            | project Name = Application['name'], Version = Application['version'], Enabled = Application['enabled'], vin_s
                            | where Name in ('Youtube') and Version < 3.0
                            | distinct tostring(Name), tostring(Version)",
                QueryFrequency = TimeSpan.Parse("00:05:00"),
                QueryPeriod = TimeSpan.Parse("05:00:00"),
                DisplayName = $"tre Applications Vulnerabilities",
                Description = "",
                Severity = SecurityInsightsAlertSeverity.High,
                TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                TriggerThreshold = 0,
                EventGroupingAggregationKind = EventGroupingAggregationKind.SingleAlert,
                IsEnabled = true,
                SuppressionDuration = TimeSpan.Parse("05:00:00"),
                IsSuppressionEnabled = false,
                AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                {
                    AlertDisplayNameFormat = "QWE Update application {{Name}}",
                    AlertDescriptionFormat = "Alert generated by Analytics scheduled rule"
                }
            };
            a.CustomDetails.Add("Name", "Name");
            DefaultRules.Add("a8794ea7-8648-4fb8-88ba-82197d52461b", a);


            /** Workbook 
// Vehicle inventory
AMICAR_CL 
| where type_s == "Vehicle"
| extend VehicleId = entityId_s
| extend Vehicle = parse_json(jsonAsString_s)
| summarize arg_max(TimeGenerated, *) by VehicleId
| project 
    VehicleId,
    Status = tostring(Vehicle["status"]),
    Make = tostring(Vehicle["make"]),
    EngineType = tostring(Vehicle["parts"]["Engine"]["type"]),
    Model = tostring(Vehicle["model"]),
    SerialNumber = tostring(Vehicle["serialNumber"]),
    Year = tostring(Vehicle["year"]),
    Color = tostring(Vehicle["color"]),
    Kilometers = tostring(Vehicle["kilometers"]),
    VehicleType = tostring(Vehicle["vehicleType"]),
    IsAvailable = tostring(Vehicle["isAvailable"]),
    Engine = tostring(Vehicle["parts"]["Engine"]),
    Tires = tostring(Vehicle["parts"]["Tires"])
            **/
        }
    }
}