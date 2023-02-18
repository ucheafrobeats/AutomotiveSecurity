using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.SecurityInsights;
using Azure.ResourceManager.SecurityInsights.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

                    Logger.LogInformation($"Applied Microsoft Sentinel Alert Rules, ruleId=[{ruleId}]");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to create Microsoft Sentinel Alert Rules, ruleId=[{ruleId}], error=[{ex.Message}], count=[{securityInsightsAlertRuleCollection?.Count()}]");
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

            SecurityInsightsAlertRuleEntityMapping fileSystemEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.File
            };
            fileSystemEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "Name", ColumnName = "Filename" });

            SecurityInsightsAlertRuleEntityMapping peripheralEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.Process
            };
            peripheralEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "CommandLine", ColumnName = "PeripheralName" });

            SecurityInsightsAlertRuleEntityMapping subtypeEntityMappings = new()
            {
                EntityType = SecurityInsightsAlertRuleEntityMappingType.RegistryValue
            };
            subtypeEntityMappings.FieldMappings.Add(new SecurityInsightsFieldMapping() { Identifier = "Name", ColumnName = "SubType" });


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


            {
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
                    QueryFrequency = TimeSpan.Parse("12:00:00"),
                    QueryPeriod = TimeSpan.Parse("12:00:00"),
                    DisplayName = $"Vehicle require maintenance",
                    Description = "Vehicle require maintenance by part.",
                    Severity = SecurityInsightsAlertSeverity.Low,
                    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                    TriggerThreshold = 0,
                    IncidentConfiguration = incidentConfiguration,
                    EventGroupingAggregationKind = EventGroupingAggregationKind.AlertPerResult,
                    IsEnabled = true,
                    SuppressionDuration = TimeSpan.Parse("12:00:00"),
                    IsSuppressionEnabled = false,
                    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                    {
                        AlertDisplayNameFormat = "Vehicle require maintenance, part={{SubType}}",
                        AlertDescriptionFormat = "Vehicle require maintenance by part.\nVehicleID: {{VehicleId}}\nPayload: {{Payload}}"
                    }
                };
                vehicleMaintenance.CustomDetails.Add("VehicleId", "VehicleId");
                vehicleMaintenance.CustomDetails.Add("SubType", "SubType");
                vehicleMaintenance.CustomDetails.Add("Payload", "Payload");

                vehicleMaintenance.EntityMappings.Add(vehicleIdEntityMappings);
                DefaultRules.Add("c8794ea7-8648-4fb8-88ba-82197d52461b", vehicleMaintenance);

            }

            {
                SecurityInsightsScheduledAlertRule vehicleMultimediaNewFile = new()
                {
                    Query = $@"let Table = {tableName}_CL;
let NewFileEvents = Table
    | where type_s == ""Security""
    | where subType_s == ""FileSystem""
    | extend Json = parse_json(jsonAsString_s)
    | extend Message = tostring(Json[""message""])
    | extend Action = tostring(Json[""action""])
    | where isempty(Action)
    | summarize arg_max(TimeGenerated, *) by VehicleId = entityId_s, Message, Action, Type = type_s, SubType = subType_s
    | sort by TimeGenerated desc 
    | extend Payload = Json[""jsonAsString""]
    | mv-expand Filename = parse_json(tostring(Payload))[""files""]
    | project  TimeGenerated, VehicleId, Type, SubType, Message, Filename;
NewFileEvents",
                    QueryFrequency = TimeSpan.Parse("01:00:00"),
                    QueryPeriod = TimeSpan.Parse("01:00:00"),
                    DisplayName = $"Vehicle multimedia new file detected",
                    Description = "Vehicle multimedia new file detected",
                    Severity = SecurityInsightsAlertSeverity.Medium,
                    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                    TriggerThreshold = 0,
                    IncidentConfiguration = incidentConfiguration,
                    EventGroupingAggregationKind = EventGroupingAggregationKind.AlertPerResult,
                    IsEnabled = true,
                    SuppressionDuration = TimeSpan.Parse("01:00:00"),
                    IsSuppressionEnabled = false,
                    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                    {
                        AlertDisplayNameFormat = "Vehicle multimedia new file detected, VehicleId={{VehicleId}}",
                        AlertDescriptionFormat = "Vehicle multimedia new file detected.\nMessage: {{Message}}\nVehicleID: {{VehicleId}}\nFilename: {{Filename}}"
                    }
                };
                vehicleMultimediaNewFile.CustomDetails.Add("VehicleId", "VehicleId");
                vehicleMultimediaNewFile.CustomDetails.Add("SubType", "SubType");
                vehicleMultimediaNewFile.CustomDetails.Add("Filename", "Filename");
                vehicleMultimediaNewFile.CustomDetails.Add("Message", "Message");

                vehicleMultimediaNewFile.EntityMappings.Add(vehicleIdEntityMappings);
                vehicleMultimediaNewFile.EntityMappings.Add(fileSystemEntityMappings);
                DefaultRules.Add("e8794ea7-8648-4fb8-88ba-82197d52461b", vehicleMultimediaNewFile);
            }

            {
                SecurityInsightsScheduledAlertRule vehicleMultimediaPeripheralDetected = new()
                {
                    Query = $@"let Table = {tableName}_CL;
let NewPeripheralEvents = Table
    | where type_s == ""Security""
    | where subType_s == ""Peripheral""
    | extend Json = parse_json(jsonAsString_s)
    | extend Message = tostring(Json[""message""])
    | extend Action = tostring(Json[""action""])
    | where isempty(Action)
    | summarize arg_max(TimeGenerated, *) by VehicleId = entityId_s, Message, Action, Type = type_s, SubType = subType_s
    | sort by TimeGenerated desc 
    | extend Payload = Json[""jsonAsString""]
    | mv-expand PeripheralName = parse_json(tostring(Payload))[""name""]
    | project  TimeGenerated, VehicleId, Type, SubType, Message, PeripheralName;
NewPeripheralEvents",
                    QueryFrequency = TimeSpan.Parse("01:00:00"),
                    QueryPeriod = TimeSpan.Parse("01:00:00"),
                    DisplayName = $"Vehicle multimedia new peripheral detected",
                    Description = "Vehicle multimedia new peripheral detected",
                    Severity = SecurityInsightsAlertSeverity.Medium,
                    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                    TriggerThreshold = 0,
                    IncidentConfiguration = incidentConfiguration,
                    EventGroupingAggregationKind = EventGroupingAggregationKind.AlertPerResult,
                    IsEnabled = true,
                    SuppressionDuration = TimeSpan.Parse("01:00:00"),
                    IsSuppressionEnabled = false,
                    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                    {
                        AlertDisplayNameFormat = "Vehicle multimedia new peripheral detected, VehicleId={{VehicleId}}",
                        AlertDescriptionFormat = "Vehicle multimedia new peripheral detected.\nMessage: {{Message}}\nVehicleID: {{VehicleId}}\nPeripheralName: {{PeripheralName}}"
                    }
                };
                vehicleMultimediaPeripheralDetected.CustomDetails.Add("VehicleId", "VehicleId");
                vehicleMultimediaPeripheralDetected.CustomDetails.Add("SubType", "SubType");
                vehicleMultimediaPeripheralDetected.CustomDetails.Add("PeripheralName", "PeripheralName");
                vehicleMultimediaPeripheralDetected.CustomDetails.Add("Message", "Message");

                vehicleMultimediaPeripheralDetected.EntityMappings.Add(vehicleIdEntityMappings);
                vehicleMultimediaPeripheralDetected.EntityMappings.Add(peripheralEntityMappings);
                DefaultRules.Add("f8794ea7-8648-4fb8-88ba-82197d52461b", vehicleMultimediaPeripheralDetected);
            }

            {

                SecurityInsightsGroupingConfiguration vehicleMultimediaExploitGroupingConfiguration = new(
                    true,
                    false,
                    TimeSpan.Parse("05:00:00"),
                    SecurityInsightsGroupingMatchingMethod.Selected);
                vehicleMultimediaExploitGroupingConfiguration.GroupByCustomDetails.Add("VehicleId");
                SecurityInsightsIncidentConfiguration vehicleMultimediaExploitIncidentConfiguration = new(true)
                {
                    GroupingConfiguration = vehicleMultimediaExploitGroupingConfiguration
                };

                SecurityInsightsScheduledAlertRule vehicleMultimediaExploit = new()
                {
                    Query = $@"let Table = {tableName}_CL;
let NewPeripheralEvents = Table
    | where type_s == ""Security""
    | where subType_s == ""Peripheral""
    | extend Json = parse_json(jsonAsString_s)
    | extend Message = tostring(Json[""message""])
    | extend Action = tostring(Json[""action""])
    | summarize arg_max(TimeGenerated, *) by VehicleId = entityId_s, Message, Action, Type = type_s, SubType = subType_s
    | sort by TimeGenerated desc 
    | extend Payload = Json[""jsonAsString""]
    | mv-expand PeripheralName = parse_json(tostring(Payload))[""name""]
    | project  TimeGenerated, VehicleId, Type, SubType, Message, PeripheralName, Action;
let NewFileEvents = Table
    | where type_s == ""Security""
    | where subType_s == ""FileSystem""
    | extend Json = parse_json(jsonAsString_s)
    | extend Message = tostring(Json[""message""])
    | extend Action = tostring(Json[""action""])
    | sort by TimeGenerated desc 
    | extend Payload = Json[""jsonAsString""]
    | mv-expand Filename = parse_json(tostring(Payload))[""files""]
    | summarize arg_max(TimeGenerated, *) by VehicleId = entityId_s, Message, Action, Type = type_s, SubType = subType_s, tostring(Filename)
    | project  TimeGenerated, VehicleId, Type, SubType, Message, Filename, Action;
let MultimediaExploit = NewFileEvents
    | union NewPeripheralEvents
    | sort by TimeGenerated;
MultimediaExploit",
                    QueryFrequency = TimeSpan.Parse("00:05:00"),
                    QueryPeriod = TimeSpan.Parse("00:05:00"),
                    DisplayName = $"Vehicle multimedia exploit",
                    Description = "Vehicle multimedia exploit auto mitigated by Microsoft Defender.",
                    Severity = SecurityInsightsAlertSeverity.High,
                    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
                    TriggerThreshold = 0,
                    IncidentConfiguration = vehicleMultimediaExploitIncidentConfiguration,
                    EventGroupingAggregationKind = EventGroupingAggregationKind.AlertPerResult,
                    IsEnabled = true,
                    SuppressionDuration = TimeSpan.Parse("00:05:00"),
                    IsSuppressionEnabled = false,
                    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
                    {
                        AlertDisplayNameFormat = "Vehicle multimedia exploit auto mitigated, VehicleId={{VehicleId}}",
                        AlertDescriptionFormat = "Vehicle multimedia exploit auto mitigated by Microsoft Defender.\nMessage: {{Message}}\nVehicleID: {{VehicleId}}\nAction: {{Action}}"
                    }
                };
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.Collection);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.CommandAndControl);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.Execution);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.Impact);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.InitialAccess);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.LateralMovement);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.Persistence);
                vehicleMultimediaExploit.Tactics.Add(SecurityInsightsAttackTactic.InhibitResponseFunction);
                vehicleMultimediaExploit.Techniques.Add("T1025");
                vehicleMultimediaExploit.Techniques.Add("T1071");
                vehicleMultimediaExploit.Techniques.Add("T0885");
                vehicleMultimediaExploit.Techniques.Add("T1059");
                vehicleMultimediaExploit.Techniques.Add("T1569");
                vehicleMultimediaExploit.Techniques.Add("T0880");
                vehicleMultimediaExploit.Techniques.Add("T1190");
                vehicleMultimediaExploit.Techniques.Add("T1570");
                vehicleMultimediaExploit.Techniques.Add("T0843");
                vehicleMultimediaExploit.Techniques.Add("T0867");
                vehicleMultimediaExploit.Techniques.Add("T0889");
                vehicleMultimediaExploit.Techniques.Add("T0816");
                vehicleMultimediaExploit.CustomDetails.Add("VehicleId", "VehicleId");
                vehicleMultimediaExploit.CustomDetails.Add("SubType", "SubType");
                vehicleMultimediaExploit.CustomDetails.Add("Action", "Action");
                vehicleMultimediaExploit.CustomDetails.Add("Message", "Message");

                vehicleMultimediaExploit.EntityMappings.Add(vehicleIdEntityMappings);
                vehicleMultimediaExploit.EntityMappings.Add(fileSystemEntityMappings);
                vehicleMultimediaExploit.EntityMappings.Add(peripheralEntityMappings);
                vehicleMultimediaExploit.EntityMappings.Add(subtypeEntityMappings);
                DefaultRules.Add("d8794ea7-8648-4fb8-88ba-82197d52461b", vehicleMultimediaExploit);
            }


            //SecurityInsightsScheduledAlertRule securityInsightsScheduledAlertRule = new SecurityInsightsScheduledAlertRule()
            //{
            //    Query = $@"{tableName}_CL 
            //                | where TimeGenerated > ago(1d)
            //                | extend Vehicle = parse_json(vehicleJsonAsString_s)
            //                | extend applications = Vehicle['parts']['Multimedia']['applications']
            //                | where isnotempty(applications)| mv-expand Application = applications
            //                | project Name = Application['name'], Version = Application['version'], Enabled = Application['enabled'], vin_s
            //                | where Name in ('Youtube') and Version < 3.0
            //                | distinct tostring(Name), tostring(Version)",
            //    QueryFrequency = TimeSpan.Parse("00:05:00"),
            //    QueryPeriod = TimeSpan.Parse("05:00:00"),
            //    DisplayName = $"tre Applications Vulnerabilities",
            //    Description = "",
            //    Severity = SecurityInsightsAlertSeverity.High,
            //    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
            //    TriggerThreshold = 0,
            //    EventGroupingAggregationKind = EventGroupingAggregationKind.SingleAlert,
            //    IsEnabled = true,
            //    SuppressionDuration = TimeSpan.Parse("05:00:00"),
            //    IsSuppressionEnabled = false,
            //    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
            //    {
            //        AlertDisplayNameFormat = "QWE Update application {{Name}}",
            //        AlertDescriptionFormat = "Alert generated by Analytics scheduled rule"
            //    }
            //};
            //securityInsightsScheduledAlertRule.CustomDetails.Add("Name", "Name");
            //DefaultRules.Add("b8794ea7-8648-4fb8-88ba-82197d52461b", securityInsightsScheduledAlertRule);

            //SecurityInsightsScheduledAlertRule a = new SecurityInsightsScheduledAlertRule()
            //{
            //    Query = $@"demoURLMonitorY_CL 
            //                | where TimeGenerated > ago(1d)
            //                | extend Vehicle = parse_json(vehicleJsonAsString_s)
            //                | extend applications = Vehicle['parts']['Multimedia']['applications']
            //                | where isnotempty(applications)| mv-expand Application = applications
            //                | project Name = Application['name'], Version = Application['version'], Enabled = Application['enabled'], vin_s
            //                | where Name in ('Youtube') and Version < 3.0
            //                | distinct tostring(Name), tostring(Version)",
            //    QueryFrequency = TimeSpan.Parse("00:05:00"),
            //    QueryPeriod = TimeSpan.Parse("05:00:00"),
            //    DisplayName = $"tre Applications Vulnerabilities",
            //    Description = "",
            //    Severity = SecurityInsightsAlertSeverity.High,
            //    TriggerOperator = SecurityInsightsAlertRuleTriggerOperator.GreaterThan,
            //    TriggerThreshold = 0,
            //    EventGroupingAggregationKind = EventGroupingAggregationKind.SingleAlert,
            //    IsEnabled = true,
            //    SuppressionDuration = TimeSpan.Parse("05:00:00"),
            //    IsSuppressionEnabled = false,
            //    AlertDetailsOverride = new SecurityInsightsAlertDetailsOverride()
            //    {
            //        AlertDisplayNameFormat = "QWE Update application {{Name}}",
            //        AlertDescriptionFormat = "Alert generated by Analytics scheduled rule"
            //    }
            //};
            //a.CustomDetails.Add("Name", "Name");
            //DefaultRules.Add("a8794ea7-8648-4fb8-88ba-82197d52461b", a);


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
