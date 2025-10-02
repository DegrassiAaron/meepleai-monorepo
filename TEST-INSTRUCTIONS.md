# AI-01 Testing Instructions

## Status: ✅ Ready for PDF Upload Test

### What's Working:
- ✅ API running on http://localhost:8080
- ✅ PostgreSQL with all migrations applied
- ✅ Qdrant collection `meepleai_documents` created (1536-dim vectors, Cosine distance)
- ✅ Redis cache running
- ✅ Test user registered: `test@dev.com` (tenant: `dev`)
- ✅ Demo game created: `catan`
- ✅ Authentication working (session cookie saved in `/tmp/meeple_test_cookies.txt`)

### What Needs Manual Testing:

#### Step 1: Get a PDF File
Download a small sample PDF (e.g., board game rules):
```bash
# Example: Download a simple PDF
curl -o /tmp/sample.pdf "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf"

# Or use any board game rulebook PDF you have
```

#### Step 2: Upload PDF to API
```bash
curl -X POST http://localhost:8080/ingest/pdf \
  -b /tmp/meeple_test_cookies.txt \
  -F 'gameId=catan' \
  -F 'file=@/tmp/sample.pdf'
```

**Expected:**
- Response: `{"documentId":"...","fileName":"sample.pdf"}`
- Status: 200 OK

#### Step 3: Monitor Processing
Watch the API logs to see the text extraction and vector indexing:
```bash
docker logs -f meepleai-api
```

**You should see:**
1. "Saved PDF file to..." - File uploaded ✅
2. "Created PDF document record..." - Database entry ✅
3. "Starting text extraction for PDF..." - PDF-02 processing
4. "Text extraction completed for PDF..." - Text extracted
5. "Starting vector indexing for PDF..." - AI-01 processing
6. "Generated X chunks for PDF..." - Text chunked
7. "Vector indexing completed for PDF..." - Indexed in Qdrant

#### Step 4: Verify Vector Storage
Check that vectors were indexed in Qdrant:
```bash
curl -s http://localhost:6333/collections/meepleai_documents | \
  grep -o '"points_count":[0-9]*' | \
  cut -d: -f2
```

**Expected:** Number > 0 (should match chunk count from logs)

#### Step 5: Test RAG Query
Query the system using vector search:
```bash
curl -X POST http://localhost:8080/agents/qa \
  -H 'Content-Type: application/json' \
  -b /tmp/meeple_test_cookies.txt \
  -d '{
    "tenantId": "dev",
    "gameId": "catan",
    "query": "What is this document about?"
  }'
```

**Expected:**
- Response with `answer` field containing text from PDF
- `snippets` array with matching text chunks
- Each snippet should have `source`, `page`, and `line` fields

### Troubleshooting:

#### Issue: "No file provided"
- Make sure the PDF file exists at the path specified
- Use `@` prefix in curl: `-F 'file=@/path/to/file.pdf'`

#### Issue: "File size exceeds maximum"
- Max size: 50 MB
- Use a smaller PDF for testing

#### Issue: Processing stuck at "processing" status
Check API logs for errors:
```bash
docker logs meepleai-api --tail 100 | grep -E "(ERROR|Exception)"
```

#### Issue: "OPENROUTER_API_KEY" not configured
Add your OpenRouter API key to the environment:
```bash
# Edit infra/env/api.env.dev and set:
OPENROUTER_API_KEY=your_actual_key_here

# Restart API:
cd infra && docker compose -p meepleai restart api
```

#### Issue: "No relevant information found"
- Wait for indexing to complete (check logs)
- Verify vectors are in Qdrant (Step 4)
- Try a different query that matches PDF content

### Success Criteria:
- [ ] PDF uploads successfully
- [ ] Text extraction completes without errors
- [ ] Vector indexing completes (check Qdrant point count > 0)
- [ ] RAG query returns relevant snippets from the PDF
- [ ] Tenant/game filtering works (only returns results for "dev"/"catan")

### Architecture Verified:
```
PDF Upload → Text Extraction (PDF-02) → Chunking (512 chars) →
Embedding (OpenRouter) → Qdrant Indexing → Vector Search (AI-01) → RAG Response
```

All components integrated and ready for end-to-end testing! 🎉
