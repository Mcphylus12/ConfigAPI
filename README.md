## TODO

- Better error handling for schema validation failures
- Use stores for schemas

## FE
- Listing configs
- working with Schemas

## Phase 2
- *STRETCH* Define preset "expected" configurations and populate open api spec for them wiith correct request/response data
- From "expected" config have frontend create a easier to use UI 
- configKey dot seperation repped as breadcrumbs

## Overview

ConfigApi is a standalone service that at it's core is a string -> JSON key value store. It has one addition feature which is where all the value lives. keys are dot separated strings that allows inheritance when retrieving the JSON. EG

1. set `service` to
```JSON
{
   "someCommonServiceConfig": "aValue"
}
```
2.set `service.Testservice` to
```JSON
{
   "testServiceSpecialConfig": 99
}
```

3. get config for `service.Testservice`

the result will be 
```JSON
{
   "testServiceSpecialConfig": 99,
   "someCommonServiceConfig": "aValue"
}
```

The service starts at the root and overlays more specific JSON as it checks if config is set for more specific keys.

The aim is single solution that can handle the back office configuration for a whole system. 
