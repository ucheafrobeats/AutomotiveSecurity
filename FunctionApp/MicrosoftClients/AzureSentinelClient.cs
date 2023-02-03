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
            DefaultRules.Add("b8794ea7-8648-4fb8-88ba-82197d52461b", securityInsightsScheduledAlertRule);
        }
    }
}
