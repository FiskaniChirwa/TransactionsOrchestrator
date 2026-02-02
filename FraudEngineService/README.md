# Fraud Engine Service

A production-ready fraud detection microservice built with .NET 8. Analyzes banking transactions in real-time using configurable rule-based algorithms with guaranteed message delivery via the Outbox pattern.

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

### What is Fraud Engine?

A **rule-based fraud detection microservice** that:
- Analyzes transactions in real-time using 5 configurable rules
- Calculates risk scores and determines transaction status
- Ensures idempotent processing via event IDs
- Stores fraud alerts and transaction history
- Provides comprehensive fraud analytics

### Fraud Detection Rules

| Rule | Description | Risk Score | Example |
|------|-------------|------------|---------|
| **HighAmountRule** | Detects amounts exceeding category thresholds | 30 | Groceries > R5,000 |
| **VelocityRule** | Detects rapid repeat transactions | 35 | 6+ transactions in 60 min |
| **UnusualTimeRule** | Flags transactions at unusual hours | 15 | Shopping at 2 AM |
| **FirstTimeCategoryRule** | First-time category usage | 10 | First crypto transaction |
| **HighRiskCategoryRule** | High-risk merchant categories | 40 | Cryptocurrency, Gambling |

### Key Capabilities

- ✅ Real-time fraud detection using rule-based algorithms
- ✅ Idempotent processing to prevent duplicate analysis
- ✅ Risk score calculation with configurable thresholds
- ✅ Comprehensive health monitoring
- ✅ SQLite database for alerts and history
- ✅ Correlation ID-based request tracing

---

## 🏗️ Architecture

### High-Level Architecture
```
┌─────────────────────────────────┐
│  Transaction Aggregation API    │
└──────────────┬──────────────────┘
               │ (async, via Outbox)
               ▼
┌─────────────────────────────────────────┐
│  Fraud Engine API (Port 5160)           │
│  ┌───────────────────────────────────┐  │
│  │ 1. Check Idempotency (EventId)   │  │
│  │ 2. Run Fraud Detection Rules     │  │
│  │ 3. Calculate Risk Score          │  │
│  │ 4. Store Alert & History         │  │
│  │ 5. Return Analysis Result        │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### Layered Architecture
```
┌─────────────────────────────────────┐
│  Presentation Layer (API)           │
│  - FraudEndpoints                   │
│  - Health Checks                    │
│  - Middleware (Idempotency)         │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Core Layer (Business Logic)        │
│  - Fraud Rules (5 implementations)  │
│  - FraudAnalysisService             │
│  - RiskCalculator                   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Infrastructure Layer               │
│  - Repositories                     │
│  - Rule Configuration               │
│  - EF Core Data Access              │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Data Layer                         │
│  - EF Core Context                  │
│  - Fraud Entities                   │
│  - SQLite Database                  │
└─────────────────────────────────────┘
```

### Request Flow
```
1. POST /api/fraud/analyze
   Header: X-Event-Id: {unique-id}
   ↓
2. Check ProcessedEvents table for EventId
   ├─→ If exists: Return cached result (idempotent)
   └─→ If new: Continue processing
   ↓
3. Run all 5 fraud detection rules
   ↓
4. Calculate total risk score
   ↓
5. Determine status (Clear/Flagged/Blocked)
   ├─→ 0-39: Clear
   ├─→ 40-69: Flagged
   └─→ 70-100: Blocked
   ↓
6. Store in FraudAlerts table
   ↓
7. Store in TransactionHistory table
   ↓
8. Store EventId in ProcessedEvents table
   ↓
9. Return analysis result
```

---

## 🛠️ Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | .NET | 8.0 | Modern C# framework |
| **API Style** | ASP.NET Core Minimal APIs | 8.0 | Lightweight endpoints |
| **Database** | SQLite + EF Core | 8.0 | Embedded database |
| **Logging** | Serilog | 8.0.0 | Structured logging |
| **API Docs** | Swagger/OpenAPI | 8.0 | Interactive documentation |
| **Health Checks** | ASP.NET Health Checks | 8.0 | Monitoring |

---

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (optional)
- Port 5160 available

### Local Development

```bash
# 1. Navigate to API project
cd FraudEngineService/Presentation/FraudEngine.Api

# 2. Restore dependencies
dotnet restore

# 3. Run the API (database auto-initializes)
dotnet run

# Output: Now listening on: http://localhost:5160
```

### Docker Deployment

```bash
# Build and run with docker-compose
docker-compose up fraud-api

# Or build standalone
docker build -f FraudEngineService/Presentation/FraudEngine.Api/Dockerfile -t fraud-engine-api .
docker run -p 5160:8080 fraud-engine-api
```

### Verify Installation

```bash
# Check health
curl http://localhost:5160/health

# Open Swagger UI
open http://localhost:5160/swagger
```

---

## 📡 API Reference

### Base URL
- **Local:** `http://localhost:5160`
- **Docker:** `http://localhost:5160`

---

### 1. Analyze Transaction

**Request:**
```http
POST /api/fraud/analyze
Content-Type: application/json
X-Event-Id: {unique-event-id}

{
  "transactionId": 100074,
  "customerId": 500,
  "accountId": 1001,
  "amount": 450.00,
  "currency": "ZAR",
  "merchantName": "WOOLWORTHS CAPE TOWN",
  "merchantCode": "5411",
  "merchantCategory": "Groceries",
  "transactionDate": "2024-01-15T14:30:00Z",
  "transactionType": "Debit"
}
```

**Response (200 OK - Low Risk):**
```json
{
  "transactionId": 100074,
  "customerId": 500,
  "riskScore": 10,
  "status": "Clear",
  "reason": "First time in category",
  "rulesTriggered": ["FirstTimeCategory"],
  "timestamp": "2024-01-29T15:30:00Z"
}
```

**Response (200 OK - High Risk):**
```json
{
  "transactionId": 100075,
  "customerId": 500,
  "riskScore": 85,
  "status": "Blocked",
  "reason": "High amount for category; Unusual time of day; High-risk category",
  "rulesTriggered": ["HighAmountForCategory", "UnusualTimeTransaction", "HighRiskCategory"],
  "timestamp": "2024-01-29T02:15:00Z"
}
```

**Risk Score Thresholds:**
- `0-39`: **Clear** (Low risk, allow)
- `40-69`: **Flagged** (Medium risk, review)
- `70-100`: **Blocked** (High risk, deny)

---

### 2. Get Fraud Alerts

**Request:**
```http
GET /api/fraud/alerts?customerId={id}&fromDate={date}&toDate={date}&page={page}&pageSize={size}
```

**Query Parameters:**
- `customerId` (optional): Filter by customer
- `fromDate` (optional): Filter start date (ISO 8601)
- `toDate` (optional): Filter end date (ISO 8601)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)

**Example:**
```bash
curl "http://localhost:5160/api/fraud/alerts?customerId=500&page=1&pageSize=10"
```

**Response (200 OK):**
```json
{
  "alerts": [
    {
      "id": 1,
      "transactionId": 100075,
      "customerId": 500,
      "riskScore": 85,
      "status": "Blocked",
      "rulesTriggered": ["HighAmountForCategory", "HighRiskCategory"],
      "reason": "High amount for category; High-risk category",
      "createdAt": "2024-01-29T02:15:00Z"
    }
  ],
  "pageInfo": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
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
  "timestamp": "2024-01-29T15:00:00Z",
  "service": "FraudEngine.API",
  "checks": {
    "database": {
      "status": "Healthy",
      "description": "Database connection is healthy"
    }
  }
}
```

---

## ⚖️ Design Decisions

### 1. Fixed Rule Weights vs Machine Learning

**Decision:** Fixed weights per rule

| Rule | Weight | Reasoning |
|------|--------|-----------|
| HighRiskCategory | 40 | Crypto/gambling inherently risky |
| VelocityCheck | 35 | Card testing pattern |
| HighAmountForCategory | 30 | Context-dependent risk |
| UnusualTimeTransaction | 15 | Weak signal alone |
| FirstTimeCategory | 10 | Learning behavior |

**Why Fixed Weights:**
- ✅ Transparent and explainable
- ✅ Easy to tune and audit
- ✅ Deterministic results
- ✅ No training data required
- ⚠️ Less adaptive than ML

**Future Enhancement:** Train ML model on historical fraud data

---

### 2. Idempotency via X-Event-Id Header

**Decision:** Use HTTP header for event ID (not request body)

**Why Header:**
- ✅ Event ID is infrastructure concern, not domain data
- ✅ Easier to enforce (middleware can validate)
- ✅ Standard pattern (follows CloudEvents spec)
- ✅ Prevents body tampering

**Implementation:**
```csharp
var eventId = context.Request.Headers["X-Event-Id"].FirstOrDefault();

var processed = await _processedEventRepo.ExistsAsync(eventId);
if (processed)
{
    // Return cached result
    return await _processedEventRepo.GetResultAsync(eventId);
}
```

---

### 3. SQLite vs PostgreSQL

**Decision:** SQLite for fraud alerts and history

**Trade-offs:**

| Aspect | SQLite | PostgreSQL |
|--------|--------|------------|
| **Setup** | ✅ Zero config | ❌ Installation required |
| **Portability** | ✅ Single file | ❌ Server process |
| **Concurrency** | ⚠️ Limited writes | ✅ High concurrency |
| **Production** | ⚠️ Limited scale | ✅ Enterprise-grade |

**When to Migrate:**
- High write concurrency (100+ writes/second)
- Multi-server deployment
- Advanced features (replication, partitioning)

---

### 4. Synchronous Processing

**Decision:** Process fraud analysis synchronously

**Why Synchronous:**
- ✅ Immediate fraud decision
- ✅ Simple implementation
- ✅ Fast processing (< 100ms per transaction)
- ❌ Blocks request thread

**Alternative (Async):**
- Transaction Aggregation uses Outbox pattern
- Sends transactions to Fraud Engine asynchronously
- Best of both worlds: fast API response + reliable delivery

---

### 5. Rule-Based vs ML Model

**Decision:** Rule-based fraud detection

**Why Rules:**
- ✅ Explainable - can show why transaction was flagged
- ✅ Deterministic - same input always gives same output
- ✅ Easy to tune - adjust weights without retraining
- ✅ No training data required
- ❌ Less adaptive than ML

**When to Use ML:**
- Have historical fraud data
- Need anomaly detection
- Want dynamic risk scoring

---

## 📁 Project Structure

```
FraudEngineService/
├── Presentation/
│   └── FraudEngine.Api/
│       ├── Endpoints/
│       │   └── FraudEndpoints.cs
│       ├── HealthChecks/
│       │   └── DatabaseHealthCheck.cs
│       ├── Program.cs
│       └── Dockerfile
│
├── Core/
│   └── FraudEngine.Core/
│       ├── Models/
│       │   ├── Transaction.cs
│       │   ├── Alert.cs
│       │   └── RuleResult.cs
│       ├── Rules/
│       │   ├── IFraudRule.cs
│       │   ├── HighAmountRule.cs
│       │   ├── VelocityRule.cs
│       │   ├── UnusualTimeRule.cs
│       │   ├── FirstTimeCategoryRule.cs
│       │   └── HighRiskCategoryRule.cs
│       ├── Services/
│       │   ├── IFraudAnalysisService.cs
│       │   ├── FraudAnalysisService.cs
│       │   └── RiskCalculator.cs
│       └── Repositories/
│           ├── IFraudAlertRepository.cs
│           ├── ITransactionHistoryRepository.cs
│           └── IProcessedEventRepository.cs
│
├── Infrastructure/
│   └── FraudEngine.Infrastructure/
│       ├── Data/
│       │   ├── FraudEngineDbContext.cs
│       │   ├── Entities/
│       │   └── Configurations/
│       ├── Repositories/
│       │   ├── FraudAlertRepository.cs
│       │   ├── TransactionHistoryRepository.cs
│       │   └── ProcessedEventRepository.cs
│       └── Configuration/
│           └── RuleConfiguration.cs
│
└── Tests/
    └── FraudEngine.Tests/
        ├── Unit/
        │   ├── Rules/
        │   └── Services/
        └── Integration/
```

---

## 🔧 Troubleshooting

### Issue: Fraud Engine Returns 500 Error

**Symptom:**
```
SQLite Error 19: 'NOT NULL constraint failed'
```

**Solution:**
```bash
# Stop Fraud Engine
# Delete database
rm fraudengine.db

# Restart Fraud Engine (auto-recreates)
dotnet run
```

---

### Issue: Duplicate Processing Despite Event ID

**Solution:**
```bash
# Check ProcessedEvents table
sqlite3 fraudengine.db "SELECT EventId, CreatedAt FROM ProcessedEvents WHERE EventId = 'your-event-id';"

# Ensure X-Event-Id header is being sent
curl -H "X-Event-Id: test-123" -X POST http://localhost:5160/api/fraud/analyze ...
```

---

### Issue: All Transactions Flagged

**Solution:**
```json
// Check rule configuration in appsettings.json
{
  "RuleConfiguration": {
    "CategoryAmountThresholds": {
      "Groceries": 5000,  // Increase if needed
      "Shopping": 10000
    }
  }
}
```

---

### Issue: Database Locked

**Solution:**
```bash
# Stop all services
# Delete lock files
rm fraudengine.db-shm fraudengine.db-wal

# Restart service
dotnet run
```