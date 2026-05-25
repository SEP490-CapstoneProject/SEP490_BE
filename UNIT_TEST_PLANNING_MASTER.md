# COMPREHENSIVE UNIT TEST PLANNING DOCUMENT
## Capstone Project - SEP490 Backend Services

**Generated:** 2026-05-25 16:06:41
**Framework:** xUnit with Moq
**Scope:** All Critical and High-Priority Services

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Service Analysis Overview](#service-analysis-overview)
3. [Critical Services (Phase 1)](#critical-services-phase-1)
   - Auth Service
   - Application Service
   - Payment Service
   - Portfolio Service
   - Subscription Service
4. [High Priority Services (Phase 2)](#high-priority-services-phase-2)
   - Challenge Service
   - Community Service
   - Company Service
   - Connection Service
   - Notification Service
   - UserProfile Service
5. [Test Case Matrix](#test-case-matrix)
6. [Mocking Strategy](#mocking-strategy)

---

## Executive Summary

This document provides a comprehensive unit test planning guide for the Capstone backend project (SEP490). The analysis identified **15 services** across the microservices architecture with the following breakdown:

### Service Priority Classification
- **CRITICAL (5 services)**: Auth, Application, Payment, Portfolio, Subscription
- **HIGH (6 services)**: Challenge, Community, Company, Connection, Notification, UserProfile
- **MEDIUM (2 services)**: Interview, Realtime
- **LOW (2 services)**: Media, Moderation (not yet implemented)

### Total Functions to Test
- **Critical Services**: ~35 core business logic functions
- **High Priority Services**: ~45 core business logic functions
- **Total Test Cases Expected**: 250+ (3-4 test cases per function)

### Testing Framework
- **Unit Testing**: xUnit
- **Mocking**: Moq
- **Database**: Entity Framework Core with In-Memory DB for tests
- **Architecture Pattern**: Clean Architecture (API, Application, Domain, Infrastructure layers)

---

## Service Analysis Overview

### Architecture Pattern
All services follow **Clean Architecture** with 4-layer structure:
1. **API Layer** - Controllers and HTTP handling
2. **Application Layer** - Business logic and services
3. **Domain Layer** - Entities and business rules
4. **Infrastructure Layer** - Database and external integrations

### Service Locations
- Root: D:\Capstone\src\Services\
- Each service has structure: {ServiceName}\{ServiceName}.API\, {ServiceName}.Application\, {ServiceName}.Domain\, {ServiceName}.Infrastructure\

### API Response Format
All services use standardized response wrapper:
\\\csharp
public class ApiResponse<T>
{
    public T Data { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}
\\\

---


## CRITICAL SERVICES (PHASE 1)

= provided title
  - Portfolio.Blocks.Count = 3
  - All blocks have data loaded
  - Response includes CreatedAt and UpdatedAt

#### Test 2: Happy Path - Get Own Portfolio (Owner Access)
- **Test Name**: `Should_ReturnPortfolio_When_OwnerAccesses`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Portfolio.EmployeeId = 1
  - Portfolio.IsPublic = false (private)
  - Current user employeeId = 1
- **Expected Output**:
  - Response status code = 200
  - Portfolio returned even if not public
  - All blocks visible

#### Test 3: Not Found Error
- **Test Name**: `Should_ReturnNotFound_When_PortfolioNotExists`
- **Input**: portfolioId = 999
- **Expected Error**: HTTP 404
- **Error Message**: "Portfolio 999 not found"

#### Test 4: Authorization Error - Private Portfolio
- **Test Name**: `Should_ReturnForbidden_When_PrivatePortfolioAndNotOwner`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Portfolio.IsPublic = false
  - Portfolio.EmployeeId = 2
  - Current user employeeId = 1
- **Expected Error**: HTTP 403
- **Error Message**: "Not authorized to view this portfolio"

### Mocking Requirements

- **Mock IPortfolioRepository.GetByIdAsync()**:
  - Return Portfolio entity with blocks loaded
  - Return null if not found

- **Mock ICurrentUserService**:
  - GetEmployeeId() returns current user's employee ID
  - GetUserId() returns current user's user ID

---

## Function: UpdateBlockAsync

**API Endpoint**: `PUT /api/portfolios/{portfolioId}/blocks/{blockId}`

**Purpose**: Update portfolio block content, variant, display order, and visibility. Handles file uploads for blocks.

### Input Parameters
- `portfolioId` (int): Portfolio ID
- `blockId` (int): Block ID to update
- `employeeId` (int): Current user's employee ID (ownership validation)
- `request` (UpdateBlockRequest): Block data to update
  - `data` (object): Block-specific content
  - `variant` (string, optional): Visual variant
  - `displayOrder` (int, optional): Display position
  - `isVisible` (bool, optional): Visibility toggle
- `fileMap` (Dictionary<string, IFormFile>): Uploaded files keyed by field name

### Success Response (HTTP 200)
```json
{
  "id": 1,
  "blockTypeCode": "EXPERIENCE",
  "blockTypeName": "Experience",
  "variant": "timeline",
  "displayOrder": 2,
  "isVisible": true,
  "data": {
    "experiences": [
      {
        "company": "Tech Corp",
        "position": "Senior Developer",
        "startDate": "2022-01-15",
        "endDate": "2024-01-15",
        "description": "Led development team"
      }
    ]
  }
}
```

### Error Responses

**HTTP 404 - Portfolio Not Found**
```json
{
  "error": "Portfolio 1 not found"
}
```

**HTTP 404 - Block Not Found**
```json
{
  "error": "Block 999 not found"
}
```

**HTTP 403 - Not Owner**
```json
{
  "error": "You do not own this portfolio"
}
```

**HTTP 400 - Block Mismatch**
```json
{
  "error": "Block does not belong to this portfolio"
}
```

### Test Cases

#### Test 1: Happy Path - Update Block Content
- **Test Name**: `Should_UpdateBlock_When_ValidDataProvided`
- **Input**: 
  ```json
  {
    "data": {
      "company": "New Company",
      "position": "Principal Engineer",
      "startDate": "2023-06-01",
      "endDate": null,
      "description": "Leading architecture initiatives"
    }
  }
  ```
- **Pre-conditions**:
  - Portfolio exists with Id = 1, EmployeeId = 1
  - Block exists with Id = 1, PortfolioId = 1, Type = EXPERIENCE
  - Current user employeeId = 1
- **Expected Output**:
  - Response status code = 200
  - Block.Data updated with new content
  - Block fields (variant, displayOrder, isVisible) can be updated
  - Handler for block type invoked to process data
  - File uploads processed if included

#### Test 2: Happy Path - Update Display Order
- **Test Name**: `Should_UpdateDisplayOrder_When_OrderProvided`
- **Input**: 
  ```json
  {
    "data": {...},
    "displayOrder": 5
  }
  ```
- **Expected Output**:
  - Block.DisplayOrder = 5
  - Portfolio blocks reordered

#### Test 3: Happy Path - Toggle Visibility
- **Test Name**: `Should_ToggleVisibility_When_IsVisibleProvided`
- **Input**: 
  ```json
  {
    "data": {...},
    "isVisible": false
  }
  ```
- **Expected Output**:
  - Block.IsVisible = false
  - Block hidden from portfolio view

#### Test 4: Authorization Error - Not Portfolio Owner
- **Test Name**: `Should_ReturnForbidden_When_EmployeeNotOwner`
- **Input**: Block data update
- **Pre-conditions**:
  - Portfolio.EmployeeId = 2
  - Current user employeeId = 1
- **Expected Error**: HTTP 403
- **Error Message**: "You do not own this portfolio"

#### Test 5: Validation Error - Block Not Found
- **Test Name**: `Should_ReturnNotFound_When_BlockNotExists`
- **Input**: blockId = 999
- **Expected Error**: HTTP 404
- **Error Message**: "Block 999 not found"

#### Test 6: Validation Error - Block Mismatch
- **Test Name**: `Should_ReturnError_When_BlockNotInPortfolio`
- **Input**: blockId = 5
- **Pre-conditions**:
  - Block 5 exists but belongs to Portfolio 2
  - Request for Portfolio 1
- **Expected Error**: HTTP 400
- **Error Message**: "Block does not belong to this portfolio"

#### Test 7: Edge Case - File Upload Processing
- **Test Name**: `Should_ProcessFileUploads_When_FilesIncluded`
- **Input**: 
  ```
  Block data with file map:
  - "profileImage": IFormFile
  - "certificate": IFormFile
  ```
- **Expected Behavior**:
  - Handler invoked with fileMap
  - Files uploaded to storage service
  - File URLs stored in block data
  - File sizes validated (< 10MB per file)

### Mocking Requirements

- **Mock IPortfolioRepository.GetByIdAsync()**:
  - Return Portfolio entity
  - Return null if not found

- **Mock IBlockRepository.GetByIdWithDataAsync()**:
  - Return PortfolioBlock with data loaded
  - Return null if not found

- **Mock IBlockRepository.UpdateAsync()**:
  - Persist block changes
  - Verify UpdatedAt timestamp updated

- **Mock IEnumerable<IBlockHandler>**:
  - Each block type has corresponding handler
  - Handler.HandleAsync() called with block, data, and files
  - EXPERIENCE handler processes company, position, dates
  - PROJECT handler processes title, description, links
  - SKILL handler processes skill items

- **Mock File Upload Service**:
  - Upload files to cloud storage
  - Return file URLs
  - Validate file types and sizes

---

## Function: GeneratePreviewAsync

**API Endpoint**: `POST /api/portfolios/{id}/preview/generate`

**Purpose**: Generate AI-powered visual preview of portfolio showcasing key information with optional visual theme.

### Input Parameters
- `portfolioId` (int): Portfolio to generate preview for
- `highlightsDescription` (string, optional): Custom highlights to emphasize
- `employeeId` (int): Current user's employee ID (ownership validation)

### Success Response (HTTP 200)
```json
{
  "success": true,
  "message": "Preview generated successfully",
  "data": {
    "previewId": "550e8400-e29b-41d4-a716-446655440000",
    "portfolioId": 1,
    "previewUrl": "https://cloudflare-cdn.example.com/preview_1_v1.jpg",
    "thumbnailUrl": "https://cloudflare-cdn.example.com/preview_1_thumb.jpg",
    "generatedAt": "2024-01-15T10:35:00Z",
    "expiresAt": "2024-02-15T10:35:00Z"
  }
}
```

### Error Responses

**HTTP 404 - Portfolio Not Found**
```json
{
  "success": false,
  "message": "Portfolio 999 not found"
}
```

**HTTP 403 - Not Authorized**
```json
{
  "success": false,
  "message": "Not authorized to generate preview for this portfolio"
}
```

**HTTP 400 - Not Approved**
```json
{
  "success": false,
  "message": "Portfolio must be approved before generating preview. Current status: Pending"
}
```

**HTTP 500 - Generation Failed**
```json
{
  "success": false,
  "message": "Failed to generate preview. Please try again later."
}
```

### Test Cases

#### Test 1: Happy Path - Generate Preview Successfully
- **Test Name**: `Should_GeneratePreview_When_PortfolioApprovedAndOwned`
- **Input**: 
  ```json
  {
    "highlightsDescription": "Senior full-stack developer with 5 years experience"
  }
  ```
- **Pre-conditions**:
  - Portfolio exists with Id = 1
  - Portfolio.EmployeeId = 1
  - Portfolio.ModerationStatus = "Approved"
  - Current user employeeId = 1
  - Google AI Studio available
  - Cloudflare image service available
- **Expected Output**:
  - Response status code = 200
  - Response.Success = true
  - PreviewId is valid GUID
  - PreviewUrl is valid HTTPS URL
  - ThumbnailUrl generated
  - GeneratedAt = current time
  - ExpiresAt = 30 days from now
  - Preview saved to database
  - Preview image cached on Cloudflare

#### Test 2: Happy Path - Generate Without Custom Highlights
- **Test Name**: `Should_GeneratePreview_When_NoHighlightsProvided`
- **Input**: highlightsDescription = null
- **Pre-conditions**:
  - Same as Test 1
- **Expected Output**:
  - Preview generated with portfolio content only
  - AI extracts key highlights from blocks

#### Test 3: Authorization Error - Not Portfolio Owner
- **Test Name**: `Should_ReturnForbidden_When_UserNotOwner`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Portfolio.EmployeeId = 2
  - Current user employeeId = 1
- **Expected Error**: HTTP 403
- **Error Message**: "Not authorized to generate preview for this portfolio"

#### Test 4: Business Logic Error - Portfolio Not Approved
- **Test Name**: `Should_ReturnError_When_PortfolioNotApproved`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Portfolio.ModerationStatus = "Pending"
  - Current user is owner
- **Expected Error**: HTTP 400
- **Error Message**: "Portfolio must be approved before generating preview. Current status: Pending"
- **Assertions**:
  - No preview generation attempted
  - No AI calls made

#### Test 5: Not Found Error
- **Test Name**: `Should_ReturnNotFound_When_PortfolioNotExists`
- **Input**: portfolioId = 999
- **Expected Error**: HTTP 404
- **Error Message**: "Portfolio 999 not found"

#### Test 6: Edge Case - AI Generation Timeout
- **Test Name**: `Should_HandleTimeoutGracefully_When_AIGenerationSlow`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Google AI Studio takes > 30 seconds
- **Expected Error**: HTTP 500
- **Error Message**: "Failed to generate preview. Please try again later."
- **Assertions**:
  - Timeout caught and logged
  - User-friendly error returned

#### Test 7: Edge Case - Image Generation Failure
- **Test Name**: `Should_ReturnError_When_ImageGenerationFails`
- **Input**: portfolioId = 1
- **Pre-conditions**:
  - Google AI Studio returns valid prompt
  - Cloudflare service unavailable
- **Expected Error**: HTTP 500
- **Error Message**: "Failed to generate preview. Please try again later."

### Mocking Requirements

- **Mock IPortfolioRepository.GetByIdAsync()**:
  - Return Portfolio with blocks and all content
  - Portfolio.ModerationStatus = "Approved"
  - Return null if not found

- **Mock GoogleAiPreviewGenerator**:
  - GeneratePromptAsync() returns visual prompt
  - Returns structured prompt with portfolio sections
  - Timeout: 30 seconds

- **Mock VisualPromptService**:
  - ProcessPortfolioData() extracts key information
  - Returns formatted prompt for image generation

- **Mock ImageGenerationService**:
  - GenerateImageAsync(prompt) returns image bytes
  - Upload to Cloudflare and return URL
  - Image dimensions: 1200x630px (preview), 400x400px (thumbnail)

- **Mock IPortfolioPreviewRepository**:
  - CreateAsync() stores preview record
  - Return preview with ID and URLs

- **Mock Cloudflare CDN Service**:
  - UploadImageAsync() returns public URL
  - URL format: https://cdn.example.com/preview_{id}_{version}.jpg

- **Mock ICurrentUserService**:
  - GetEmployeeId() and UserId
  - IsAdmin() for override permissions

---

# SUBSCRIPTION SERVICE

## Service Overview

**Purpose**: Manages subscription plans, user subscriptions, upgrades, cancellations, and entitlement tracking.

**Key Responsibilities**:
- Retrieve subscription plans
- Create new subscriptions (pending payment)
- Upgrade existing subscriptions
- Cancel subscriptions
- Track subscription status and payment status
- Manage entitlements and feature access
- Redis caching for performance
- Audit logging of subscription changes
- Auto-renewal handling

**Main API Endpoints**:
- `GET /api/subscriptions/plans` - Get available plans
- `GET /api/subscriptions/plans/{id}` - Get plan details
- `POST /api/subscriptions/subscribe` - Create subscription
- `POST /api/subscriptions/upgrade` - Upgrade subscription
- `POST /api/subscriptions/cancel` - Cancel subscription
- `GET /api/subscriptions/current` - Get current subscription

---

## Function: GetAllPlansAsync

**API Endpoint**: `GET /api/subscriptions/plans`

**Purpose**: Retrieve all active subscription plans with features and pricing.

### Input Parameters
- None

### Success Response (HTTP 200)
```json
[
  {
    "id": 1,
    "name": "Basic",
    "description": "For job seekers starting out",
    "price": 99000,
    "currency": "VND",
    "billingCycle": "Monthly",
    "features": [
      {
        "name": "MAX_APPLY",
        "value": "5",
        "description": "Apply to 5 jobs per month"
      },
      {
        "name": "PORTFOLIO_LIMIT",
        "value": "1",
        "description": "Create 1 portfolio"
      }
    ],
    "isActive": true
  },
  {
    "id": 2,
    "name": "Premium",
    "description": "For serious job hunters",
    "price": 299000,
    "currency": "VND",
    "billingCycle": "Monthly",
    "features": [
      {
        "name": "MAX_APPLY",
        "value": "25",
        "description": "Apply to 25 jobs per month"
      }
    ],
    "isActive": true
  }
]
```

### Test Cases

#### Test 1: Happy Path - Get All Plans
- **Test Name**: `Should_ReturnAllActivePlans_When_PlansExist`
- **Input**: None
- **Pre-conditions**:
  - 3 active plans exist in database
  - Each plan has 2-5 features
- **Expected Output**:
  - Response status code = 200
  - Response.Length >= 1
  - All returned plans have IsActive = true
  - Each plan includes features array
  - Plans sorted by price or priority

#### Test 2: Happy Path - Empty Plans
- **Test Name**: `Should_ReturnEmptyList_When_NoActivePlans`
- **Input**: None
- **Pre-conditions**:
  - No active plans exist (all disabled)
- **Expected Output**:
  - Response status code = 200
  - Response = []

### Mocking Requirements

- **Mock IPlanRepository.GetAllActiveAsync()**:
  - Return list of Plan entities
  - Return empty list if no active plans

---

## Function: SubscribeAsync

**API Endpoint**: `POST /api/subscriptions/subscribe`

**Purpose**: Create new subscription for user. Validates plan exists, checks for duplicate active subscriptions, and creates pending subscription record.

### Input Parameters
- `userId` (int): User subscribing (from JWT)
- `request` (SubscribeRequest):
  - `planId` (int): Plan to subscribe to
  - `autoRenew` (bool, optional): Enable auto-renewal (default: true)

### Success Response (HTTP 200)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": 1,
  "planId": 1,
  "planName": "Basic",
  "status": "Pending",
  "paymentStatus": "Pending",
  "startDate": "2024-01-15T10:30:00Z",
  "endDate": "2024-02-15T10:30:00Z",
  "autoRenew": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "message": "Subscription created. Please proceed to payment."
}
```

### Error Responses

**HTTP 400 - Plan Not Found**
```json
{
  "error": "Plan not found"
}
```

**HTTP 409 - Active Subscription Exists**
```json
{
  "error": "User already has an active subscription. Use upgrade instead."
}
```

### Test Cases

#### Test 1: Happy Path - Create New Subscription
- **Test Name**: `Should_CreateSubscription_When_NoPreviousActive`
- **Input**: 
  ```json
  {
    "planId": 1,
    "autoRenew": true
  }
  ```
- **Pre-conditions**:
  - Plan 1 exists with BillingCycle = Monthly
  - User has no active subscription
  - No pending subscription for same plan
- **Expected Output**:
  - Response status code = 200
  - Subscription.Id = GUID
  - Subscription.Status = "Pending"
  - Subscription.PaymentStatus = "Pending"
  - Subscription.StartDate = UtcNow
  - Subscription.EndDate = UtcNow + 1 month (for monthly plan)
  - Subscription.AutoRenew = true
  - User redirected to payment flow
  - Log message: "Created subscription {SubscriptionId} for user {UserId}, plan {PlanId}. Awaiting payment."

#### Test 2: Happy Path - Reuse Pending Subscription
- **Test Name**: `Should_ReturnPendingSubscription_When_SamePlanPending`
- **Input**: 
  ```json
  {
    "planId": 1,
    "autoRenew": true
  }
  ```
- **Pre-conditions**:
  - Pending subscription already exists for user + plan 1
  - Not yet paid
- **Expected Output**:
  - Response status code = 200
  - Same subscription ID returned
  - User redirected to payment with same payment link
  - Log message: "Returning existing pending subscription"

#### Test 3: Business Logic Error - Active Subscription Exists
- **Test Name**: `Should_ReturnError_When_ActiveSubscriptionExists`
- **Input**: 
  ```json
  {
    "planId": 2,
    "autoRenew": true
  }
  ```
- **Pre-conditions**:
  - User has active subscription to Plan 1
  - Subscription.Status = Active
- **Expected Error**: HTTP 409
- **Error Message**: "User already has an active subscription. Use upgrade instead."
- **Assertions**:
  - No new subscription created
  - User directed to upgrade flow instead

#### Test 4: Validation Error - Plan Not Found
- **Test Name**: `Should_ReturnError_When_PlanNotFound`
- **Input**: 
  ```json
  {
    "planId": 999
  }
  ```
- **Expected Error**: HTTP 400
- **Error Message**: "Plan not found"

#### Test 5: Edge Case - Annual Billing Cycle
- **Test Name**: `Should_CalculateCorrectEndDate_When_YearlyPlan`
- **Input**: 
  ```json
  {
    "planId": 3
  }
  ```
- **Pre-conditions**:
  - Plan 3 exists with BillingCycle = Yearly
- **Expected Output**:
  - Subscription.EndDate = UtcNow + 1 year (365 days)

### Mocking Requirements

- **Mock IPlanRepository.GetByIdWithFeaturesAsync()**:
  - Return Plan with features loaded
  - Return null if not found
  - Plan 1: Monthly, Price 99000
  - Plan 2: Monthly, Price 299000
  - Plan 3: Yearly, Price 999000

- **Mock ISubscriptionRepository.GetActiveByUserIdAsync()**:
  - Return active subscription if exists
  - Return null if no active subscription

- **Mock ISubscriptionRepository.GetPendingByUserAndPlanAsync()**:
  - Return pending subscription if exists
  - Return null otherwise

- **Mock ISubscriptionRepository.CreateAsync()**:
  - Generate GUID for subscription ID
  - Persist with Status = Pending, PaymentStatus = Pending
  - Return created entity

---

## Function: UpgradeAsync

**API Endpoint**: `POST /api/subscriptions/upgrade`

**Purpose**: Upgrade user from current plan to higher tier. Updates plan, invalidates cache, and publishes upgrade event.

### Input Parameters
- `userId` (int): User upgrading
- `request` (UpgradeRequest):
  - `newPlanId` (int): Target plan ID

### Success Response (HTTP 200)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": 1,
  "planId": 2,
  "planName": "Premium",
  "status": "Active",
  "paymentStatus": "Completed",
  "startDate": "2024-01-15T10:30:00Z",
  "endDate": "2024-02-15T10:30:00Z",
  "autoRenew": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "message": "Upgraded to Premium plan"
}
```

### Error Responses

**HTTP 400 - No Active Subscription**
```json
{
  "error": "No active subscription to upgrade"
}
```

**HTTP 400 - Plan Not Found**
```json
{
  "error": "New plan not found"
}
```

### Test Cases

#### Test 1: Happy Path - Upgrade to Higher Plan
- **Test Name**: `Should_UpgradePlan_When_ActiveSubscriptionExists`
- **Input**: 
  ```json
  {
    "newPlanId": 2
  }
  ```
- **Pre-conditions**:
  - User has active subscription to Plan 1 (Basic)
  - New Plan 2 (Premium) exists with higher tier
  - Subscription.Status = Active
- **Expected Output**:
  - Response status code = 200
  - Subscription.PlanId updated from 1 to 2
  - Subscription.UpdatedAt = UtcNow
  - Subscription.Status remains Active
  - Redis cache invalidated for user
  - SubscriptionUpgradedEvent published with OldPlanId = 1, NewPlanId = 2
  - Audit log created: "User upgraded from Basic to Premium"
  - Entitlements refreshed in Redis with new plan features

#### Test 2: Happy Path - Downgrade to Lower Plan
- **Test Name**: `Should_DowngradePlan_When_HigherPlanCurrently`
- **Input**: 
  ```json
  {
    "newPlanId": 1
  }
  ```
- **Pre-conditions**:
  - User has active subscription to Plan 2 (Premium)
  - Plan 1 (Basic) exists
- **Expected Output**:
  - Response status code = 200
  - Subscription.PlanId = 1
  - SubscriptionDowngradedEvent published
  - Entitlements reduced accordingly

#### Test 3: Business Logic Error - No Active Subscription
- **Test Name**: `Should_ReturnError_When_NoActiveSubscription`
- **Input**: 
  ```json
  {
    "newPlanId": 2
  }
  ```
- **Pre-conditions**:
  - User has no active subscription
  - Status is Cancelled or Pending
- **Expected Error**: HTTP 400
- **Error Message**: "No active subscription to upgrade"

#### Test 4: Validation Error - New Plan Not Found
- **Test Name**: `Should_ReturnError_When_NewPlanNotFound`
- **Input**: 
  ```json
  {
    "newPlanId": 999
  }
  ```
- **Pre-conditions**:
  - User has active subscription to Plan 1
- **Expected Error**: HTTP 400
- **Error Message**: "New plan not found"

#### Test 5: Edge Case - Upgrade to Same Plan
- **Test Name**: `Should_AllowSamePlanUpgrade_WhenRequested`
- **Input**: 
  ```json
  {
    "newPlanId": 1
  }
  ```
- **Pre-conditions**:
  - User already on Plan 1
- **Expected Output**:
  - Response status code = 200
  - Subscription updated (refreshed)
  - Can use for renewing expiring subscription

### Mocking Requirements

- **Mock ISubscriptionRepository.GetActiveByUserIdAsync()**:
  - Return active UserSubscription
  - Return null if none

- **Mock IPlanRepository.GetByIdWithFeaturesAsync()**:
  - Return Plan with all features

- **Mock ISubscriptionRepository.UpdateAsync()**:
  - Update PlanId and UpdatedAt
  - Persist change

- **Mock IRedisService.InvalidateUserCacheAsync()**:
  - Clear user's cached entitlements
  - Verify called with correct userId

- **Mock WriteEntitlementsToRedisAsync()**:
  - Write new plan features to Redis
  - Feature keys: MAX_APPLY, PORTFOLIO_LIMIT, etc.
  - TTL: 24 hours

- **Mock IRabbitMQPublisher**:
  - PublishAsync(SubscriptionUpgradedEvent)
  - Event contains UserId, SubscriptionId, OldPlanId, NewPlanId, UpgradedAt

- **Mock IAuditLogService**:
  - LogAsync(userId, "SUBSCRIPTION_UPGRADED", details)

---

## Function: GetPlansAsync (by role)

**API Endpoint**: `GET /api/subscriptions/plans?role=USER`

**Purpose**: Retrieve subscription plans filtered by user role (USER vs RECRUITER have different plans).

### Input Parameters
- `role` (string): User role (USER, RECRUITER)

### Success Response (HTTP 200)
```json
[
  {
    "id": 1,
    "name": "Starter",
    "description": "Perfect for job seekers",
    "price": 99000,
    "billingCycle": "Monthly",
    "features": [...],
    "role": "USER"
  }
]
```

### Test Cases

#### Test 1: Happy Path - Get USER Plans
- **Test Name**: `Should_ReturnUserPlans_When_RoleIsUSER`
- **Input**: role = "USER"
- **Pre-conditions**:
  - 3 plans exist for USER role
  - 2 plans exist for RECRUITER role
- **Expected Output**:
  - Response status code = 200
  - Only USER plans returned
  - Response.Length = 3

#### Test 2: Happy Path - Get RECRUITER Plans
- **Test Name**: `Should_ReturnRecruiterPlans_When_RoleIsRECRUITER`
- **Input**: role = "RECRUITER"
- **Expected Output**:
  - Response status code = 200
  - Only RECRUITER plans returned
  - Response.Length = 2

### Mocking Requirements

- **Mock IPlanRepository.GetActiveByRoleAsync()**:
  - Return plans filtered by role
  - Return empty list if no plans for role

---

## Function: CancelSubscriptionAsync

**API Endpoint**: `POST /api/subscriptions/cancel`

**Purpose**: Cancel active subscription. Sets status to Cancelled and publishes cancellation event.

### Input Parameters
- `userId` (int): User cancelling
- `reason` (string, optional): Cancellation reason

### Success Response (HTTP 200)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Cancelled",
  "cancelledAt": "2024-01-15T10:30:00Z",
  "message": "Subscription cancelled successfully"
}
```

### Error Responses

**HTTP 400 - No Active Subscription**
```json
{
  "error": "No active subscription to cancel"
}
```

### Test Cases

#### Test 1: Happy Path - Cancel Subscription
- **Test Name**: `Should_CancelSubscription_When_ActiveSubscriptionExists`
- **Input**: reason = "Too expensive"
- **Pre-conditions**:
  - User has active subscription
  - Subscription.Status = Active
- **Expected Output**:
  - Response status code = 200
  - Subscription.Status = Cancelled
  - Subscription.CancelledAt = UtcNow
  - SubscriptionCancelledEvent published
  - Entitlements revoked/downgraded
  - Audit log created

#### Test 2: Business Logic Error - No Active Subscription
- **Test Name**: `Should_ReturnError_When_NoActiveSubscription`
- **Input**: reason = "..."
- **Pre-conditions**:
  - User has no active subscription
- **Expected Error**: HTTP 400
- **Error Message**: "No active subscription to cancel"

### Mocking Requirements

- **Mock ISubscriptionRepository.GetActiveByUserIdAsync()**:
  - Return active subscription
  - Return null if none

- **Mock ISubscriptionRepository.UpdateAsync()**:
  - Set Status = Cancelled, CancelledAt = UtcNow

- **Mock IRabbitMQPublisher**:
  - PublishAsync(SubscriptionCancelledEvent)

- **Mock IRedisService.InvalidateUserCacheAsync()**:
  - Clear user entitlements

---

## Summary Table: Critical Service Functions

| Service | Function | Endpoint | Priority | Test Cases |
|---------|----------|----------|----------|-----------|
| Auth | RegisterAsync | POST /register | Critical | 4 (1 happy, 3 error) |
| Auth | LoginAsync | POST /login | Critical | 4 (1 happy, 3 error) |
| Auth | RefreshTokenAsync | POST /refresh | Critical | 4 (1 happy, 3 error) |
| Auth | Change


## HIGH PRIORITY SERVICES (PHASE 2)

**Return Type:** `Task<IEnumerable<Message>>`

**Business Logic:**
1. Validate room exists
2. Query all messages for room ordered by CreatedAt ASC (oldest first)
3. Return message list
4. Optional: Load with sender info enrichment

**Database Operations:**
```sql
SELECT *
FROM Message
WHERE MessageRoomId = @roomId
ORDER BY CreatedAt ASC
```

**Example Response:**
```json
{
  "messages": [
    {
      "id": 2001,
      "messageRoomId": 501,
      "userId": 123,
      "content": "Hi, how are you?",
      "createdAt": "2025-06-15T15:35:00+07:00",
      "status": 1
    },
    {
      "id": 2002,
      "messageRoomId": 501,
      "userId": 456,
      "content": "I'm doing well, thanks!",
      "createdAt": "2025-06-15T15:36:00+07:00",
      "status": 1
    }
  ]
}
```

---

### Test Coverage Plan

#### CreateConnection Tests

**Happy Path:**
```gherkin
Scenario: Successfully create connection request
  Given authenticated user 123
  When creating connection request to user 456 with profileId 42
  Then connection created with PENDING status
  And createAt set to current time
  And id auto-generated
  And recipient 456 receives realtime notification via SignalR
  And recipient 456 receives notification event for notification service
  And Connection entity returned
```

**Validation Tests:**
- UserIdFrom missing → 400 Bad Request
- UserIdTo missing → 400 Bad Request
- UserIdFrom == UserIdTo → 400 Bad Request (self-connection)
- User not authenticated → 401 Unauthorized
- ProfileId invalid → 400 Bad Request

**Authorization Tests:**
- Authenticated user's ID must match UserIdFrom
- Creating connection for another user → 403 Forbidden

**Concurrency Tests:**
- Two simultaneous connection requests → both processed
- Duplicate connection requests (same users) → allowed (separate connections)
- No race condition in auto-increment ID

**Realtime Tests:**
- SignalR message sent to recipient's group
- Multiple recipient connections all receive notification
- Offline recipients don't block response

---

#### CreateMessage Tests

**Happy Path:**
```gherkin
Scenario: Send message in active room
  Given room 501 with two connected users
  And authenticated user 123
  When sending message "Hi, how are you?"
  Then message created with UNREAD status
  And message timestamp set to now
  And room.LastMessAt updated to message timestamp
  And message broadcast to all users in room via SignalR
  And message broadcast to Realtime Service
  And MessageDto returned with id
```

**Validation Tests:**
- Content empty or null → rejected
- Content > 5000 chars → truncated or rejected
- Room doesn't exist → 404 Not Found
- User not part of room → 403 Forbidden

**Status Handling Tests:**
- Status = 0 (UNREAD) → correctly set
- Status = 1 (READ) → correctly set
- Status = invalid value → default to UNREAD
- Status = 2 (DELIVERED) → correctly set

**Concurrency Tests:**
- Multiple messages simultaneously → all saved in order
- Race condition in LastMessAt update → handled with locking
- No message loss on rapid-fire messages

**Realtime Broadcasting Tests:**
- SignalR broadcast to room group succeeds
- Multiple clients in room all receive message
- Offline users don't block broadcast
- Realtime Service publish async (non-blocking)

---

#### GetRoomMessages Tests

**Happy Path:**
```gherkin
Scenario: Load all messages from room
  Given room 501 with 50 messages
  And authenticated user 123 in room
  When requesting room messages
  Then all 50 messages returned
  And ordered by CreatedAt ascending (oldest first)
  And message list with IEnumerable<Message> returned
```

**Empty Room Tests:**
- Room with 0 messages → return empty list
- Room created but no messages yet → return empty

**Ordering Tests:**
- Messages ordered chronologically oldest to newest
- Timestamps preserved exactly as stored

**Authorization Tests:**
- User not in room → 403 Forbidden (or return empty)
- Unauthenticated request → 401 Unauthorized

---

### Data Flow Documentation - Connection Service

#### CreateConnection Data Flow

```
API Request (CreateConnectionRequest: userIdFrom, userIdTo, profileId)
    ↓
ConnectionController.CreateConnection()
    ↓
[VALIDATE AUTHENTICATION]
├─ Extract userId from JWT token
└─ Verify userId == request.UserIdFrom

    ↓
[CREATE CONNECTION ENTITY]
├─ Connection conn = new Connection {
│   UserIdFrom: request.UserIdFrom,
│   UserIdTo: request.UserIdTo,
│   ProfileId: request.ProfileId,
│   Status: "PENDING",
│   CreateAt: VietnamTime.Now(),
│   ConnectionAt: null,
│   Rooms: [] (sanitized)
│ }
└─ IConnectionService.CreateConnectionAsync(conn)

    ↓
[SANITIZE IN SERVICE]
├─ conn.Id = 0 (ensure insert)
├─ conn.Rooms = [] (clear any nested data)
├─ conn.CreateAt = VietnamTime.Now() (enforce server time)
└─ conn.Status = "PENDING" (enforce initial status)

    ↓
[PERSIST TO DATABASE]
├─ IConnectionRepository.CreateAsync(conn)
│   └─ DbContext.Connection.Add(conn)
│   └─ DbContext.SaveChangesAsync()
│       └─ INSERT Connection (...) VALUES (...)
│       └─ SELECT SCOPE_IDENTITY() → get generated Id
└─ Return persisted entity with Id

    ↓
[REALTIME NOTIFICATION - GROUP BROADCAST]
├─ IHubContext<ChatHub>.Clients.Group($"user_{created.UserIdTo}")
│   .SendAsync("ConnectionRequested", {
│     connectionId: created.Id,
│     fromUserId: created.UserIdFrom,
│     toUserId: created.UserIdTo,
│     profileId: created.ProfileId,
│     status: created.Status,
│     createdAt: created.CreateAt
│   })
└─ (Non-blocking, async fire-and-forget)

    ↓
[REALTIME SERVICE NOTIFICATION]
├─ IConnectionEventPublisher.PublishConnectionRequestedAsync(
│   created.Id,
│   created.UserIdFrom,
│   created.UserIdTo,
│   created.ProfileId,
│   created.CreateAt
│ )
└─ (Async event to Realtime Service, non-blocking)

    ↓
[ACTOR INFO ENRICHMENT FOR NOTIFICATION]
├─ GetUserProfileAsync(created.UserIdFrom)
│   └─ HTTP GET /api/users/{userId} from UserProfile Service
│       Returns: { Id, Name, Avatar, Role }
└─ Store as actorProfile

    ↓
[NOTIFICATION EVENT PUBLISHING]
├─ IConnectionEventPublisher.PublishConnectionRequestNotificationAsync({
│   EventType: "connection.request.created",
│   UserId: created.UserIdTo.ToString(),
│   ActorId: created.UserIdFrom.ToString(),
│   ActorType: "USER",
│   ObjectId: created.Id.ToString(),
│   Title: "Đã nhận yêu cầu kết nối",
│   Content: $"{actorProfile.Name} vừa gửi cho bạn một yêu cầu kết nối.",
│   Type: "CONNECTION_REQUEST_SENT",
│   Author: actorProfile,
│   CreatedAt: created.CreateAt
│ })
└─ (Consumed by Notification Service to create notification record)

    ↓
[RETURN RESPONSE]
└─ 201 Created
    Location: /api/connection/{created.Id}
    Body: created Connection entity
```

**Service Dependencies:**
- `IConnectionRepository` - Database access
- `IHubContext<ChatHub>` - SignalR broadcasting
- `IConnectionEventPublisher` - Event publishing
- User Profile Service (HTTP) - Actor enrichment

**Real-time Channels:**
1. SignalR Group: `user_{userId}` - Direct user notifications
2. Realtime Service Events - Cross-service real-time
3. Notification Service Events - Durable notification records

---

#### CreateMessage Data Flow

```
API Request (CreateMessageRequest: roomId, content)
    ↓
ConnectionController.CreateMessage()
    ↓
[VALIDATE]
├─ Room exists (roomId)
├─ User authenticated and in room
├─ Content not empty
└─ Content length <= 5000

    ↓
[SANITIZE INPUT IN SERVICE]
├─ message.Id = 0 (ensure insert)
├─ message.Room = null (clear nested object)
├─ message.CreatedAt = VietnamTime.Now() (enforce server time)
├─ If message.Status not in [1, 2]:
│   └─ message.Status = 0 (UNREAD default)
└─ IConnectionService.CreateMessageAsync(message)

    ↓
[INSERT MESSAGE]
├─ IConnectionRepository.CreateMessageAsync(message)
│   └─ DbContext.Message.Add(message)
│   └─ DbContext.SaveChangesAsync()
│       └─ INSERT Message (MessageRoomId, UserId, Content, CreatedAt, Status)
│       └─ SELECT SCOPE_IDENTITY() → get generated Id
└─ Return created message with Id

    ↓
[UPDATE ROOM LAST MESSAGE TIME]
├─ IConnectionRepository.GetRoomByIdAsync(created.MessageRoomId)
│   └─ SELECT * FROM Room WHERE Id = @roomId
├─ room.LastMessAt = created.CreatedAt
├─ IConnectionRepository.UpdateRoomAsync(room)
│   └─ UPDATE Room SET LastMessAt = @timestamp WHERE Id = @roomId
│   └─ DbContext.SaveChangesAsync()
└─ Return void

    ↓
[REALTIME BROADCAST TO ROOM - SIGNALR]
├─ IHubContext<ChatHub>.Clients.Group($"room_{roomId}")
│   .SendAsync("MessageReceived", {
│     id: created.Id,
│     roomId: created.MessageRoomId,
│     userId: created.UserId,
│     content: created.Content,
│     createdAt: created.CreatedAt,
│     status: created.Status
│   })
└─ (Non-blocking, fire-and-forget)

    ↓
[REALTIME SERVICE BROADCAST]
├─ IConnectionEventPublisher.PublishMessageCreatedAsync({
│   messageId: created.Id,
│   roomId: roomId,
│   userId: created.UserId,
│   content: created.Content,
│   createdAt: created.CreatedAt
│ })
└─ (Async event to Realtime Service)

    ↓
[NOTIFICATION FOR OFFLINE USERS]
├─ If recipient user offline:
│   └─ INotificationEventPublisher.PublishChatMessageNotificationAsync({
│       eventType: "connection.message.created",
│       recipientId: otherUserId,
│       actorId: created.UserId,
│       objectId: created.Id,
│       title: "New message",
│       content: created.Content,
│       timestamp: created.CreatedAt
│     })
└─ (Consumed by Notification Service for push notifications)

    ↓
[RETURN RESPONSE]
└─ 201 Created
    Body: MessageDto {
      id: created.Id,
      roomId: created.MessageRoomId,
      userId: created.UserId,
      content: created.Content,
      createdAt: created.CreatedAt,
      status: created.Status
    }
```

**External Service Calls:**
- None synchronous (all async for real-time)

**Transaction Scope:**
- Message INSERT and Room UPDATE in separate transactions
- Message creation guaranteed (auto-retry on Room update failure)

**Real-time Broadcasting:**
- SignalR broadcast to room group
- Realtime Service event async
- Notification event async (offline handling)

---

## 5. NOTIFICATION SERVICE

### Service Architecture

**Controllers Location:** `D:\Capstone\src\Services\Notification\Notification.API\Controllers\`
- `NotificationController.cs` - Notification retrieval and management
- `FcmController.cs` - Firebase Cloud Messaging setup
- `DeviceTokenController.cs` - Device token management

**Service Layer:** `Notification.Application\Services\`
- `NotificationService.cs` - Notification retrieval and filtering
- `NotificationPublishingService.cs` - Event consumption and creation
- `FcmService.cs` - Firebase push notification sending
- `AggregationFlushService.cs` - Batch notification aggregation

**Domain Layer:** `Notification.Domain\Entities\`
- `NotificationEntity.cs` - Notification record
- `DeviceTokenEntity.cs` - User device tokens
- `NotificationSettingsEntity.cs` - User preferences
- `PushNotificationLogEntity.cs` - Delivery logs

---

### Critical Function Specifications

#### 5.1 GetNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID from JWT
- `cursor: int?` - Cursor for pagination (notification ID from previous page)
- `limit: int` - Items per page (clamped 1-50)

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications for user ordered by CreatedAt DESC
2. Apply cursor pagination (Id < cursor)
3. Load limit + 1 to detect hasMore
4. Build actor maps from stored actor data
5. Resolve missing actor info via HTTP (fallback)
6. Enrich DTOs with actor details
7. Return cursor-paginated result

**Database Operations:**
```sql
SELECT TOP (limit + 1) *
FROM NotificationEntity
WHERE UserId = @userId
AND Id < @cursor (if cursor provided)
ORDER BY CreatedAt DESC
```

**Data Transformation:**
```
NotificationEntity → UserNotificationDto {
  id: n.Id,
  userId: n.UserId,
  title: n.Title,
  content: n.Content,
  type: n.Type,
  objectId: n.ObjectId,
  actor: {
    id: int.Parse(n.ActorId) ?? 0,
    name: n.ActorName,
    avatar: n.ActorAvatar,
    role: n.ActorType == "COMPANY" ? "COMPANY" : "USER"
  },
  createdAt: n.CreatedAt,
  isRead: n.IsRead
}
```

**Example Response:**
```json
{
  "items": [
    {
      "id": 5001,
      "userId": "123",
      "title": "Challenge Graded",
      "content": "Your solution for 'Build Todo API' scored 85/100",
      "type": "CHALLENGE_GRADED",
      "objectId": "550e8400-e29b-41d4-a716-446655440000",
      "actor": {
        "id": 0,
        "name": "System",
        "avatar": null,
        "role": "SYSTEM"
      },
      "createdAt": "2025-06-15T16:00:00Z",
      "isRead": false
    },
    {
      "id": 5000,
      "userId": "123",
      "title": "New Message",
      "content": "John: Hi, how are you?",
      "type": "CHAT_MESSAGE",
      "objectId": "2001",
      "actor": {
        "id": 456,
        "name": "John Doe",
        "avatar": "https://...",
        "role": "USER"
      },
      "createdAt": "2025-06-15T15:36:00Z",
      "isRead": false
    }
  ],
  "nextCursor": 4999,
  "hasMore": true
}
```

---

#### 5.2 GetCommunityNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetCommunityNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID
- `cursor: int?` - Pagination cursor
- `limit: int` - Items per page

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications filtered by CommunityTypes only:
   - POST_FAVORITED
   - COMMENT_CREATED
   - REPLY_TO_COMMENT
   - POST_REPORTED
2. Apply cursor pagination
3. Enrich with actor data
4. Return paginated result

**Notification Type Groups:**
```csharp
public static class NotificationTypeGroups
{
    public static readonly string[] CommunityTypes = new[]
    {
        "POST_FAVORITED",
        "POST_SAVED",
        "COMMENT_CREATED",
        "REPLY_TO_COMMENT",
        "POST_REPORTED",
        "COMMENT_FAVORITED"
    };
}
```

---

#### 5.3 GetMessageNotifications

**Function Signature:**
```csharp
public async Task<CursorPagedResult<UserNotificationDto>> GetMessageNotificationsAsync(
    string userId, int? cursor, int limit)
```

**Parameters:**
- `userId: string` - User ID
- `cursor: int?` - Pagination cursor
- `limit: int` - Items per page

**Return Type:** `Task<CursorPagedResult<UserNotificationDto>>`

**Business Logic:**
1. Query notifications filtered by Type = "CHAT_MESSAGE" only
2. Apply cursor pagination
3. Enrich with actor data
4. Return paginated result

**Example Response:**
```json
{
  "items": [
    {
      "id": 5000,
      "userId": "123",
      "title": "New Message from John",
      "content": "Hi, how are you?",
      "type": "CHAT_MESSAGE",
      "objectId": "2001",
      "actor": {
        "id": 456,
        "name": "John Doe",
        "avatar": "https://...",
        "role": "USER"
      },
      "createdAt": "2025-06-15T15:36:00Z",
      "isRead": false
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

---

#### 5.4 PublishNotification

**Function Signature:**
```csharp
public async Task<NotificationCreatedEventDto> BuildCreatedEventAsync(
    NotificationEntity entity)
```

**Parameters:**
- `entity: NotificationEntity` - Notification to publish

**Return Type:** `Task<NotificationCreatedEventDto>`

**Business Logic:**
1. Extract notification data from entity
2. Build event DTO with:
   - Notification ID, type, user ID
   - Actor information (resolved or stored)
   - Content, title, timestamp
3. Return DTO for event publishing
4. Used for push notifications via FCM

**Event Structure:**
```json
{
  "eventType": "notification.created",
  "notificationId": 5001,
  "userId": "123",
  "title": "Challenge Graded",
  "content": "Your solution scored 85/100",
  "type": "CHALLENGE_GRADED",
  "objectId": "550e8400...",
  "actor": {
    "id": 0,
    "name": "System",
    "avatar": null,
    "role": "SYSTEM"
  },
  "createdAt": "2025-06-15T16:00:00Z"
}
```

---

#### 5.5 AggregateNotifications

**Function Signature:**
```csharp
public async Task<List<NotificationEntity>> AggregateNotificationsAsync(
    string userId, string aggregationType)
```

**Parameters:**
- `userId: string` - User to aggregate for
- `aggregationType: string` - Aggregation key (e.g., "POST_{postId}", "COMMENT_REPLIES")

**Return Type:** `Task<List<NotificationEntity>>`

**Business Logic:**
1. Query recent notifications matching aggregation key
2. Example aggregations:
   - Multiple "comment_created" on same post → single aggregated notification
   - Multiple "reply_to_comment" on same comment → single notification "3 people replied"
   - Multiple "post_favorited" on same post → "5 people favorited your post"
3. Combine message, update CreatedAt to latest
4. Return aggregated notification list

**Aggregation Examples:**

**Before Aggregation:**
```json
[
  {
    "id": 5010,
    "type": "POST_FAVORITED",
    "content": "John favorited your post",
    "actorId": "456",
    "actorName": "John",
    "createdAt": "2025-06-15T16:05:00Z"
  },
  {
    "id": 5009,
    "type": "POST_FAVORITED",
    "content": "Jane favorited your post",
    "actorId": "789",
    "actorName": "Jane",
    "createdAt": "2025-06-15T16:10:00Z"
  },
  {
    "id": 5008,
    "type": "POST_FAVORITED",
    "content": "Bob favorited your post",
    "actorId": "999",
    "actorName": "Bob",
    "createdAt": "2025-06-15T16:15:00Z"
  }
]
```

**After Aggregation:**
```json
[
  {
    "id": 5010,
    "type": "POST_FAVORITED_AGGREGATED",
    "content": "Jane, Bob and 1 other person favorited your post",
    "actorId": "789,999,456",
    "actorName": "Jane, Bob, John",
    "aggregationCount": 3,
    "createdAt": "2025-06-15T16:15:00Z"
  }
]
```

---

### Test Coverage Plan

#### GetNotifications Tests

**Happy Path:**
```gherkin
Scenario: Load user notifications with cursor pagination
  Given user "123" with 50 unread notifications
  When requesting notifications with limit 20
  Then 20 notifications returned
  And nextCursor points to oldest notification ID
  And hasMore true if more exist
  And actor info enriched (stored data used, HTTP fallback if needed)
  And notifications ordered chronologically (newest first)
```

**Pagination Tests:**
- cursor = null → return first batch
- cursor = non-existent ID → graceful handling (return empty)
- limit = 0 → clamped to 1
- limit = 100 → clamped to 50
- Empty notifications → return empty items, hasMore = false

**Actor Enrichment Tests:**
- Stored actor data present → use directly (no HTTP call)
- Stored data missing → HTTP GET to UserProfile/Company service
- HTTP call fails → use fallback actor with minimal info
- Multiple same actor → deduplicate HTTP calls

**Type Filtering Tests:**
- All types included in GetNotifications
- Only community types in GetCommunityNotifications
- Only CHAT_MESSAGE in GetMessageNotifications

---

#### PublishNotification Tests

**Happy Path:**
```gherkin
Scenario: Build notification event for publishing
  Given notification entity with:
    - Type: "CHALLENGE_GRADED"
    - UserId: "123"
    - ActorId: null (system notification)
    - Content and title
  When building event
  Then event created with all fields mapped
  And timestamp preserved
  And actor data included (null for system)
  And event returned as NotificationCreatedEventDto
```

**Data Mapping Tests:**
- All NotificationEntity fields map to EventDto correctly
- Timestamps in ISO8601 format
- ActorId nullable handling
- Content truncation if > 500 chars

**Event Publishing Tests:**
- Event published to message queue (RabbitMQ/Azure Service Bus)
- FCM push triggered for users with device tokens
- Retry on delivery failure
- Log delivery status

---

#### AggregateNotifications Tests

**Happy Path:**
```gherkin
Scenario: Aggregate multiple same-type notifications
  Given 5 notifications: "John favorited", "Jane favorited", "Bob favorited", ...
  When aggregating by POST_FAVORITED
  Then single aggregated notification returned
  And content: "Jane, Bob and 2 other people favorited your post"
  And actorIds combined: "789,999,..."
  And createdAt = latest timestamp
```

**Aggregation Rules Tests:**
- POST_FAVORITED on same post → aggregated
- COMMENT_CREATED on same post → aggregated
- REPLY_TO_COMMENT on same comment → aggregated
- Different post IDs → not aggregated
- Mixed types → not aggregated

**Edge Cases:**
- 2 notifications → "John and Jane favorited..."
- 3+ notifications → "Jane, Bob and X other(s) favorited..."
- 10+ aggregated → "Jane, Bob and 8 others favorited..."

---

### Data Flow Documentation - Notification Service

#### GetNotifications Data Flow

```
API Request (userId, cursor, limit)
    ↓
NotificationController.GetNotifications()
    ↓
[EXTRACT USER ID FROM JWT]
├─ User.FindFirst(ClaimTypes.NameIdentifier)
├─ User.FindFirst("sub")
└─ Throw UnauthorizedAccessException if missing

    ↓
[CLAMP LIMIT]
└─ limit = Math.Clamp(limit, 1, 50)

    ↓
INotificationService.GetNotificationsAsync(userId, cursor, limit)
    ↓
[FETCH NOTIFICATIONS]
├─ INotificationRepository.GetNotificationsAsync(userId, cursor, limit)
│   └─ SELECT TOP (limit + 1) *
│      FROM NotificationEntity
│      WHERE UserId = @userId
│      AND (cursor == null OR Id < @cursor)
│      ORDER BY CreatedAt DESC
├─ Determine hasMore = count > limit
├─ If hasMore: take first limit only
└─ Return notifications list

    ↓
[BUILD ACTOR MAP - DEDUPLICATED]
├─ Extract unique (ActorId, ActorType, ActorName, ActorAvatar)
├─ For actors with stored data (ActorName not null):
│   └─ Use stored data directly (no HTTP call)
└─ For actors with missing stored data:
    └─ Collect list for HTTP enrichment

    ↓
[HTTP ENRICHMENT - PARALLEL BATCH CALLS]
├─ If missing actors:
│   ├─ Group by ActorType (USER vs COMPANY)
│   ├─ IActorResolverClient.ResolveBatchAsync(userActorIds)
│   │   └─ HTTP GET /api/actors/batch?ids=456,789
│   │       Returns: [{id, name, avatar, role}, ...]
│   └─ Merge with stored data
└─ Build actorMap: {ActorId → ActorDto}

    ↓
[BUILD DTOS]
└─ For each notification:
    ├─ Create UserNotificationDto {
    │   id: n.Id,
    │   userId: n.UserId,
    │   title: n.Title,
    │   content: n.Content,
    │   type: n.Type,
    │   objectId: n.ObjectId,
    │   actor: actorMap.Get(n.ActorId) ?? BuildFallback(n.ActorId),
    │   createdAt: n.CreatedAt,
    │   isRead: n.IsRead
    │ }
    └─ Add to items list

    ↓
[RETURN PAGINATED RESULT]
└─ CursorPagedResult<UserNotificationDto> {
    Items: dtos,
    NextCursor: hasMore ? notifications.Last().Id : null,
    HasMore: hasMore
  }
```

**Service Dependencies:**
- `INotificationRepository` - Notification query
- `IActorResolverClient` - HTTP enrichment (fallback only)

**Caching Strategy:**
- Actor info cached 5 minutes in Redis
- Notification list NOT cached (real-time)

---

#### PublishNotification Data Flow

```
Event from Message Queue (any service)
    ↓
NotificationPublishingService (Consumer)
    ↓
[RECEIVE EVENT]
├─ Event payload:
│   {
│     eventType: "challenge.submission.graded",
│     submissionId: "...",
│     userId: "123",
│     score: 85,
│     timestamp: "2025-06-15T16:00:00Z",
│     actorId: null (system),
│     actorType: "SYSTEM",
│     actorName: "Challenge System",
│     title: "Challenge Graded",
│     content: "..."
│   }
└─ Deserialize event

    ↓
[CREATE NOTIFICATION ENTITY]
├─ NotificationEntity {
│   Id: auto-generated,
│   UserId: event.userId,
│   Title: event.title,
│   Content: event.content,
│   Type: MapEventTypeToNotificationType(event.eventType),
│   ObjectId: event.objectId,
│   ActorId: event.actorId,
│   ActorType: event.actorType,
│   ActorName: event.actorName,
│   ActorAvatar: event.actorAvatar,
│   CreatedAt: UtcNow,
│   IsRead: false
│ }
└─ INotificationRepository.AddAsync(entity)

    ↓
[PERSIST TO DATABASE]
└─ DbContext.SaveChangesAsync()
    └─ INSERT NotificationEntity

    ↓
[BUILD CREATED EVENT]
├─ INotificationService.BuildCreatedEventAsync(entity)
│   └─ NotificationCreatedEventDto {
│       eventType: "notification.created",
│       notificationId: entity.Id,
│       userId: entity.UserId,
│       ...
│     }
└─ Return event

    ↓
[PUBLISH TO FCM]
├─ IDeviceTokenService.GetDeviceTokensAsync(entity.UserId)
│   └─ SELECT * FROM DeviceTokenEntity
│      WHERE UserId = @userId AND IsActive = true
├─ For each device token:
│   ├─ IFcmService.SendNotificationAsync({
│   │   token: deviceToken,
│   │   title: entity.Title,
│   │   body: entity.Content,
│   │   data: {
│   │     notificationId: entity.Id,
│   │     type: entity.Type,
│   │     objectId: entity.ObjectId
│   │   }
│   │ })
│   │   └─ HTTP POST to Firebase Cloud Messaging
│   │       Returns: {success, messageId}
│   └─ INSERT PushNotificationLogEntity {
│       notificationId: entity.Id,
│       deviceTokenId: token.Id,
│       sentAt: UtcNow,
│       status: "SENT",
│       messageId: response.messageId
│     }
└─ Log any failures for retry

    ↓
[CACHE INVALIDATION]
├─ Remove from Redis: $"unread:{userId}"
└─ Update unread count cache

    ↓
[RETURN ASYNC]
└─ Consumer completes (notification fully processed)
```

**Event Sources:**
- Challenge Service (submission.graded)
- Community Service (post.created, comment.created
