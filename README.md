# Banking Microservices Platform

A production-ready .NET 8 microservices architecture demonstrating transaction aggregation, PDF document generation, and real-time fraud detection with guaranteed message delivery.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Services](#services)
4. [Quick Start](#quick-start)
5. [Running with Docker](#running-with-docker)
6. [Service Communication](#service-communication)
7. [Testing the Platform](#testing-the-platform)
8. [Configuration](#configuration)
9. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

This platform demonstrates modern microservices patterns and practices:

### Core Services

1. **Transaction Aggregation Service** - Aggregates customer transaction data from multiple sources
2. **Fraud Engine Service** - Real-time fraud detection using rule-based algorithms
3. **Document Service** - PDF document generation with secure downloads

### Supporting Services

4. **Mock Provider Services** - Simulates downstream banking APIs (Customer, Account, Transaction)

### Key Features

- ✅ **Guaranteed Message Delivery** - Outbox pattern for reliable async communication
- ✅ **Fraud Detection** - Real-time transaction analysis with 5 configurable rules
- ✅ **PDF Generation** - Template-based documents with QuestPDF
- ✅ **Resilience** - Polly policies (retry, circuit breaker, timeout)
- ✅ **Caching** - Stale-While-Revalidate pattern for performance
- ✅ **Observability** - Structured logging, correlation IDs, health checks
- ✅ **Clean Architecture** - Clear separation of concerns across all services

---

## 🏗️ System Architecture

### High-Level Architecture
```
┌──────────────────────────────────────────────────────────────┐
│                         Client                                │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│         Transaction Aggregation Service (Port 5118)            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ • Fetch customer data from Mock APIs                     │  │
│  │ • Categorize transactions (MCC codes + merchant names)   │  │
│  │ • Write to Outbox (guaranteed delivery)                  │  │
│  │ • Return response < 500ms                                │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────┬──────────────────────┬──────────────────┬───────────────┘
       │                      │                  │
       │                      │                  │
       ▼                      ▼                  ▼
┌──────────────┐    ┌──────────────────┐    ┌──────────────┐
│ Mock APIs    │    │ Outbox Processor │    │ Document API │
│ (Ports       │    │ (Background)     │    │ (Port 5085)  │
│ 5001-5003)   │    │ • Poll every 1s  │    │              │
│              │    │ • Batch: 50 msg  │    │ • Generate   │
│ • Customer   │    │ • Retry: exp.    │    │   PDFs       │
│ • Account    │    │   backoff        │    │ • Secure     │
│ • Transaction│    └────────┬─────────┘    │   downloads  │
└──────────────┘             │              └──────────────┘
                             │
                             ▼
                   ┌──────────────────┐
                   │  Fraud Engine    │
                   │  (Port 5160)     │
                   │                  │
                   │ • 5 fraud rules  │
                   │ • Risk scoring   │
                   │ • Idempotency    │
                   │ • Alert storage  │
                   └──────────────────┘
```

### Service Dependencies

```
Transaction Aggregation Service
├─→ Customer Mock API (5001)
├─→ Account Mock API (5003)
├─→ Transaction Mock API (5002)
├─→ Fraud Engine API (5160) - async via Outbox
└─→ Document API (5085) - sync for PDFs

Fraud Engine Service
└─→ (No external dependencies)

Document Service
└─→ (No external dependencies)
```

### Data Flow

```
1. Client Request
   ↓
2. Transaction Aggregation API
   ├─→ Fetch customer details
   ├─→ Fetch accounts
   ├─→ Fetch transactions
   ├─→ Categorize transactions
   ├─→ Write to Outbox (guaranteed)
   └─→ Return response < 500ms
   ↓
3. Background Worker (every 1s)
   ├─→ Read pending Outbox messages
   ├─→ Send to Fraud Engine (with Polly)
   └─→ Mark as Completed/Failed
   ↓
4. Fraud Engine
   ├─→ Check idempotency
   ├─→ Run fraud rules
   ├─→ Calculate risk score
   ├─→ Store alert
   └─→ Return result
```

---

## 📦 Services

### 1. Transaction Aggregation Service
**Port:** 5118  
**Purpose:** Orchestrates transaction data from multiple sources

[📖 Full Documentation](./docs/TRANSACTION_AGGREGATION_SERVICE.md)

**Key Features:**
- Aggregates customer transactions from 3 mock APIs
- Intelligent categorization using MCC codes
- Outbox pattern for guaranteed fraud detection delivery
- Stale-While-Revalidate caching
- Polly resilience (retry, circuit breaker, timeout)

---

### 2. Fraud Engine Service
**Port:** 5160  
**Purpose:** Real-time fraud detection

[📖 Full Documentation](./docs/FRAUD_ENGINE_SERVICE.md)

**Key Features:**
- 5 configurable fraud detection rules
- Risk score calculation (0-100)
- Idempotent processing via X-Event-Id
- Status determination (Clear/Flagged/Blocked)
- SQLite storage for alerts and history

**Fraud Rules:**
| Rule | Score | Example |
|------|-------|---------|
| HighAmountForCategory | 30 | Groceries > R5,000 |
| VelocityCheck | 35 | 6+ transactions in 60 min |
| UnusualTimeTransaction | 15 | Shopping at 2 AM |
| FirstTimeCategory | 10 | First crypto purchase |
| HighRiskCategory | 40 | Cryptocurrency, Gambling |

---

### 3. Document Service
**Port:** 5085  
**Purpose:** PDF document generation

[📖 Full Documentation](./docs/DOCUMENT_SERVICE.md)

**Key Features:**
- Template-based PDF generation using QuestPDF
- Secure token-based downloads
- Configurable expiration and usage limits
- File system storage (S3-ready)
- SHA256 file integrity verification

**Current Templates:**
- Transaction Statement (banking statements with transaction history)

---

### 4. Mock Provider Services
**Ports:** 5001 (Customer), 5002 (Transaction), 5003 (Account)  
**Purpose:** Simulates downstream banking APIs

**Endpoints:**
- `GET /api/customers/{id}` - Customer details
- `GET /api/customers/{id}/accounts` - Customer accounts
- `GET /api/accounts/{id}/balance` - Account balance
- `GET /api/transactions?accountId={id}` - Transaction history

**Test Data:**
- Customer IDs: 500, 501, 502
- Account IDs: 1001, 1002, 1003, 1004, 1005

---

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Docker Desktop (optional)
- Git

### Option 1: Docker Compose (Recommended)

```bash
# Clone repository
git clone <repository-url>
cd <repository-name>

# Start all services
docker-compose up

# Services will be available at:
# - Transaction Aggregation API: http://localhost:5118
# - Fraud Engine API: http://localhost:5160
# - Document API: http://localhost:5085
# - Mock Customer API: http://localhost:5001
# - Mock Transaction API: http://localhost:5002
# - Mock Account API: http://localhost:5003
```

### Option 2: Local Development

**Terminal 1 - Mock Services:**
```bash
cd MockProviders
dotnet run

# Output: Now listening on http://localhost:5160
```

**Terminal 2 - Fraud Engine:**
```bash
cd FraudEngineService/Presentation/FraudEngine.Api
dotnet run

# Output: Now listening on http://localhost:5160
```

**Terminal 3 - Document Service:**
```bash
cd DocumentService/Presentation/Document.Api
dotnet run

# Output: Now listening on http://localhost:5085
```

**Terminal 4 - Transaction Aggregation:**
```bash
cd TransactionAggregationService/Presentation/TransactionAggregation.Api
dotnet run

# Output: Now listening on http://localhost:5118
```

---

## 🐳 Running with Docker

### Docker Compose Configuration

The `docker-compose.yml` orchestrates all services:

```yaml
services:
  # Main Services
  transaction-aggregation-api:
    image: transaction-aggregation-api
    ports:
      - "5118:8080"
    environment:
      - MockProviders__CustomerServiceUrl=http://customer-mock:8080
      - MockProviders__TransactionServiceUrl=http://transaction-mock:8080
      - MockProviders__AccountServiceUrl=http://account-mock:8080
      - FraudEngineApiClient__BaseUrl=http://fraud-api:8080
      - DocumentApiClient__BaseUrl=http://document-api:8080

  fraud-api:
    image: fraud-engine-api
    ports:
      - "5160:8080"

  document-api:
    image: document-api
    ports:
      - "5085:8080"

  # Mock Providers
  customer-mock:
    image: mock-customer
    ports:
      - "5001:8080"

  transaction-mock:
    image: mock-transaction
    ports:
      - "5002:8080"

  account-mock:
    image: mock-account
    ports:
      - "5003:8080"
```

### Docker Commands

```bash
# Start all services
docker-compose up

# Start in detached mode
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Rebuild images
docker-compose build

# Restart specific service
docker-compose restart transaction-aggregation-api
```

---

## 🔗 Service Communication

### Synchronous Communication

**Transaction Aggregation → Mock APIs**
- Pattern: HTTP REST with Polly resilience
- Timeout: 5 seconds
- Retry: 3 attempts with exponential backoff
- Circuit Breaker: Opens after 5 failures

**Transaction Aggregation → Document API**
- Pattern: Synchronous HTTP POST
- Use Case: Generate PDF statements
- Reason: Immediate confirmation required

### Asynchronous Communication

**Transaction Aggregation → Fraud Engine**
- Pattern: Outbox pattern with background worker
- Polling Interval: 1 second
- Batch Size: 50 messages
- Retry: Up to 10 attempts with exponential backoff
- Idempotency: X-Event-Id header

**Benefits:**
- ✅ Guaranteed delivery (survives crashes)
- ✅ Fast user response (< 500ms)
- ✅ No message broker required
- ✅ Observable processing status

---

## 🧪 Testing the Platform

### End-to-End Workflow

```bash
# 1. Get customer transactions (triggers entire flow)
curl "http://localhost:5118/api/customers/500/transactions?fromDate=2024-01-01T00:00:00Z&toDate=2024-01-31T23:59:59Z"

# 2. Check Outbox processing
sqlite3 TransactionAggregationService/transactionaggregation.db \
  "SELECT EventId, Status, AttemptCount FROM OutboxMessages ORDER BY CreatedAt DESC LIMIT 5;"

# 3. Verify Fraud Engine received transactions
curl "http://localhost:5160/api/fraud/alerts?customerId=500"

# 4. Generate PDF statement
curl -X POST http://localhost:5085/api/documents/generate \
  -H "Content-Type: application/json" \
  -d '{
    "documentType": 1,
    "data": {
      "customerId": 500,
      "customerName": "John Doe",
      "accounts": [...],
      "totalTransactions": 10
    }
  }'
```

### Health Check Endpoints

```bash
# Check all services
curl http://localhost:5118/health  # Transaction Aggregation
curl http://localhost:5160/health  # Fraud Engine
curl http://localhost:5085/health  # Document Service
```

### Swagger Documentation

- **Transaction Aggregation:** http://localhost:5118/swagger
- **Fraud Engine:** http://localhost:5160/swagger
- **Document Service:** http://localhost:5085/swagger

---

## ⚙️ Configuration

### Environment Variables (Docker)

```yaml
# Transaction Aggregation Service
ASPNETCORE_ENVIRONMENT: Development
ASPNETCORE_HTTP_PORTS: 8080
MockProviders__CustomerServiceUrl: http://customer-mock:8080
MockProviders__TransactionServiceUrl: http://transaction-mock:8080
MockProviders__AccountServiceUrl: http://account-mock:8080
FraudEngineApiClient__BaseUrl: http://fraud-api:8080
DocumentApiClient__BaseUrl: http://document-api:8080
```

### Configuration Files

Each service has an `appsettings.json`:

**Transaction Aggregation:**
```json
{
  "OutboxProcessor": {
    "PollingIntervalSeconds": 1,
    "BatchSize": 50,
    "MaxRetryAttempts": 10
  },
  "FraudEngineClient": {
    "BaseUrl": "http://localhost:5160",
    "TimeoutSeconds": 10,
    "RetryAttempts": 3
  }
}
```

**Fraud Engine:**
```json
{
  "RuleConfiguration": {
    "CategoryAmountThresholds": {
      "Groceries": 5000,
      "Shopping": 10000,
      "Entertainment": 1000
    },
    "VelocityWindowMinutes": 60,
    "VelocityThreshold": 5
  }
}
```

**Document Service:**
```json
{
  "Document": {
    "Storage": {
      "Provider": "FileSystem",
      "FileSystem": {
        "BasePath": "wwwroot/documents"
      }
    },
    "Security": {
      "DefaultTokenExpiryMinutes": 60,
      "DefaultMaxDownloads": 5
    }
  }
}
```

---

## 🔧 Troubleshooting

### Issue: Services Can't Communicate

**Symptom:** Transaction Aggregation can't reach Mock APIs

**Solution:**
```bash
# Docker: Ensure services are on same network
docker network inspect <network-name>

# Local: Verify all services are running
curl http://localhost:5001/health  # Customer Mock
curl http://localhost:5002/health  # Transaction Mock
curl http://localhost:5003/health  # Account Mock
```

---

### Issue: Outbox Messages Not Processing

**Check 1: Fraud Engine is Running**
```bash
curl http://localhost:5160/health
```

**Check 2: Outbox Worker is Running**
```bash
# Check logs for:
# "Outbox Processor started - Polling every 1s"
```

**Check 3: Database Status**
```bash
sqlite3 transactionaggregation.db << EOF
SELECT Status, COUNT(*) as Count, AVG(AttemptCount) as AvgAttempts
FROM OutboxMessages
GROUP BY Status;
EOF
```

---

### Issue: Port Already in Use

**Solution:**
```bash
# Find process using port
lsof -i :5118

# Kill process
kill -9 <PID>

# Or use different ports in docker-compose.yml
ports:
  - "5119:8080"  # Change external port
```

---

### Issue: Database Locked

**Symptom:**
```
SQLite Error: database is locked
```

**Solution:**
```bash
# Stop all services
docker-compose down
# Or: kill all dotnet processes

# Delete lock files
find . -name "*.db-wal" -delete
find . -name "*.db-shm" -delete

# Restart services
docker-compose up
```

---

### Issue: Docker Build Failures

**Solution:**
```bash
# Clean Docker cache
docker-compose build --no-cache

# Remove old images
docker image prune -a

# Rebuild specific service
docker-compose build transaction-aggregation-api
```

---

## 📚 Additional Resources

### Service Documentation

- [Transaction Aggregation Service](./docs/TRANSACTION_AGGREGATION_SERVICE.md) - Detailed API docs, architecture, and design decisions
- [Fraud Engine Service](./docs/FRAUD_ENGINE_SERVICE.md) - Fraud rules, risk scoring, and testing scenarios
- [Document Service](./docs/DOCUMENT_SERVICE.md) - PDF generation, templates, and security model

### Architecture Patterns

- **Outbox Pattern** - Guaranteed message delivery without message broker
- **Stale-While-Revalidate** - Performance optimization with background refresh
- **Polly Resilience** - Retry, circuit breaker, and timeout patterns
- **Clean Architecture** - Clear separation of concerns across all services
- **Result Pattern** - Explicit error handling without exceptions

---

## 📄 License

This is an assessment project. All rights reserved.

Built with ❤️ using .NET 8.