# SLC-AS-ChatOps-SRM

This repository contains an automation script solution with scripts that can be used to retrieve ongoing bookings from your DataMiner system using the DataMiner Teams bot.

The following scrips are currently available:

- [Booking Info](#booking-info)
- [Show Resource Utilization](#show-resource-utilization)

## Pre-requisites

Kindly ensure that your DataMiner system and your Microsoft Teams adhere to the pre-requisites described in [DM Docs](https://docs.dataminer.services/user-guide/Cloud_Platform/TeamsBot/Microsoft_Teams_Chat_Integration.html#server-side-prerequisites).

> **Note**
> There is a known issue with the newtonsoft.json reference. After uploading the script to the DataMiner system make sure to update such reference to version 11.0.2.

### Installation

Deploy the automation script from this repo to your DMS.
   - This can be done by cloning the repo and using DIS to publish in your DMS or going to the Catalog and deploy from there or use the DataMiner CI/CD Automation GitHub Action.

## Booking Info

Automation script that returns the ongoing bookings from the connected DataMiner system when running the command below. More specifically the return value will contain the total amount of ongoing bookings including a table which will show by default the ongoing bookings whose end is nearest (default filter maximum 10 bookings).

```
run Show Ongoing Bookings
```

![Booking Info example](/Documentation/OngoingBookingsChatOpsCommand.png)

## Show Resource Utilization

Automation script that returns the utilization (%) of each resource in the specified resource pool for the last week.

![Resource Utilization](/Documentation/ResourceUtilization.png)
