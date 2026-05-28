# Unit Test Case Matrix
## Capstone Backend Services

This document provides a structured matrix of all test cases needed for the identified critical and high-priority functions.

---

## TEST CASE MATRIX BY SERVICE

### 1. AUTH SERVICE

#### 1.1 RegisterAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Register with valid data | Happy Path | Valid RegisterRequest (email, password, role) | User created, JWT token returned | Status=200, Token!=null, User.Email=input.Email |
| Register with invalid email | Validation | Invalid email format | 400 Bad Request | Status=400, Error contains "email" |
| Register with duplicate email | Business Logic | Existing email | 400 Bad Request | Status=400, Error contains "already exists" |
| Register with weak password | Validation | Password < 6 chars | 400 Bad Request | Status=400, Error contains "password" |
| Register with null request | Validation | null | 400 Bad Request | Status=400 |

#### 1.2 LoginAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Login with correct credentials | Happy Path | Correct email/password | JWT token + refresh token | Status=200, Token!=null, RefreshToken!=null |
| Login with wrong password | Business Logic | Correct email, wrong password | 401 Unauthorized | Status=401, Error="Invalid credentials" |
| Login with non-existent email | Business Logic | Non-existent email | 401 Unauthorized | Status=401, Error="Invalid credentials" |
| Login with locked user | Business Logic | Locked user email/password | 403 Forbidden | Status=403, Error="User is locked" |
| Login with inactive user | Business Logic | Inactive user email/password | 403 Forbidden | Status=403, Error="User account inactive" |

#### 1.3 RefreshTokenAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Refresh with valid token | Happy Path | Valid refresh token | New JWT token | Status=200, NewToken!=null, Token!=OldToken |
| Refresh with expired token | Business Logic | Expired refresh token | 401 Unauthorized | Status=401, Error="Token expired" |
| Refresh with invalid token | Validation | Malformed token | 401 Unauthorized | Status=401, Error="Invalid token" |
| Refresh with revoked token | Business Logic | Revoked token | 401 Unauthorized | Status=401, Error="Token revoked" |

#### 1.4 ChangePasswordAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Change password with correct current | Happy Path | Correct old + new password | Password updated | Status=200, NewPassword can login |
| Change with wrong current password | Validation | Wrong old password | 400 Bad Request | Status=400, Error="Current password incorrect" |
| Change with weak new password | Validation | New password < 6 chars | 400 Bad Request | Status=400, Error="Password too weak" |
| Change same as current | Business Logic | Old password = new password | 400 Bad Request | Status=400, Error="New password must be different" |

#### 1.5 LockUser / UnlockUser
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Lock user (admin) | Happy Path | Valid user ID, admin role | User locked | Status=200, User.Status=LOCKED |
| Lock non-existent user | Business Logic | Invalid user ID | 404 Not Found | Status=404, Error="User not found" |
| Lock as non-admin | Authorization | Valid user ID, no admin role | 403 Forbidden | Status=403, Error="Insufficient permissions" |
| Unlock locked user | Happy Path | Locked user ID, admin role | User unlocked | Status=200, User.Status=ACTIVE |

#### 1.6 GetInternalUsersByIdsAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get existing users | Happy Path | Valid user IDs comma-separated | User list | Status=200, Users.Count=2, All emails populated |
| Get with empty list | Edge Case | Empty string | Empty list | Status=200, Users.Count=0 |
| Get with invalid IDs | Edge Case | Non-numeric IDs | Only valid parsed | Status=200, Only parsed users returned |
| Get with duplicates | Edge Case | Duplicate IDs | Distinct users | Status=200, NoDuplicates in response |

---

### 2. APPLICATION SERVICE

#### 2.1 CreateApplicationAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Create with valid data | Happy Path | Valid CreateApplicationRequest + jobPostId | Application created | Status=201, Application.Status=WAITING, CreatedBy=CurrentUser |
| Create without quota | Business Logic | User at max applications | 400 Bad Request | Status=400, Error="Quota exceeded" |
| Create for non-existent job | Business Logic | Invalid jobPostId | 404 Not Found | Status=404, Error="Job post not found" |
| Duplicate application | Business Logic | Same user applies twice | 400 Bad Request | Status=400, Error="Already applied" |
| Unauthorized user | Authorization | Not logged in | 401 Unauthorized | Status=401 |

#### 2.2 UpdateApplicationStatusAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Accept application | Happy Path | applicationId, status=ACCEPTED | Status updated + notification sent | Status=200, Application.Status=ACCEPTED, NotificationPublished=true |
| Reject application | Happy Path | applicationId, status=REJECTED | Status updated + rejection notification | Status=200, Application.Status=REJECTED, NotificationPublished=true |
| Update by non-company | Authorization | User != Company owner | 403 Forbidden | Status=403 |
| Update non-existent app | Business Logic | Invalid applicationId | 404 Not Found | Status=404 |
| Invalid status transition | Business Logic | WAITING -> WAITING | 400 Bad Request | Status=400, Error="Invalid status transition" |

#### 2.3 CheckEntitlementAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Feature allowed | Happy Path | Valid user + allowed feature | Allowed | Status=200, IsAllowed=true, RemainingQuota>0 |
| Feature quota exceeded | Business Logic | Valid user + exceeded quota | Not allowed | Status=400, IsAllowed=false, Error="Quota exceeded" |
| Invalid user | Business Logic | Non-existent user | 404 Not Found | Status=404 |
| Expired plan | Business Logic | User subscription expired | Not allowed | Status=400, Error="Subscription expired" |

---

### 3. PAYMENT SERVICE

#### 3.1 CreatePaymentAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Create payment (valid) | Happy Path | Valid subscription, amount | Payment created, PayOS URL | Status=200, PaymentId!=null, CheckoutUrl!=null, OrderCode!=null |
| Create with invalid amount | Validation | Amount <= 0 | 400 Bad Request | Status=400, Error="Invalid amount" |
| Create for non-existent plan | Business Logic | Invalid planId | 404 Not Found | Status=404, Error="Plan not found" |
| Duplicate payment attempt | Business Logic | Same user, same plan, within 5 min | 400 Bad Request | Status=400, Error="Payment in progress" |
| Unauthorized | Authorization | No auth token | 401 Unauthorized | Status=401 |

#### 3.2 VerifyPaymentAsync (Webhook)
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Valid webhook signature | Happy Path | PayOS webhook, valid signature | Payment marked paid | Status=200, Payment.Status=COMPLETED, UserSubscriptionCreated=true |
| Invalid signature | Security | PayOS webhook, wrong signature | 403 Unauthorized | Status=403, Error="Invalid signature" |
| Non-existent order code | Business Logic | Invalid order code | 404 Not Found | Status=404 |
| Duplicate webhook | Idempotency | Webhook called twice | Idempotent | Status=200, Payment updated only once |
| Payment failed | Business Logic | PayOS reports failed | Payment marked failed | Status=200, Payment.Status=FAILED, UserSubscriptionNotCreated=true |

#### 3.3 GetPaymentByIdAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get own payment | Happy Path | Valid paymentId, owner | Payment details | Status=200, Payment.UserId=CurrentUser |
| Get other user payment | Authorization | Valid paymentId, not owner | 403 Forbidden | Status=403 |
| Non-existent payment | Business Logic | Invalid paymentId | 404 Not Found | Status=404 |

---

### 4. PORTFOLIO SERVICE

#### 4.1 GetByIdAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get public portfolio (any user) | Happy Path | Valid portfolioId, public | Portfolio + blocks | Status=200, Portfolio.Blocks.Count>0, AllBlocks.DataLoaded |
| Get own portfolio (private) | Happy Path | Valid portfolioId, owner | Full portfolio | Status=200, AllBlocksVisible=true |
| Get private (not owner) | Authorization | Valid portfolioId, not owner, private | 403 Forbidden | Status=403, Error="Not authorized" |
| Non-existent portfolio | Business Logic | Invalid portfolioId | 404 Not Found | Status=404 |

#### 4.2 UpdateBlockAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Update block (owner) | Happy Path | Valid portfolioId, blockId, valid data | Block updated | Status=200, Block.Data updated, UpdatedAt changed |
| Update with file upload | Happy Path | Block + image file | File saved + block updated | Status=200, File.Url!=null, Block.ImageUrl set |
| Update non-existent block | Business Logic | Invalid blockId | 404 Not Found | Status=404 |
| Update (not owner) | Authorization | Valid IDs, not owner | 403 Forbidden | Status=403 |
| Invalid block data | Validation | Invalid data structure | 400 Bad Request | Status=400, Error="Invalid block data" |

#### 4.3 GeneratePreviewAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Generate valid preview | Happy Path | Valid portfolioId + blocks | Preview URL | Status=200, PreviewUrl!=null, PreviewUrl.Length>0 |
| Generate for empty portfolio | Edge Case | Portfolio with no blocks | Basic preview | Status=200, PreviewUrl!=null |
| Generate with image blocks | Happy Path | Portfolio + image blocks | Images included | Status=200, PreviewUrl includes images |
| AI service timeout | Business Logic | AI service slow | Fallback preview | Status=200, PreviewUrl!=null, UsedFallback=true |

---

### 5. SUBSCRIPTION SERVICE

#### 5.1 SubscribeAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Subscribe to monthly plan | Happy Path | Valid planId, MONTHLY cycle | Subscription created | Status=200, Subscription.Status=ACTIVE, StartDate=Today, EndDate=Today+30days |
| Subscribe to yearly plan | Happy Path | Valid planId, YEARLY cycle | Subscription created | Status=200, Subscription.EndDate=Today+365days |
| Subscribe while active | Business Logic | User already has subscription | 400 Bad Request | Status=400, Error="Already subscribed" |
| Invalid plan | Business Logic | Non-existent planId | 404 Not Found | Status=404 |
| Unauthorized | Authorization | No auth token | 401 Unauthorized | Status=401 |

#### 5.2 UpgradeAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Upgrade to higher plan | Happy Path | Current=BASIC, Target=PRO | Subscription upgraded | Status=200, Subscription.PlanId=PRO, ProrationApplied=true |
| Upgrade same plan | Business Logic | Current=PRO, Target=PRO | 400 Bad Request | Status=400, Error="Same plan" |
| Upgrade to lower plan | Business Logic | Current=PRO, Target=BASIC | 400 Bad Request | Status=400, Error="Downgrade not allowed" |
| No active subscription | Business Logic | User not subscribed | 400 Bad Request | Status=400, Error="No active subscription" |
| Insufficient credit | Business Logic | Proration > balance | 400 Bad Request | Status=400, Error="Insufficient credit" |

#### 5.3 CancelAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Cancel active subscription | Happy Path | Active subscription | Cancelled | Status=200, Subscription.Status=CANCELLED, Features.AccessDisabled=true |
| Cancel with proration | Happy Path | Cancel mid-month | Refund issued | Status=200, Refund!=null, RefundAmount>0 |
| Cancel non-existent | Business Logic | No subscription | 404 Not Found | Status=404 |
| Cancel already cancelled | Edge Case | Already cancelled | 400 Bad Request | Status=400, Error="Already cancelled" |

---

### 6. CHALLENGE SERVICE

#### 6.1 CreateChallengeAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Create challenge | Happy Path | Valid CreateChallengeDto | Challenge created, DRAFT status | Status=201, Challenge.Status=DRAFT, Challenge.CreatedBy=UserId |
| Create with test cases | Happy Path | Challenge + test cases | Test cases saved | Status=201, TestCases.Count=request.TestCases.Count |
| Create with skills | Happy Path | Challenge + skill tags | Skills associated | Status=201, Challenge.Skills linked |
| Invalid difficulty | Validation | Invalid difficulty level | 400 Bad Request | Status=400 |
| Missing required fields | Validation | Null title or description | 400 Bad Request | Status=400 |

#### 6.2 SubmitSolutionAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Submit valid solution | Happy Path | Valid code solution | Submission created, grading started | Status=201, Submission.Status=PENDING_GRADE, GradingJobQueued=true |
| Submit duplicate | Business Logic | User already submitted | 400 Bad Request | Status=400, Error="Already submitted" |
| Challenge closed | Business Logic | Challenge expired | 400 Bad Request | Status=400 |
| Invalid language | Validation | Unsupported language | 400 Bad Request | Status=400 |

#### 6.3 GradeSolutionAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Grade passing solution | Happy Path | Solution passes all tests | Graded, points awarded | Status=200, Submission.Status=GRADED, Points>0, AllTests=PASS |
| Grade failing solution | Happy Path | Solution fails tests | Graded, no points | Status=200, Submission.Status=GRADED, Points=0, SomeTests=FAIL |
| Grade with errors | Business Logic | Code doesn't compile | Failed | Status=200, Submission.Status=FAILED, CompilationError!=null |
| AI grading timeout | Business Logic | AI takes >30s | Timeout | Status=500, Error="Grading timeout" |

---

### 7. NOTIFICATION SERVICE

#### 7.1 GetNotificationsAsync (Cursor Pagination)
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get all notifications | Happy Path | No cursor, limit=20 | Notifications list | Status=200, Notifications.Count<=20, NextCursor!=null |
| Get with cursor | Happy Path | cursor=ABC123, limit=20 | Next batch | Status=200, Notifications newer than cursor |
| Get empty | Edge Case | No notifications | Empty list | Status=200, Notifications.Count=0, NextCursor=null |
| Get with unread filter | Filtering | onlyUnread=true | Only unread | Status=200, AllNotifications.IsRead=false |

#### 7.2 PublishNotificationAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Publish to user | Happy Path | Valid userId + message | Notification created | Status=200, Notification.UserId=userId, CreatedAt=now |
| Publish to multiple | Happy Path | Array of userIds | Multiple created | Status=200, NotificationCount=userIds.Count |
| Invalid user | Business Logic | Non-existent userId | 404 Not Found | Status=404 |
| Publishing event | Event Publishing | Event triggered | Notification created | Status=200, NotificationFromEvent=true |

---

### 8. COMMUNITY SERVICE

#### 8.1 GetFeedAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get feed with cursor | Happy Path | cursor=null, pageSize=20 | Posts list | Status=200, Posts.Count<=20, NextCursor!=null |
| Get next page | Pagination | cursor=ABC, pageSize=20 | Next batch | Status=200, PostsNewerThanCursor=true |
| Filter by search query | Filtering | q="javascript" | Filtered posts | Status=200, AllPostsContain="javascript" |
| Empty feed | Edge Case | No posts | Empty list | Status=200, Posts.Count=0 |
| Unauthorized access | Authorization | Deleted user | No private posts | Status=200, OnlyPublicPosts=true |

#### 8.2 GetCommentsAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get post comments | Happy Path | Valid postId | Comments + replies | Status=200, Comments.Count>=0, RepliesLoaded=true |
| Non-existent post | Business Logic | Invalid postId | 404 Not Found | Status=404 |
| Comments paginated | Pagination | postId + limit | Limited results | Status=200, Comments.Count<=limit |

---

### 9. CONNECTION SERVICE

#### 9.1 CreateConnectionAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Create connection request | Happy Path | Valid targetUserId | Connection in PENDING | Status=201, Connection.Status=PENDING, ConnectionFrom=UserId |
| Duplicate request | Business Logic | Already sent request | 400 Bad Request | Status=400, Error="Request already sent" |
| Self connection | Business Logic | targetUserId=currentUserId | 400 Bad Request | Status=400, Error="Cannot connect to self" |
| Blocked user | Business Logic | Target blocked sender | 403 Forbidden | Status=403 |

#### 9.2 CreateMessageAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Send message | Happy Path | Valid roomId + content | Message created | Status=201, Message.Content=input, Sender=UserId, CreatedAt=now |
| Send with file | Happy Path | Message + file | File saved + linked | Status=201, Message.FileUrl!=null |
| Message to non-existent room | Business Logic | Invalid roomId | 404 Not Found | Status=404 |
| Unauthorized sender | Authorization | User not in room | 403 Forbidden | Status=403 |

---

### 10. USER PROFILE SERVICE

#### 10.1 CreateProfileAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Create employee profile | Happy Path | Valid CreateEmployeeRequest | Profile created | Status=201, Employee.UserId=currentUser, IsActive=true |
| Create company profile | Happy Path | Valid CreateCompanyRequest | Profile created | Status=201, Company.UserId=currentUser, IsActive=true |
| Duplicate profile | Business Logic | User already has profile | 400 Bad Request | Status=400, Error="Profile already exists" |
| File upload (avatar) | Happy Path | Profile + avatar file | Avatar saved | Status=201, Profile.AvatarUrl!=null |

#### 10.2 GetBatchAsync
| Test Case | Type | Input | Expected | Assertions |
|-----------|------|-------|----------|-----------|
| Get multiple profiles | Happy Path | userIds=1,2,3 | 3 profiles | Status=200, Profiles.Count=3, AllDataPopulated=true |
| Get with invalid IDs | Edge Case | userIds with invalid | Only valid returned | Status=200, InvalidIDsIgnored=true |
| Get empty list | Edge Case | userIds empty | Empty response | Status=200, Profiles.Count=0 |

---

## MOCKING STRATEGY TEMPLATE

### For ALL Services

```csharp
// Example: AuthService Tests
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockNotificationService = new Mock<INotificationService>();
        
        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockTokenService.Object,
            _mockPasswordHasher.Object,
            _mockNotificationService.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Email = "test@example.com",
            Password = "Password123",
            FullName = "Test User"
        };

        var hashedPassword = "hashed_password_hash";
        var token = new LoginResponse 
        { 
            AccessToken = "jwt_token",
            RefreshToken = "refresh_token"
        };

        _mockPasswordHasher.Setup(x => x.Hash(request.Password))
            .Returns(hashedPassword);
        
        _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User)null);
        
        _mockUserRepo.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        _mockTokenService.Setup(x => x.GenerateTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(token);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.AccessToken, result.AccessToken);
        _mockUserRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }
}
```

### Common Mocks Needed By Layer

#### API Layer Mocks
- `IAuthService` / Service interfaces
- `IHttpContextAccessor` (for User context)
- `ILogger<T>` (optional, for logging)

#### Application Layer Mocks
- `IRepository<T>` implementations
- `IDbContext` (EF Core)
- External service clients (PayOS, Gemini AI, etc.)
- `IMapper` (AutoMapper)
- `IEmailService`, `ISmsService`
- `IEventBus` / `IPublisher` (for async messaging)

#### Infrastructure Layer Mocks
- `DbContext` (use InMemoryDatabase for testing)
- HTTP clients for external APIs
- Redis client (for caching tests)
- File storage service

---

## RECOMMENDED TEST EXECUTION ORDER

### Phase 1 (Week 1): Critical Services
1. **Auth Service** - Foundation for all other tests
2. **Subscription Service** - For plan/entitlement tests
3. **Payment Service** - Payment processing flow
4. **Application Service** - Job application flow
5. **Portfolio Service** - User portfolio management

### Phase 2 (Week 2-3): High-Priority Services
6. **Challenge Service** - Challenge creation/grading
7. **Community Service** - Feed and content
8. **UserProfile Service** - Profile management
9. **Connection Service** - Messaging
10. **Notification Service** - Notification flow
11. **Company Service** - Job posts

### Phase 3 (Week 4): Remaining
12. **Interview Service** - Scheduling
13. **Realtime Service** - WebSocket integration

---

## TEST COVERAGE TARGETS

| Service | Target Coverage | Rationale |
|---------|-----------------|-----------|
| Auth | 95%+ | Critical security |
| Payment | 95%+ | Financial transactions |
| Subscription | 90%+ | Business critical |
| Application | 85%+ | Core business logic |
| Portfolio | 85%+ | User-facing feature |
| Challenge | 80%+ | Complex grading logic |
| Others | 70%+ | Supporting services |

---

## NEXT STEPS

1. Create xUnit test projects for each service
2. Set up test fixtures and base test classes
3. Implement repository mocks (InMemory for EF Core)
4. Start with Happy Path tests
5. Add validation and error scenarios
6. Add edge case and integration tests
7. Run full test suite and achieve coverage targets

Generated: 2026-05-25
Framework: xUnit + Moq
Scope: All Critical and High-Priority Services
