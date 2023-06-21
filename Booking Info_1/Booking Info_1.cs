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

namespace Booking_Info_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using AdaptiveCards;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		private ServiceManagerHelper serviceManager;
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				var resourceManagerHelper = new ResourceManagerHelper();
				resourceManagerHelper.RequestResponseEvent += (o, e) => e.responseMessage = Engine.SLNet.SendSingleResponseMessage(e.requestMessage);

				serviceManager = new ServiceManagerHelper();
				serviceManager.RequestResponseEvent += (o, e) => e.responseMessage = Engine.SLNet.SendSingleResponseMessage(e.requestMessage);

				var ongoingReservations = resourceManagerHelper.GetReservationInstances(ReservationInstanceExposers.Status.Equal((int)ReservationStatus.Ongoing)).ToList();

				var card = GetAdaptiveCard(ongoingReservations);

				engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(card));
			}
			catch (Exception ex)
			{
				engine.Log(ex.ToString());
				engine.ExitFail(ex.Message);
			}
		}

		private List<AdaptiveElement> GetAdaptiveCard(List<ReservationInstance> reservations, int maxAmountToShow = 10)
		{
			var container = new AdaptiveContainer
			{
				Spacing = AdaptiveSpacing.Medium,
			};

			container.Items.Add(new AdaptiveTextBlock
			{
				Text = $"There are {reservations.Count} ongoing bookings.",
			});

			if (reservations.Count > maxAmountToShow)
			{
				container.Items.Add(new AdaptiveTextBlock
				{
					Text = $"Below are the {maxAmountToShow} bookings whose end is nearest:",
				});
			}

			var reservationNameColumnItems = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock
				{
					Text = "Booking Name",
					Style = AdaptiveTextBlockStyle.Heading,
					Weight = AdaptiveTextWeight.Bolder,
				},
			};

			var reservationStartColumnItems = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock
				{
					Text = "Booking Start",
					Style = AdaptiveTextBlockStyle.Heading,
					Weight = AdaptiveTextWeight.Bolder,
				},
			};

			var reservationEndColumnItems = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock
				{
					Text = "Booking End",
					Style = AdaptiveTextBlockStyle.Heading,
					Weight = AdaptiveTextWeight.Bolder,
				},
			};

			var reservationDefinitionColumnItems = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock
				{
					Text = "Service Definition",
					Style = AdaptiveTextBlockStyle.Heading,
					Weight = AdaptiveTextWeight.Bolder,
				},
			};

			bool first = true;
			foreach (var reservation in reservations.OrderBy(r => r.End).Take(maxAmountToShow))
			{
				reservationNameColumnItems.Add(new AdaptiveTextBlock
				{
					Text = reservation.Name,
					Separator = first,
					Wrap = true,
				});

				reservationStartColumnItems.Add(new AdaptiveTextBlock
				{
					Text = reservation.Start.ToString(),
					Separator = first,
					Wrap = true,
				});

				reservationEndColumnItems.Add(new AdaptiveTextBlock
				{
					Text = reservation.End.ToString(),
					Separator = first,
					Wrap = true,
				});

				reservationDefinitionColumnItems.Add(new AdaptiveTextBlock
				{
					Text = reservation is ServiceReservationInstance serviceReservation ? serviceManager.GetServiceDefinition(serviceReservation.ServiceDefinitionID)?.Name ?? "N/A" : "N/A",
					Separator = first,
					Wrap = true,
				});

				first = false;
			}

			container.Items.Add(new AdaptiveColumnSet
			{
				new AdaptiveColumn
				{
					Width = "auto",
					Items = reservationNameColumnItems,
				},
				new AdaptiveColumn
				{
					Width = "auto",
					Items = reservationStartColumnItems,
				},
				new AdaptiveColumn
				{
					Width = "auto",
					Items = reservationEndColumnItems,
				},
				new AdaptiveColumn
				{
					Width = "auto",
					Items = reservationDefinitionColumnItems,
				},
			});

			return new List<AdaptiveElement>(container);
		}
	}
}