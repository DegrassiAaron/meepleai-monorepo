# OPS-02: Service Name Fix - Verification Guide

## Quick Verification

### 1. Restart API Container

```bash
cd D:\Repositories\meepleai-monorepo\infra
docker compose down api
docker compose up -d --build api
```

### 2. Check Environment Variables Loaded

```bash
docker exec meepleai-api-1 env | grep OTEL
```

**Expected Output**:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4318
OTEL_SERVICE_NAME=MeepleAI.Api
OTEL_RESOURCE_ATTRIBUTES=service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=development
```

### 3. Check Application Startup Logs

```bash
docker logs meepleai-api-1 | grep OpenTelemetry
```

**Expected Output**:
```
[OpenTelemetry] Configuring OTLP exporter with endpoint: http://jaeger:4318
[OpenTelemetry] Service name: MeepleAI.Api, version: 1.0.0
[OpenTelemetry] OTEL_SERVICE_NAME env var: MeepleAI.Api
[OpenTelemetry] OTEL_RESOURCE_ATTRIBUTES env var: service.name=MeepleAI.Api,service.version=1.0.0,deployment.environment=development
```

### 4. Verify in Jaeger UI

1. Open `http://localhost:16686` in browser
2. Click on "Service" dropdown in search panel
3. **VERIFY**: "MeepleAI.Api" appears in the list
4. Select "MeepleAI.Api" and click "Find Traces"
5. **VERIFY**: Traces are visible

### 5. Trigger Test Trace

Generate some traffic to ensure traces are flowing:

```bash
# Hit the API health endpoint
curl http://localhost:8080/health

# Hit a game endpoint
curl http://localhost:8080/api/v1/games
```

Then refresh Jaeger UI and verify new traces appear under "MeepleAI.Api".

## Detailed Verification

### Check Trace Resource Attributes

1. Open Jaeger UI: `http://localhost:16686`
2. Select service "MeepleAI.Api"
3. Click "Find Traces"
4. Click on any trace
5. Expand the root span
6. Check the "Process" section

**Expected Resource Attributes**:
```
service.name: MeepleAI.Api
service.version: 1.0.0
service.instance.id: <machine-name>
deployment.environment: development
telemetry.sdk.name: opentelemetry
telemetry.sdk.language: dotnet
telemetry.sdk.version: 1.13.1
```

### Verify OTLP Export is Working

Check Jaeger logs to confirm it's receiving OTLP data:

```bash
docker logs jaeger-1 | grep -i otlp
```

**Expected Output** (sample):
```
{"level":"info","ts":1729123456.789,"caller":"otlpreceiver/otlp.go:123","msg":"OTLP receiver started","kind":"receiver","data_type":"traces"}
{"level":"info","ts":1729123456.790,"caller":"otlpreceiver/otlp.go:456","msg":"Received OTLP request","spans":5,"service":"MeepleAI.Api"}
```

### Verify Service Name in Multiple Places

The service name should appear correctly in:

1. **Jaeger Service Dropdown**: Shows "MeepleAI.Api"
2. **Trace Details**: Root span shows service.name = MeepleAI.Api
3. **Service Graph**: If you view dependencies, service should be labeled "MeepleAI.Api"
4. **Operation List**: Operations should be grouped under "MeepleAI.Api"

## Troubleshooting

### Issue: Environment Variables Not Set

**Symptom**: `docker exec meepleai-api-1 env | grep OTEL` returns nothing

**Solutions**:

1. Verify `infra/env/api.env.dev` file exists and contains the variables
2. Check `docker-compose.yml` has correct `env_file` reference:
   ```yaml
   api:
     env_file:
       - ./env/api.env.dev
   ```
3. Rebuild and restart:
   ```bash
   docker compose down api
   docker compose up -d --build api
   ```

### Issue: Service Name Still Shows "jaeger"

**Symptom**: Service dropdown shows "jaeger" instead of "MeepleAI.Api"

**Solutions**:

1. **Clear Jaeger Data** (if using ephemeral storage):
   ```bash
   docker compose down jaeger
   docker volume rm meepleai-monorepo_jaegerdata  # If volume exists
   docker compose up -d jaeger
   ```

2. **Check Environment Variables in Container**:
   ```bash
   docker exec meepleai-api-1 env | grep OTEL_SERVICE_NAME
   ```
   If empty, environment variables not loaded.

3. **Check Application Logs**:
   ```bash
   docker logs meepleai-api-1 | grep "OTEL_SERVICE_NAME env var"
   ```
   Should show: `OTEL_SERVICE_NAME env var: MeepleAI.Api`

4. **Hard Refresh Browser**: Jaeger UI may cache service list (Ctrl+Shift+R)

### Issue: Logs Show "(not set)" for Environment Variables

**Symptom**:
```
[OpenTelemetry] OTEL_SERVICE_NAME env var: (not set)
```

**Root Cause**: Environment file not loaded or syntax error

**Solutions**:

1. Check file syntax - no quotes around values:
   ```bash
   # CORRECT
   OTEL_SERVICE_NAME=MeepleAI.Api

   # WRONG
   OTEL_SERVICE_NAME="MeepleAI.Api"
   ```

2. Verify file encoding (should be UTF-8, not UTF-16)

3. Check for Windows line endings (CRLF vs LF) - Docker prefers LF

### Issue: Traces Not Appearing at All

**Symptom**: Service name fixed but no traces visible

**Check**:

1. **Jaeger Health**: `curl http://localhost:16686/` should return 200
2. **OTLP Endpoint Reachable**:
   ```bash
   docker exec meepleai-api-1 curl -I http://jaeger:4318/v1/traces
   ```
   Should return 405 Method Not Allowed (means endpoint exists)
3. **Check Tracing Configuration**: See `docs/issue/ops-02-jaeger-tracing-fix.md`

## Validation Checklist

- [ ] Environment variables loaded in API container (OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES)
- [ ] Application startup logs show correct service name
- [ ] Application startup logs show environment variables loaded
- [ ] Jaeger UI service dropdown shows "MeepleAI.Api"
- [ ] Traces visible when selecting "MeepleAI.Api" service
- [ ] Trace resource attributes include service.name=MeepleAI.Api
- [ ] Trace resource attributes include service.version=1.0.0
- [ ] Trace resource attributes include deployment.environment=development
- [ ] Test traffic generates new traces visible in Jaeger
- [ ] No errors in API logs related to OpenTelemetry
- [ ] No errors in Jaeger logs related to OTLP ingestion

## Expected Timeline

- **Environment variable configuration**: 5 minutes
- **Container restart**: 2 minutes
- **Verification**: 5 minutes
- **Total**: ~12 minutes

If issues persist after following troubleshooting steps, see:
- `docs/tecnic/ops-02-jaeger-service-name-fix.md` - Full technical documentation
- `docs/issue/ops-02-jaeger-tracing-fix.md` - Original tracing fix (Activity Sources)
- `docs/ops-02-opentelemetry-design.md` - Complete OpenTelemetry architecture

## Post-Verification

Once service name is confirmed working:

1. **Update CI Environment**: Add same environment variables to CI pipeline
2. **Document for Production**: Update production deployment configs
3. **Monitor**: Check Jaeger UI regularly to ensure traces continue flowing
4. **Test Other Environments**: Apply same fix to staging/production configs
