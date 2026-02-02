# Document Service API

A production-ready PDF document generation microservice built with .NET 8 and QuestPDF. Accepts structured JSON data and generates professional, template-based PDF documents with secure, time-limited download links.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Getting Started](#getting-started)
5. [API Reference](#api-reference)
6. [Design Decisions](#design-decisions)
7. [Project Structure](#project-structure)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### What is Document Service?

A **generic, template-based PDF generation microservice** that:
- Accepts structured data (JSON) via REST API
- Applies configurable PDF templates using QuestPDF
- Generates professional, print-ready PDFs
- Provides secure, time-limited download links
- Supports multiple document types through extensible templates

### Current Templates

**Transaction Statement Template** - Banking statements with:
- Customer information and account summaries
- Transaction histories with category breakdowns
- Professional formatting with headers, footers, pagination

### Key Capabilities

- ✅ Template-based PDF generation with QuestPDF
- ✅ Secure token-based downloads with expiration
- ✅ File system storage with S3-ready architecture
- ✅ Comprehensive health monitoring
- ✅ Correlation ID-based request tracing
- ✅ SHA256 file integrity verification

---

## 🏗️ Architecture

### High-Level Architecture
```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  Document Service API (Port 5085)       │
│  ┌───────────────────────────────────┐  │
│  │ 1. Validate Request Data          │  │
│  │ 2. Select PDF Template            │  │
│  │ 3. Generate PDF (QuestPDF)        │  │
│  │ 4. Store in FileSystem/S3         │  │
│  │ 5. Return Secure Download Link    │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### Layered Architecture
```
┌─────────────────────────────────────┐
│  Presentation Layer (API)           │
│  - DocumentRoutes                   │
│  - Middleware (Correlation IDs)     │
│  - Health Checks                    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Models Layer (DTOs)                │
│  - Request/Response DTOs            │
│  - Template Data Models             │
│  - Enums (DocumentType)             │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Core Layer (Interfaces)            │
│  - IDocumentService                 │
│  - IDocumentValidator               │
│  - IPdfTemplate                     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Infrastructure Layer               │
│  - Templates (QuestPDF)             │
│  - Storage (FileSystem/S3)          │
│  - Document Service                 │
│  - Validator                        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Data Layer                         │
│  - EF Core Context                  │
│  - Document Entities                │
│  - SQLite Database                  │
└─────────────────────────────────────┘
```

### Request Flow
```
1. POST /api/documents/generate
   ↓
2. Validate request data
   ↓
3. Get template for DocumentType
   ↓
4. Render PDF using QuestPDF
   ↓
5. Generate secure token (256-bit)
   ↓
6. Store PDF (FileSystem/S3)
   ↓
7. Save metadata to SQLite
   ↓
8. Return download URL + token

Download Flow:
1. GET /api/documents/{id}/download?token={token}
   ↓
2. Validate token (not expired/revoked/over limit)
   ↓
3. Retrieve PDF from storage
   ↓
4. Update usage statistics
   ↓
5. Stream PDF to client
```

---

## 🛠️ Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | .NET | 8.0 | Modern C# framework |
| **PDF Library** | QuestPDF | 2024.12.3 | Fluent PDF generation |
| **Database** | SQLite + EF Core | 8.0 | Metadata storage |
| **Logging** | Serilog | 8.0.0 | Structured logging |
| **API Docs** | Swagger/OpenAPI | 8.0 | Interactive documentation |
| **Health Checks** | ASP.NET Health Checks | 8.0 | Monitoring |

### Why QuestPDF?

| Feature | QuestPDF | Alternatives |
|---------|----------|--------------|
| **License** | MIT (Free) | AGPL/Commercial |
| **API Style** | Fluent C# | Imperative/HTML |
| **Performance** | Excellent | Good |
| **Tables** | Excellent | Varies |
| **Maintenance** | Active | Varies |

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (optional)
- Port 5085 available

### Local Development

```bash
# 1. Navigate to API project
cd DocumentService/Presentation/Document.Api

# 2. Restore dependencies
dotnet restore

# 3. Run database migrations
dotnet ef database update --project ../../Infrastructure/Document.Data

# 4. Run the API
dotnet run

# Output: Now listening on: http://localhost:5085
```

### Docker Deployment

```bash
# Build and run with docker-compose
docker-compose up document-api

# Or build standalone
docker build -f DocumentService/Presentation/Document.Api/Dockerfile -t document-api .
docker run -p 5085:8080 document-api
```

### Verify Installation

```bash
# Check health
curl http://localhost:5085/health

# Open Swagger UI
open http://localhost:5085/swagger
```

---

## 📡 API Reference

### Base URL
- **Local:** `http://localhost:5085`
- **Docker:** `http://localhost:5085`

---

### 1. Generate Document

**Request:**
```http
POST /api/documents/generate
Content-Type: application/json
X-Correlation-Id: optional-id

{
  "documentType": 1,
  "fileName": "optional-custom-name.pdf",
  "data": {
    "customerId": 12345,
    "customerName": "John Doe",
    "accounts": [
      {
        "accountId": 67890,
        "accountType": "Checking",
        "accountNumber": "****1234",
        "currentBalance": 5000.00,
        "availableBalance": 4800.00,
        "currency": "ZAR",
        "transactions": [
          {
            "transactionId": 1,
            "date": "2024-01-15T10:30:00Z",
            "amount": -150.00,
            "description": "Grocery Store Purchase",
            "merchantCategory": "Groceries",
            "balanceAfter": 4850.00
          }
        ]
      }
    ],
    "totalTransactions": 2,
    "dateRange": {
      "fromDate": "2024-01-01T00:00:00Z",
      "toDate": "2024-01-31T23:59:59Z"
    }
  },
  "options": {
    "tokenExpiryMinutes": 120,
    "maxDownloads": 10
  }
}
```

**Response (200 OK):**
```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "documentType": 1,
  "fileName": "statement_20240128.pdf",
  "fileSizeBytes": 245678,
  "downloadUrl": "http://localhost:5085/api/documents/3fa85f64.../download?token=8kJh3...",
  "downloadToken": "8kJh3-2xP_9fL4nQ5mR7wT1vY6zB0cD...",
  "expiresAt": "2024-01-28T12:00:00Z",
  "maxDownloads": 10,
  "generatedAt": "2024-01-28T10:00:00Z"
}
```

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed",
  "errorCode": "VALIDATION_ERROR",
  "validationErrors": [
    "Required field 'customerId' is missing"
  ]
}
```

---

### 2. Download Document

**Request:**
```http
GET /api/documents/{documentId}/download?token={token}
```

**Example:**
```bash
curl -o statement.pdf \
  "http://localhost:5085/api/documents/3fa85f64.../download?token=8kJh3..."
```

**Response (200 OK):**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="statement.pdf"
Content-Length: 245678

[PDF binary stream]
```

**Response (401 Unauthorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid token, expired link, or download limit reached"
}
```

---

### 3. Get Supported Document Types

**Request:**
```http
GET /api/documents/types
```

**Response (200 OK):**
```json
[
  {
    "type": 1,
    "name": "Transaction Statement",
    "description": "Banking transaction statements",
    "requiredFields": ["customerId", "customerName", "accounts"],
    "optionalFields": ["dateRange"]
  }
]
```

---

### 4. Health Check

**Request:**
```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database is accessible",
      "data": { "documentCount": 42 }
    },
    "storage": {
      "status": "Healthy",
      "description": "Storage is accessible"
    },
    "pdf-generation": {
      "status": "Healthy",
      "description": "1 PDF template(s) registered"
    }
  }
}
```

---

## ⚖️ Design Decisions

### 1. Dictionary vs Strongly-Typed Request

**Decision:** Accept `Dictionary<string, object>` in API, convert to typed models internally

**Why:**
- ✅ Generic - any document type can send any structure
- ✅ Extensible - add templates without API changes
- ✅ Flexible - callers don't need internal models
- ❌ No compile-time validation

**Approach:**
```csharp
// API accepts dictionary
Dictionary<string, object> data

// Template converts to typed model
var json = JsonSerializer.Serialize(data);
var model = JsonSerializer.Deserialize<TransactionStatementData>(json);
```

---

### 2. SQLite vs PostgreSQL

**Decision:** SQLite for metadata storage

**Trade-offs:**

| Aspect | SQLite | PostgreSQL |
|--------|--------|------------|
| **Setup** | ✅ Zero config | ❌ Installation required |
| **Portability** | ✅ Single file | ❌ Server process |
| **Scalability** | ⚠️ Single server | ✅ Multi-server |
| **Production** | ⚠️ Limited | ✅ Enterprise-grade |

**Migration Path:** Change connection string to switch databases

---

### 3. Synchronous PDF Generation

**Decision:** Generate PDFs synchronously (blocking request)

**Why Synchronous:**
- ✅ Simpler - no queue infrastructure
- ✅ Instant feedback - user gets URL immediately
- ✅ Good for small PDFs - 1-2 seconds
- ❌ Blocks request - user waits
- ❌ Timeout risk for large PDFs

**When to Use Queue:**
- PDFs take > 30 seconds
- Batch generation
- Email delivery workflows

---

### 4. File System vs S3 Storage

**Decision:** File System for dev, S3-ready for production

| Criteria | File System | S3 |
|----------|-------------|-----|
| **Dev Speed** | ✅ Fast | ⚠️ Setup required |
| **Scalability** | ❌ Single server | ✅ Unlimited |
| **Redundancy** | ❌ None | ✅ 99.999999999% |
| **Cost** | ✅ Free | Pay per GB |

**Switching:** Update config, no code changes required

---

### 5. Token Security

**Decision:** Database lookup with random tokens

**Token Generation:**
```csharp
var randomBytes = new byte[32];  // 256 bits
using var rng = RandomNumberGenerator.Create();
rng.GetBytes(randomBytes);
return Convert.ToBase64String(randomBytes)
    .Replace("+", "-")
    .Replace("/", "_")
    .Replace("=", "");
```

**Why Database Lookup:**
- ✅ Revocable - can invalidate immediately
- ✅ Use count tracking
- ✅ Audit trail
- ❌ Database hit per download

**Alternative (JWT):**
- ✅ No database needed
- ❌ Not revocable
- ❌ No use tracking

---

## 📁 Project Structure

```
DocumentService/
├── Presentation/
│   └── Document.Api/
│       ├── Routes/
│       ├── Middleware/
│       ├── HealthChecks/
│       ├── Program.cs
│       └── Dockerfile
│
├── Models/
│   └── Document.Models/
│       ├── Requests/
│       ├── Responses/
│       ├── TemplateData/
│       └── Enums/
│
├── Core/
│   └── Document.Core/
│       ├── Services/
│       └── Exceptions/
│
├── Infrastructure/
│   └── Document.Infrastructure/
│       ├── Pdf/
│       │   ├── Templates/
│       │   └── PdfTemplateRegistry.cs
│       ├── Storage/
│       │   ├── FileSystemStorageProvider.cs
│       │   └── S3StorageProvider.cs (future)
│       └── Services/
│
└── Data/
    └── Document.Data/
        ├── Entities/
        ├── Configurations/
        ├── Migrations/
        └── DocumentDbContext.cs
```

---

## 🔧 Troubleshooting

### Issue: "Database locked" error

**Solution:**
```bash
# Close database tools
# Delete lock files
rm documents.db-shm documents.db-wal

# Restart API
dotnet run
```

---

### Issue: "Template not found"

**Solution:**
```csharp
// Ensure template is registered in Program.cs
builder.Services.AddSingleton<IPdfTemplate, TransactionStatementTemplate>();
```

---

### Issue: "Storage not accessible"

**Solution:**
```bash
# Create storage directory
mkdir -p wwwroot/documents

# Check permissions
chmod 755 wwwroot/documents

# Verify configuration
cat appsettings.json | grep BasePath
```

---

### Issue: Health check fails

**Solution:**
```bash
# Check individual endpoints
curl http://localhost:5085/health/db
curl http://localhost:5085/health/storage

# Run migrations
dotnet ef database update --project Infrastructure/Document.Data
```