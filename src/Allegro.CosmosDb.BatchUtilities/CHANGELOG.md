# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2022-09-01

### Breaking change

* Support for multiple CosmosClients 
* Because of that multiple small breaking changes were required:
  * Change `ICosmosClientProvider` to `ICosmosBatchClientProvider` with only one method `public CosmosClient GetBatchClient(string clientName)`
  * Change `AddCosmosBatchUtilitiesFromConfiguration` to `AddCosmosBatchClientFromConfiguration` which now require building function for CosmosClient and clientName
  * Change both `AddCosmosBatchUtilities` to `AddCosmosBatchClient` which now require building function for CosmosClient and clientName
  * Make `CosmosAutoScalerFactory` internal, expose ICosmosAutoScalerFactoryProvider instead

## [1.0.3] - 2022-05-17

### Changed

* Extend `IServiceCollection` extensions to make better use of DI

## [1.0.2] - 2022-04-13

### Changed

* Add `CosmosAutoScalerMetricsCalculated` event to `CosmosAutoScaler` to enable metrics collection

## [1.0.1] - 2022-03-04

### Changed

* Embed library's README instead of repository's README

## [1.0.0] - 2022-03-04

### Added

* Initiated AllegroPay.CosmosDb.BatchUtilities project
