# TMS API Versioning Policy

## Purpose

This document defines how the TMS API is versioned and how we decide whether a change requires a new API version. The goal is to protect existing clients while allowing the API to evolve safely.

## Breaking Changes

A breaking change is any change that can cause an existing client application to fail or behave differently without modification.

The following changes require a new API version:

- Removing an existing response field.
- Renaming an existing request or response field.
- Changing the data type or meaning of an existing field.
- Changing an existing HTTP status code behavior.
  - Example: changing a successful response from `200 OK` to `404 Not Found`.
- Tightening validation rules that reject previously accepted requests.
  - Example: making an optional field required.
- Changing the default behavior of an endpoint.
  - Example: changing the default sorting order or pagination behavior.
- Removing or changing the meaning of an existing query parameter.

Breaking changes are introduced through a new API version (for example, V2).

## Additive Changes

The following changes are considered non-breaking and can be released without creating a new version:

- Adding a new optional response field.
- Adding a new endpoint.
- Adding a new optional query parameter.
- Adding new enum values when clients are designed to ignore unknown values.
- Adding additional links or metadata to responses.

Clients should be written to tolerate additional fields.

## Version Lifetime and Sunset Policy

When a new API version is released, the previous version continues to operate for a minimum of six months.

For example:

- V1 remains available after V2 is released.
- Teams have at least six months to migrate.
- Rural training centres using quarterly maintenance schedules have enough time to plan and complete migration.

The shutdown date of an older version is announced before removal.

## Communication Process

From the first release of V2, the API will communicate version lifecycle information through:

- Deprecation headers indicating that an API version will be retired.
- Sunset headers showing the planned shutdown date.
- Link headers pointing clients to migration documentation.

In addition:

- A CHANGELOG entry will document all API changes.
- An email notification will be sent to every team that owns an API key.
- A calendar invitation will be created for the V1 shutdown date.

## Version Skipping

API versions do not require sequential migration.

A client may migrate directly from V1 to V3 if V3 is available.

Clients are not required to upgrade through every intermediate version.


