# Tenant Reference Audit

## Overview
This note captures the remaining references to the legacy tenant concept that are still present after the migration toward a global schema.

## Findings
- **API integration tests** still assert the absence of tenant fields to ensure the new contract is respected. These tests reference the field names in negative assertions.
- **n8n workflow templates** under `infra/init/n8n/` continue to read and forward `tenantId` values when preparing webhook payloads and logging request metadata.

## Next Steps
- Decide whether the n8n automation should be updated to drop tenant-specific parameters or if those workflows are deprecated.
- Keep the integration tests up to date if the API contract changes again.
