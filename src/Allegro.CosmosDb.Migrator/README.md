# Introduction 
This project contains fully working service that enables real time as is migration between two CosmosDb containers.

It contains:
- migration api
- migration statistics api
- verification api

# Getting Started

## CosmosDb Emulator
Unfortunately to CosmosDb is not easily available as a standalone solution. We are able to use some Emulators for development purposes.

To learn more about emulator please refer to: https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=powershell%2Cssl-netstd21#developing-with-the-emulator

There are some docker images for Windows containers and linux (preview). So for development purposes please install emulator or use Azure CosmosDb account.

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

# Road map

- encrypt connection string stored in migration container
- RU consumption limiter must be global per collection as for now it is per partition key range so we need to specify max level divided by count of partition range
- Validation service is now comparing documents based on unique id of document and not able to handle cases where id is not unique but pair of partition key + id is identity of record


If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)