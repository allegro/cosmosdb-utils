# Introduction 
This project contains fully working service that enables real time as is migration between two CosmosDb containers.

It contains:
- migration api
- migration statistics api

# Getting Started

## CosmosDb Emulator
Unfortunately to CosmosDb is not easily available as a standalone solution. We are able to use some Emulators for development purposes.

To learn more about emulator please refer to: https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=powershell%2Cssl-netstd21#developing-with-the-emulator

There are some docker images for Windows containers and linux (preview). So for development purposes please install emulator or use Azure CosmosDb account.

# Road map

- encrypt connection string stored in migration container
- RU consumption limiter must be global per collection as for now it is per partition key range so we need to specify max level divided by count of partition range
- verification api for some basic checks