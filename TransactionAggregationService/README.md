# Transaction Aggregation Service

A .NET 8 microservice that aggregates financial transaction data from multiple sources, provides intelligent categorization, and integrates with fraud detection for real-time risk analysis via the Outbox pattern.

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

### What is Transaction Aggregation Service?

An **orchestration layer** that:
- Consolidates customer transaction data from distributed microservices
- Applies intelligent categorization using MCC codes and merchant names
- Sends transactions to Fraud Engine with guaranteed delivery (Outbox pattern)
- Provides enriched analytics for financial insights
- Implements sophisticated caching with Stale-While-Revalidate pattern

### System Components

1. **Transaction Aggregation API** - Main orchestration service
2. **Mock Provider Services** - Simulates downstream APIs (Customer, Account, Transaction)
3. **Outbox Processor** - Background worker for guaranteed message delivery
4. **Fraud Engine Client** - Integration with fraud detection

### Key Capabilities

- ✅ Real-time transaction aggregation across multiple accounts
- ✅ Intelligent categorization using MCC codes and merchant names
- ✅ Guaranteed fraud detection delivery via Outbox pattern
- ✅ Stale-While-Revalidate caching for fast responses
- ✅ Production-grade resilience (retry, circuit breaker, timeout)
- ✅ Correlation ID-based request tracing

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
│  Transaction Aggregation API (5118)     │
│  ┌───────────────────────────────────┐  │
│  │ 1. Aggregate Transactions         │  │
│  │ 2. Categorize Merchants           │  │
│  │ 3. Write to Outbox (guaranteed)   │  │
│  │ 4. Return Response (< 500ms)      │  │
│  └───────────────────────────────────┘  │
└──┬────────────────────┬─────────────────┘
   │                    │
   │                    ▼ (async, 1s polling)
   │           ┌────────────────────────┐
   │           │  Outbox Processor      │
   │           │  - Poll every 1s       │
   │           │  - Batch: 50 messages  │
   │           │  - Parallel: 10 threads│
   │           │  - Retry: exponential  │
   │           └───────────┬────────────┘
   │                       │
   ▼                       ▼
┌──────────────┐    ┌─────────────────┐
│ Mock APIs    │    │  Fraud Engine   │
│ - Customer   │    │  (Port 5160)    │
│ - Account    │    └─────────────────┘
│ - Transaction│
└──────────────┘
```

### Layered Architecture
```
┌─────────────────────────────────────┐
│  Presentation Layer (API)           │
│  - TransactionEndpoints             │
│  - HealthEndpoints                  │
│  - Middleware (Correlation IDs)     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Core Layer (Business Logic)        │
│  - TransactionAggregationService    │
│  - CategorizationService            │
│  - Result<T> Pattern                │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Infrastructure Layer               │
│  - API Clients (Polly Resilience)   │
│  - Cache Service (SWR)              │
│  - Outbox Publisher                 │
│  - Background Worker                │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Data Layer                         │
│  - EF Core Context                  │
│  - Outbox Entities                  │
│  - SQLite Database                  │
└─────────────────────────────────────┘
```

### Request Flow
```
1. GET /api/customers/{id}/transactions
   ↓
2. Fetch customer details (Customer API)
   ↓
3. Fetch accounts (Account API)
   ↓
4. Fetch transactions (Transaction API)
   ↓
5. Categorize transactions (MCC + merchant names)
   ↓
6. Write events to Outbox table (guaranteed)
   ↓
7. Return aggregated response (< 500ms)

Background Worker (every 1s):
1. Read pending messages from Outbox
   ↓
2. Send to Fraud Engine (with Polly)
   ├─→ Retry (3 attempts, exponential backoff)
   ├─→ Circuit Breaker (open after 5 failures)
   └─→ Timeout (10s)
   ↓
3. Mark as Completed or Failed
```

---

## 🛠️ Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | .NET | 8.0 | Modern C# framework |
| **API Style** | ASP.NET Core Minimal APIs | 8.0 | Lightweight endpoints |
| **Resilience** | Polly | 8.2 | Retry, circuit breaker, timeout |
| **Caching** | IMemoryCache | 8.0 | Stale-While-Revalidate |
| **Database** | SQLite + EF Core | 8.0 | Outbox storage |
| **Logging** | Serilog | 8.0.0 | Structured logging |
| **API Docs** | Swagger/OpenAPI | 8.0 | Interactive documentation |

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (optional)
- Ports available: 5118 (API), 5001-5003 (Mocks), 5160 (Fraud Engine)

### Local Development

```bash
# 1. Start Mock Services (required)
cd MockProviders
dotnet run

# Output: Mock Provider API started on http://localhost:5160

# 2. Start Fraud Engine (in separate terminal)
cd FraudEngineService/Presentation/FraudEngine.Api
dotnet run

# Output: Now listening on: http://localhost:5160

# 3. Start Transaction Aggregation API (in separate terminal)
cd TransactionAggregationService/Presentation/TransactionAggregation.Api
dotnet run

# Output: 
# Outbox Processor started - Polling every 1s
# Now listening on: http://localhost:5118
```

### Docker Deployment

```bash
# Build and run entire stack
docker-compose up

# Or build standalone
docker build -f TransactionAggregationService/Presentation/TransactionAggregation.Api/Dockerfile -t transaction-aggregation-api .
docker run -p 5118:8080 transaction-aggregation-api
```

### Verify Installation

```bash
# Check health
curl http://localhost:5118/health

# Open Swagger UI
open http://localhost:5118/swagger
```

---

## 📡 API Reference

### Base URL
- **Local:** `http://localhost:5118`
- **Docker:** `http://localhost:5118`

---

### 1. Get Customer Transactions

**Request:**
```http
GET /api/customers/{customerId}/transactions?fromDate={date}&toDate={date}
```

**Query Parameters:**
- `fromDate` (optional): Filter start date (ISO 8601)
- `toDate` (optional): Filter end date (ISO 8601)

**Example:**
```bash
curl "http://localhost:5118/api/customers/500/transactions?fromDate=2024-01-01T00:00:00Z&toDate=2024-01-31T23:59:59Z"
```

**Response (200 OK):**
```json
{
  "customerId": 500,
  "customerName": "John Doe",
  "accounts": [
    {
      "accountId": 1001,
      "accountType": "Savings",
      "accountNumber": "ACC-1001",
      "currency": "ZAR",
      "currentBalance": 15000.00,
      "availableBalance": 14500.00,
      "transactions": [
        {
          "transactionId": 100074,
          "accountId": 1001,
          "date": "2024-01-15T14:30:00Z",
          "amount": 450.00,
          "currency": "ZAR",
          "description": "POS Purchase - WOOLWORTHS CAPE TOWN",
          "type": "Debit",
          "status": "Completed",
          "merchantName": "WOOLWORTHS CAPE TOWN",
          "merchantCode": "5411",
          "merchantCategory": "Groceries",
          "balanceAfter": 14550.00
        }
      ]
    }
  ],
  "totalTransactions": 1,
  "dateRange": {
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z"
  }
}
```

**What Happens Behind the Scenes:**
1. ✅ Aggregates data from Customer, Account, Transaction APIs
2. ✅ Categorizes transactions based on MCC codes
3. ✅ **Writes events to Outbox** (guaranteed persistence)
4. ✅ Returns response in < 500ms
5. ⏰ Background worker processes outbox asynchronously

---

### 2. Get Transaction Summary

**Request:**
```http
GET /api/customers/{customerId}/transactions/summary?fromDate={date}&toDate={date}
```

**Example:**
```bash
curl "http://localhost:5118/api/customers/500/transactions/summary?fromDate=2024-01-01T00:00:00Z"
```

**Response (200 OK):**
```json
{
  "customerId": 500,
  "customerName": "John Doe",
  "totalDebits": 5000.00,
  "totalCredits": 10000.00,
  "netAmount": 5000.00,
  "categorySummaries": [
    {
      "category": "Groceries",
      "totalAmount": 2500.00,
      "transactionCount": 5,
      "percentageOfTotal": 50.0,
      "averageTransactionAmount": 500.00
    }
  ],
  "dateRange": {
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z"
  }
}
```

---

### 3. Health Check

**Request:**
```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-02-01T14:30:00Z",
  "service": "TransactionAggregation.API",
  "checks": {
    "external_apis": {
      "status": "Healthy",
      "description": "All external services are healthy",
      "data": {
        "CustomerService": "Healthy",
        "TransactionService": "Healthy",
        "AccountService": "Healthy"
      }
    },
    "outbox": {
      "status": "Healthy",
      "description": "Outbox processing is healthy",
      "data": {
        "pending_count": 0,
        "failed_count": 0
      }
    }
  }
}
```

**Degraded State:**
```json
{
  "status": "Degraded",
  "checks": {
    "outbox": {
      "status": "Degraded",
      "description": "Outbox processing lag: 6.5 minutes",
      "data": {
        "pending_count": 50,
        "failed_count": 2
      }
    }
  }
}
```

---

### 4. Mock Service Endpoints

**Customer Service (Port 5001):**
```bash
# Get customer
GET http://localhost:5001/api/customers/{customerId}

# Get customer accounts
GET http://localhost:5001/api/customers/{customerId}/accounts
```

**Transaction Service (Port 5002):**
```bash
# Get transactions
GET http://localhost:5002/api/transactions?accountId={accountId}&fromDate={date}&toDate={date}
```

**Account Service (Port 5003):**
```bash
# Get account balance
GET http://localhost:5003/api/accounts/{accountId}/balance
```

**Test Customer IDs:** 500, 501, 502

---

## ⚖️ Design Decisions

### 1. Outbox Pattern vs Direct HTTP Calls

**Decision:** Outbox pattern with background processing

**Trade-offs:**

| Aspect | Outbox Pattern | Direct HTTP |
|--------|---------------|-------------|
| **Reliability** | ✅ Guaranteed delivery | ❌ Messages can be lost |
| **Performance** | ✅ Fast response (< 500ms) | ❌ Waits for downstream |
| **Complexity** | ⚠️ Background worker | ✅ Simple |
| **Resilience** | ✅ Survives crashes | ❌ Lost if app restarts |

**Why Outbox:**
- Guaranteed delivery is critical for fraud detection
- User experience prioritized (sub-500ms response)
- No external message broker needed

---

### 2. Polling Interval: 1 Second

**Decision:** Background worker polls every 1 second

**Why 1 Second:**
- ✅ Near real-time processing (1-3 second latency)
- ✅ Fast fraud detection for high-value transactions
- ⚠️ Slightly higher database load
- ⚠️ More frequent HTTP requests

**Alternative:** 5-10 seconds (lower load, slower processing)

**Configuration:**
```json
{
  "OutboxProcessor": {
    "PollingIntervalSeconds": 1,
    "BatchSize": 50,
    "MaxRetryAttempts": 10
  }
}
```

---

### 3. Stale-While-Revalidate Caching

**Decision:** Serve stale data while refreshing in background

**How it Works:**
```
1. Request comes in
2. Check cache
   ├─→ Fresh (< 30s old): Return immediately
   ├─→ Stale (30s-5min old): Return + refresh in background
   └─→ Expired (> 5min): Fetch fresh data
3. If API fails, serve stale data up to 30 minutes
```

**Why SWR:**
- ✅ Fast responses (always serve from cache if available)
- ✅ Self-healing (background refresh)
- ✅ Graceful degradation (serve stale if API fails)
- ❌ Data may be up to 30 seconds stale

---

### 4. MCC Code Categorization

**Decision:** Hardcoded MCC codes in-memory

**Categorization Rules:**
```csharp
"5411" => "Groceries"
"5812" => "Restaurants"
"5541" => "Transport"
"4900" => "Utilities"
"7995" => "Entertainment"
"5311" => "Shopping"
```

**Why Hardcoded:**
- ✅ Fast (in-memory, no DB queries)
- ✅ Simple to understand and modify
- ✅ Version-controlled
- ❌ Requires code changes to update

**Future:** Externalize to JSON config or database

---

### 5. Result<T> Pattern vs Exceptions

**Decision:** Use Result<T> wrapper for all external calls

**Why Result<T>:**
- ✅ Explicit error handling (no hidden exceptions)
- ✅ Supports warnings (stale cache, partial data)
- ✅ Better for API orchestration
- ✅ Caller controls flow

**Example:**
```csharp
public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
}
```

---

## 📁 Project Structure

```
TransactionAggregationService/
├── Presentation/
│   └── TransactionAggregation.Api/
│       ├── Endpoints/
│       │   ├── TransactionEndpoints.cs
│       │   └── HealthEndpoints.cs
│       ├── Middleware/
│       │   └── CorrelationIdMiddleware.cs
│       ├── Program.cs
│       └── Dockerfile
│
├── Core/
│   └── TransactionAggregation.Core/
│       ├── Services/
│       │   ├── ITransactionAggregationService.cs
│       │   ├── TransactionAggregationService.cs
│       │   ├── ICategorizationService.cs
│       │   └── CategorizationService.cs
│       └── Models/
│           ├── AggregatedTransactionResponse.cs
│           └── TransactionSummaryResponse.cs
│
├── Infrastructure/
│   └── TransactionAggregation.Infrastructure/
│       ├── Outbox/
│       │   ├── IOutboxPublisher.cs
│       │   ├── OutboxPublisher.cs
│       │   └── OutboxRepository.cs
│       ├── BackgroundServices/
│       │   └── OutboxProcessorService.cs
│       ├── Clients/
│       │   ├── IFraudEngineClient.cs
│       │   ├── FraudEngineClient.cs
│       │   ├── ICustomerApiClient.cs
│       │   └── ITransactionApiClient.cs
│       ├── Caching/
│       │   ├── ICacheService.cs
│       │   └── CacheService.cs
│       └── Resilience/
│           └── ResiliencePolicyFactory.cs
│
├── Models/
│   └── TransactionAggregation.Models/
│       ├── Common/
│       │   └── Result.cs
│       └── Contracts/
│           ├── TransactionResponse.cs
│           └── CustomerResponse.cs
│
└── Tests/
    ├── Unit/
    └── Integration/
```

---

## 🔧 Troubleshooting

### Issue: Outbox Messages Stuck in "Pending"

**Check 1: Fraud Engine is Running**
```bash
curl http://localhost:5160/health
```

**Check 2: Background Worker is Running**
```bash
# Check logs for:
# "Outbox Processor started - Polling every 1s"
```

**Manual Retry:**
```bash
sqlite3 transactionaggregation.db << EOF
UPDATE OutboxMessages 
SET Status = 'Pending', AttemptCount = 0, LastError = NULL 
WHERE Status = 'Failed';
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
```

---

### Issue: Database Locked

**Solution:**
```bash
# Stop all services
# Delete lock files
rm *.db-wal *.db-shm

# Restart services
dotnet run
```

---

### Issue: Mock Services Not Responding

**Solution:**
```bash
# Verify mock services are running
curl http://localhost:5160/health

# Check docker-compose network
docker-compose ps

# Restart mock services
docker-compose restart customer-mock transaction-mock account-mock
```