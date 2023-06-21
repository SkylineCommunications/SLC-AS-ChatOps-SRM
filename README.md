# SLC-AS-ChatOps-SRM

This repository contains an automation script solution with scripts that can be used to retrieve ongoing bookings from your DataMiner system using the DataMiner Teams bot.

The following scrips are currently available:

[Booking Info](#Booking Info)

## Pre-requisites

Kindly ensure that your DataMiner system and your Microsoft Teams adhere to the pre-requisites described in [DM Docs](https://docs.dataminer.services/user-guide/Cloud_Platform/TeamsBot/Microsoft_Teams_Chat_Integration.html#server-side-prerequisites).

## Booking Info

Automation script that returns the ongoing bookings from the connected DataMiner system. More specifically the return value will contain the total amount of ongoing bookings including a table which will show by default the ongoing bookings whose end is nearest (default filter maximum 10 bookings).

![Booking Info example](/Documentation/OngoingBookingsChatOpsCommand.png)