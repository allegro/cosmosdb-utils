# Introduction 
This project contains fully working service that enables real time as is migration between two CosmosDb containers.

It contains:
- migration api
- migration statistics api

# Getting Started

To run this tool locally you need to set connection string in [appsettings.json](./src/Allegro.CosmosDb.Migrator.Api/appsettings.json) in section `cosmos` 

```json
"cosmos": {
    "connectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "database": "Migrator",
    "requestTimeoutInSeconds" : 10
  },
```

By default connection string of emulator is set, as it seems it is constant default value.

Than you need to build and run `Allegro.CosmosDb.Migrator.Api` project.

To easier play with api you can use test scripts [CosmosMigrator.rest](CosmosMigrator.rest) via [Visual Studio Code Rest Client Extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client). 

## CosmosDb Emulator
Unfortunately to CosmosDb is not easily available as a standalone solution. We are able to use some Emulators for development purposes.

To learn more about emulator please refer to: https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=powershell%2Cssl-netstd21#developing-with-the-emulator

There are some docker images for Windows containers and linux (preview). So for development purposes please install emulator or use Azure CosmosDb account.

# Road map

- encrypt connection string stored in migration container
- RU consumption limiter must be global per collection as for now it is per partition key range so we need to specify max level divided by count of partition range
- verification api for some basic checks
- use [cosmos utilities](https://github.com/allegro/cosmosdb-utils#utilities) instead of local approach 