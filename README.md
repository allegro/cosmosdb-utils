# Allegro CosmosDb utils

This repo contains a collection of useful Azure CosmosDb SDK v3 extensions and utilities, developed as part of [Allegro Pay](https://allegropay.pl/) product.

## Utilities

- [Allegro.CosmosDb.BatchUtilities](src/Allegro.CosmosDb.BatchUtilities/README.md) - utilities for performing batch operations in Azure CosmosDb, such as rate limiting and autoscaling.  
  [![NuGet](https://img.shields.io/nuget/v/Allegro.CosmosDb.BatchUtilities.svg)](https://nuget.org/packages/Allegro.CosmosDb.BatchUtilities) [![Build Status](https://github.com/allegro/cosmosdb-utils/actions/workflows/Allegro.CosmosDb.BatchUtilities.ci.yml/badge.svg?branch=main)](https://github.com/allegro/cosmosdb-utils/actions/workflows/Allegro.CosmosDb.BatchUtilities.ci.yml?query=branch%3Amain)
- [Allegro.CosmosDb.ConsistencyLevelUtilities](src/Allegro.CosmosDb.ConsistencyLevelUtilities/README.md) - utilities helpful in handling CosmosDb Consistency Levels.  
  [![NuGet](https://img.shields.io/nuget/v/Allegro.CosmosDb.ConsistencyLevelUtilities.svg)](https://nuget.org/packages/Allegro.CosmosDb.ConsistencyLevelUtilities) [![Build Status](https://github.com/allegro/cosmosdb-utils/actions/workflows/Allegro.CosmosDb.ConsistencyLevelUtilities.ci.yml/badge.svg?branch=main)](https://github.com/allegro/cosmosdb-utils/actions/workflows/Allegro.CosmosDb.ConsistencyLevelUtilities.ci.yml?query=branch%3Amain)
- [Allegro.CosmosDb.Migrator](src/Allegro.CosmosDb.Migrator/README.md) - utilities for migrating data online between containers.
## License

Copyright 2021 Allegro Group

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.