/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace Show_Resource_Utilization_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AdaptiveCards;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.Time;

	/// <summary>
	/// DataMiner Script Class.
	/// </summary>
	public class Script
	{
		private static int durationInDays = 7;
		private DateTime start;
		private DateTime end;

		/// <summary>
		/// The Script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(Engine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (Exception ex)
			{
				ReturnErrorMessage(engine, ex.Message);
			}
		}

		private void RunSafe(Engine engine)
		{
			end = DateTime.UtcNow;
			start = end.AddDays(-durationInDays);

			var resourcePoolName = engine.GetScriptParam("Resource Pool Name").Value;

			ResourceManagerHelper rmHelper = new ResourceManagerHelper();
			rmHelper.RequestResponseEvent += (sender, e) => e.responseMessage = engine.SendSLNetSingleResponseMessage(e.requestMessage);

			List<Resource> resources = GetResourcesByPoolName(rmHelper, resourcePoolName);
			if (resources.Count == 0)
			{
				ReturnErrorMessage(engine, "No resources available.");

				return;
			}

			var resourceMapping = resources.ToDictionary(r => r.ID, r => new ResourceInfo { Resource = r, Duration = 0 });
			foreach (var reservation in GetReservationsByResources(rmHelper, resourceMapping.Keys.ToList()))
			{
				reservation.ResourcesInReservationInstance
					.Where(resource => resourceMapping.Keys.Contains(resource.GUID))
					.ForEach(resource => UpdateTimeRange(resource.GUID, reservation.TimeRange, resourceMapping));
			}

			ReturnResultMessage(engine, resourceMapping.Values.ToList(), false);
		}

		private List<Resource> GetResourcesByPoolName(ResourceManagerHelper rmHelper, string name)
		{
			ResourcePool resourcePool = rmHelper.GetResourcePools(new ResourcePool { Name = name }).FirstOrDefault();
			if (resourcePool == null)
			{
				throw new ArgumentException($"No resource pool found with name '{name}'.");
			}

			return rmHelper.GetResources(ResourceExposers.PoolGUIDs.Contains(resourcePool.GUID)).ToList();
		}

		private List<ReservationInstance> GetReservationsByResources(ResourceManagerHelper rmHelper, List<Guid> resourceIds)
		{
			var resourceFilter = new ORFilterElement<ReservationInstance>(resourceIds.Select(id => ReservationInstanceExposers.ResourceIDsInReservationInstance.Contains(id)).ToArray());

			var timeRangeUtc = new TimeRangeUtc(start, end);

			var filter = resourceFilter
				.AND(ReservationInstanceExposers.Start.LessThanOrEqual(timeRangeUtc.Stop)
				.AND(ReservationInstanceExposers.End.GreaterThanOrEqual(timeRangeUtc.Start)));

			return rmHelper.GetReservationInstances(filter).ToList();
		}

		private void UpdateTimeRange(Guid resourceId, TimeRangeUtc timeRange, Dictionary<Guid, ResourceInfo> dic)
		{
			DateTime startToUse = (timeRange.Start < start) ? start : timeRange.Start;
			DateTime endToUse = (timeRange.Stop > end) ? end : timeRange.Stop;

			dic[resourceId].Duration += (endToUse - startToUse).TotalMinutes;
		}

		private void ReturnResultMessage(Engine engine, List<ResourceInfo> resources, bool skipNotUsed)
		{
			var table = new AdaptiveTable
			{
				Type = "Table",
				FirstRowAsHeaders = true,
				Columns = new List<AdaptiveTableColumnDefinition>
				{
					new AdaptiveTableColumnDefinition
					{
						Width = 250,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 100,
					},
				},
				Rows = new List<AdaptiveTableRow>
				{
					new AdaptiveTableRow
					{
						Type = "TableRow",
						Cells = new List<AdaptiveTableCell>
						{
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Resource")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Utilization")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
						},
					},
				},
			};

			Double maxDuration = 24 * 60 * durationInDays;

			foreach (var resourceInfo in resources)
			{
				if (skipNotUsed && resourceInfo.Duration.Equals(0))
				{
					continue;
				}

				var row = new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(resourceInfo.Resource.Name)
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock($"{resourceInfo.Duration / maxDuration * 100:N2}%")
								{
									Type = "TextBlock",
								},
							},
						},
					},
				};

				table.Rows.Add(row);
			}

			var adaptiveCardBody = new List<AdaptiveElement>();
			adaptiveCardBody.Add(table);

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private void ReturnErrorMessage(Engine engine, string message)
		{
			var adaptiveCardBody = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock(message)
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Default,
				},
			};

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}
	}

	internal class ResourceInfo
	{
		public Resource Resource { get; set; }

		public double Duration { get; set; }
	}
}