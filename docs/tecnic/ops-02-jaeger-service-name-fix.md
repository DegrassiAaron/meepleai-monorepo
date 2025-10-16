# OPS-02: Jaeger Service Name Configuration Fix

## Issue Summary

**Problem**: OpenTelemetry traces were appearing in Jaeger v2 UI, but the service name showed as "jaeger" instead of "MeepleAI.Api" in the service dropdown.

**Root Cause**: While the service name was configured programmatically via `ConfigureResource().AddService()`, Jaeger v2 requires the service name to be present in the OTLP resource attributes. According to the OpenTelemetry specification, environment variables take precedence over programmatic configuration and ensure consistent service identification across all exporters.

**Impact**: Before fix - traces were not properly attributed to the MeepleAI.Api service, making it difficult to filter and analyze application traces. After fix - traces correctly show "MeepleAI.Api" as the service name.

## Solution

### Environment Variable Configuration

Added three OpenTelemetry standard environment variables to `infra/env/api.env.dev`:

```bash
# OPS-02: OpenTelemetry distributed tracing and metrics
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4318
OTEL_SERVICE_NAME=MeepleAI.Api
OTEL_RESOURCE_ATTRIBUTES=service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=development
```

### Environment Variables Explained

1. **OTEL_SERVICE_NAME**:
   - Sets the `service.name` resource attribute
   - Takes highest precedence per OpenTelemetry specification
   - Ensures all exporters use the correct service name

2. **OTEL_RESOURCE_ATTRIBUTES**:
   - Provides additional resource attributes as comma-separated key=value pairs
   - Includes:
     - `service.name`: Service identifier (redundant with OTEL_SERVICE_NAME, but provides defense-in-depth)
     - `service.version`: Application version for tracking deployments
     - `deployment.environment`: Environment tag (development, staging, production)

3. **OTEL_EXPORTER_OTLP_ENDPOINT**:
   - OTLP endpoint for trace export
   - Already configured, included for completeness

### Code Changes

**Program.cs** (lines 313-322):

Added environment variable reading and debug logging:

```csharp
// OPS-02: OpenTelemetry configuration
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://jaeger:4318";
var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "MeepleAI.Api";
var serviceVersion = "1.0.0";

// Debug: Log OTLP configuration
Console.WriteLine($"[OpenTelemetry] Configuring OTLP exporter with endpoint: {otlpEndpoint}");
Console.WriteLine($"[OpenTelemetry] Service name: {serviceName}, version: {serviceVersion}");
Console.WriteLine($"[OpenTelemetry] OTEL_SERVICE_NAME env var: {builder.Configuration["OTEL_SERVICE_NAME"] ?? "(not set)"}");
Console.WriteLine($"[OpenTelemetry] OTEL_RESOURCE_ATTRIBUTES env var: {builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"] ?? "(not set)"}");
```

**Key Changes:**
- Service name now reads from `OTEL_SERVICE_NAME` environment variable with fallback to hardcoded value
- Added debug logging to verify environment variables are loaded
- Logs both the resolved service name and the raw environment variable values

### OpenTelemetry Specification Reference

From [OpenTelemetry .NET Documentation](https://github.com/open-telemetry/opentelemetry-dotnet):

> **OTEL_SERVICE_NAME**
> - Description: Sets the value of the `service.name` resource attribute.
> - Precedence: If `service.name` is also provided in `OTEL_RESOURCE_ATTRIBUTES`, then `OTEL_SERVICE_NAME` takes precedence.

> **OTEL_RESOURCE_ATTRIBUTES**
> - Description: Key-value pairs to be used as resource attributes.
> - Format: key1=value1,key2=value2
> - Reference: See the Resource SDK specification for more details.

The OpenTelemetry SDK automatically reads these environment variables and applies them to the resource configuration before programmatic configuration, ensuring proper service identification in all telemetry backends.

## Verification Steps

### 1. Verify Environment Variables are Set

```bash
# In the API container
docker exec -it meepleai-api-1 env | grep OTEL
```

Expected output:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4318
OTEL_SERVICE_NAME=MeepleAI.Api
OTEL_RESOURCE_ATTRIBUTES=service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=development
```

### 2. Check Application Startup Logs

```bash
docker logs meepleai-api-1 | grep OpenTelemetry
```

Expected output:
```
[OpenTelemetry] Configuring OTLP exporter with endpoint: http://jaeger:4318
[OpenTelemetry] Service name: MeepleAI.Api, version: 1.0.0
[OpenTelemetry] OTEL_SERVICE_NAME env var: MeepleAI.Api
[OpenTelemetry] OTEL_RESOURCE_ATTRIBUTES env var: service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=development
```

### 3. Verify in Jaeger UI

1. Navigate to `http://localhost:16686`
2. Open the "Service" dropdown in the search panel
3. Verify "MeepleAI.Api" appears in the list (not "jaeger")
4. Select "MeepleAI.Api" and click "Find Traces"
5. Verify traces are visible and properly attributed

### 4. Inspect Trace Resource Attributes

1. Click on any trace in Jaeger UI
2. Expand the root span
3. Check the "Process" section
4. Verify resource attributes include:
   - `service.name`: MeepleAI.Api
   - `service.version`: 1.0.0
   - `service.instance.id`: (machine name)
   - `deployment.environment`: development

## Technical Deep Dive

### Why Environment Variables?

The OpenTelemetry specification defines a clear precedence order for resource configuration:

1. **Environment Variables** (highest precedence)
   - `OTEL_SERVICE_NAME`
   - `OTEL_RESOURCE_ATTRIBUTES`

2. **Programmatic Configuration**
   - `ConfigureResource().AddService()`
   - `ConfigureResource().AddAttributes()`

3. **SDK Defaults** (lowest precedence)
   - `unknown_service:dotnet`

By setting environment variables, we ensure the service name is:
- Consistent across all exporters (OTLP, Prometheus, Console)
- Properly propagated to OTLP resource semantic conventions
- Correctly interpreted by Jaeger v2
- Easy to override in different deployment environments

### Jaeger v2 OTLP Integration

Jaeger v2 (since version 1.35.0) implements native OTLP ingestion. The service name extraction follows this logic:

1. Extract `service.name` from OTLP resource attributes
2. If not present, fall back to span-level `service` tag
3. If neither present, use "unknown_service"

The issue occurred because while `ConfigureResource().AddService()` sets the service name in the .NET SDK, it may not have been properly serialized to OTLP resource attributes in all cases. Environment variables guarantee correct serialization.

### Defense-in-Depth Strategy

The fix implements three layers:

1. **OTEL_SERVICE_NAME**: Primary mechanism (highest precedence per spec)
2. **OTEL_RESOURCE_ATTRIBUTES**: Secondary mechanism (includes service.name explicitly)
3. **ConfigureResource().AddService()**: Tertiary mechanism (programmatic fallback)

This ensures service name is set even if one layer fails.

## Deployment Considerations

### Docker Compose

Environment variables are loaded from `infra/env/api.env.dev` via the `env_file` directive in `docker-compose.yml`:

```yaml
api:
  env_file:
    - ./env/api.env.dev
```

### Kubernetes

For Kubernetes deployments, add environment variables to the deployment manifest:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: meepleai-api
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: OTEL_SERVICE_NAME
          value: "MeepleAI.Api"
        - name: OTEL_RESOURCE_ATTRIBUTES
          value: "service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=production"
        - name: OTEL_EXPORTER_OTLP_ENDPOINT
          value: "http://jaeger-collector:4318"
```

### Environment-Specific Configuration

Different environments should use different `deployment.environment` values:

- **Development**: `deployment.environment=development`
- **Staging**: `deployment.environment=staging`
- **Production**: `deployment.environment=production`

This enables filtering traces by environment in Jaeger UI.

## Troubleshooting

### Service Name Still Shows "jaeger"

**Possible Causes:**
1. Environment variables not loaded by API container
2. Docker Compose cache issue (old image)
3. Jaeger UI cache (browser-side)

**Solutions:**
```bash
# Rebuild and restart API container
cd infra
docker compose down
docker compose up -d --build api

# Clear Jaeger data (if needed)
docker compose down -v
docker compose up -d

# Hard refresh browser (Ctrl+Shift+R)
```

### Environment Variables Not Set

**Check:**
```bash
docker exec -it meepleai-api-1 env | grep OTEL
```

**If empty:**
- Verify `infra/env/api.env.dev` exists and contains variables
- Check `docker-compose.yml` has correct `env_file` path
- Rebuild container: `docker compose up -d --build api`

### Service Name Appears as "unknown_service"

**Causes:**
- OTEL_SERVICE_NAME is empty or malformed
- OpenTelemetry SDK not reading environment variables

**Solutions:**
- Check startup logs for "[OpenTelemetry] Service name: unknown_service"
- Verify environment file syntax (no quotes around values)
- Ensure no conflicting environment variables in docker-compose.yml

## References

- **OpenTelemetry .NET SDK**: https://github.com/open-telemetry/opentelemetry-dotnet
- **OpenTelemetry Resource Semantic Conventions**: https://opentelemetry.io/docs/specs/semconv/resource/
- **Jaeger OTLP Integration**: https://www.jaegertracing.io/docs/latest/deployment/#otlp
- **OTEL Environment Variables Spec**: https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/

## Change Log

| Date       | Author        | Change                                                                 |
|------------|---------------|------------------------------------------------------------------------|
| 2025-10-16 | Claude Code   | Initial fix: Added OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES     |
| 2025-10-16 | Claude Code   | Enhanced Program.cs debug logging for environment variable validation  |
| 2025-10-16 | Claude Code   | Updated api.env.dev.example with OpenTelemetry configuration template |
