# AI Service Azure Setup Guide

## 1. Mục tiêu

Hướng dẫn setup và vận hành phần AI vừa triển khai cho:
- **Portfolio Service**
- **Company Service**
- Shared module **`RecruitmentPlatform.AI`**

Phạm vi gồm moderation, embedding lifecycle, matching 2 chiều, cache và RabbitMQ async embedding trên **Azure production**.

---

## 2. Kiến trúc AI hiện tại

### 2.1 Shared AI module

Module: `src/Shared/RecruitmentPlatform.AI`

Thành phần chính:
- `MatchingEngine`
- `ScoringHelper`
- `CosineSimilaritySafe`
- `TextNormalizer`
- `OpenAiEmbeddingService`

### 2.2 Matching endpoints

- `GET /api/portfolio/{id}/match-jobs`
- `GET /api/company-posts/{id}/match-portfolios`
- `GET /api/portfolio/internal/matching-candidates`
- `GET /api/company-posts/internal/matching-candidates`

### 2.3 Pipeline matching (đã cố định)

1. Data quality filter  
2. Pre-filter theo skill overlap (top 100-150)  
3. Cosine similarity (safe)  
4. Tính score components  
5. Weighted scoring  
6. Threshold filter (`finalScore >= 0.3`)  
7. Stable sorting (`finalScore DESC`, `UpdatedAt DESC`, `Id ASC`)  
8. Pagination (max 50)  
9. Versioned cache key

### 2.4 Moderation + embedding lifecycle

- Portfolio moderation outcome: `Rejected`, `PendingReview`, `Approved`
- Embedding status: `Pending`, `Ready`, `Failed`
- Async embedding event qua RabbitMQ:
  - `portfolio.changed`
  - `company.post.changed`

---

## 3. Azure cấu hình bắt buộc

## 3.1 Secrets/Env cho cả 2 service

| Key | Bắt buộc | Gợi ý giá trị |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Yes | SQL Server connection string |
| `OpenAI__BaseUrl` | Yes | `https://api.openai.com` |
| `OpenAI__ApiKey` | Yes | OpenAI API key thật |
| `RabbitMQ__HostName` | Yes | hostname RabbitMQ (internal) |
| `RabbitMQ__UserName` | Yes | user RabbitMQ |
| `RabbitMQ__Password` | Yes | password RabbitMQ |
| `RabbitMQ__VirtualHost` | Yes | `/` hoặc vhost riêng |
| `RabbitMQ__Port` | Yes | `5672` |

### 3.2 Portfolio Service env bổ sung

| Key | Bắt buộc |
| --- | --- |
| `ServiceUrls__CompanyService` | Yes |
| `ServiceUrls__AuthService` | Yes |
| `ServiceUrls__UserProfileService` | Yes |
| `ServiceUrls__MediaService` | Yes |

### 3.3 Company Service env bổ sung

| Key | Bắt buộc |
| --- | --- |
| `ServiceUrls__PortfolioService` | Yes |
| `ServiceUrls__UserProfileService` | Yes |
| `ServiceUrls__MediaService` | Yes |

> Khuyến nghị dùng Azure Key Vault + Managed Identity để nạp secrets.

---

## 4. Checklist deploy Azure

1. Deploy shared code kèm Portfolio + Company image mới.
2. Cập nhật đầy đủ env vars/secrets ở Container Apps.
3. Restart revision của:
   - portfolio-service
   - company-service
4. Verify startup log:
   - không lỗi OpenAI key
   - không lỗi RabbitMQ connection
   - consumer queue đã bắt đầu listen

---

## 5. Verify sau deploy

## 5.1 Health & Swagger

- Portfolio swagger: `/swagger`
- Company swagger: `/swagger`

## 5.2 Test API matching

### Candidate -> Job
```http
GET /api/portfolio/{portfolioId}/match-jobs?page=1&pageSize=20
```

### Job -> Candidate
```http
GET /api/company-posts/{postId}/match-portfolios?page=1&pageSize=20
```

### Internal feed
```http
GET /api/portfolio/internal/matching-candidates?limit=150
GET /api/company-posts/internal/matching-candidates?limit=150
```

Kỳ vọng:
- `finalScore` trong `[0..1]` và đã round 4 decimals
- pagination hoạt động, `pageSize <= 50`
- kết quả ổn định theo sorting rule

---

## 6. Troubleshooting

### 6.1 `EmbeddingStatus` không lên `Ready`
- Kiểm tra `OpenAI__ApiKey` hợp lệ.
- Kiểm tra outbound network tới OpenAI.
- Kiểm tra log consumer RabbitMQ của service tương ứng.

### 6.2 Matching trả rỗng
- Check source entity có `EmbeddingStatus = Ready`.
- Check cross-service URL (`ServiceUrls__CompanyService` / `ServiceUrls__PortfolioService`).
- Check timeout 2s có bị trigger liên tục (dependency chậm).

### 6.3 Queue không xử lý
- Verify RabbitMQ host/user/password/vhost/port.
- Verify exchange `skillsnap.events` và routing key:
  - `portfolio.changed`
  - `company.post.changed`

### 6.4 Cache stale
- Cache key đang version-aware:
  - `portfolio:{id}:{embeddingVersion}:matched-jobs`
  - `job:{id}:{embeddingVersion}:matched-portfolios`
- Khi embeddingVersion tăng, key mới sẽ được dùng tự động.

---

## 7. Ghi chú vận hành

- Không gọi AI trong runtime matching.
- Embedding chỉ tạo khi create/update + async event xử lý lại.
- Nếu muốn giảm chi phí OpenAI:
  - batch update ngoài giờ cao điểm
  - hạn chế update không cần thiết trên portfolio/job content.
