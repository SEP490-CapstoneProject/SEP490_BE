Agent completed. agent_id: test-specs-generator, agent_type: explore, status: completed, description: Generating detailed test specifications, elapsed: 174s, total_turns: 0, duration: 174s

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
