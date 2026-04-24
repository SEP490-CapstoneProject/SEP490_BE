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
| `OpenAI__EmbeddingModel` | Yes | `text-embedding-3-small` (khuyến nghị) |
| `RabbitMQ__HostName` | Yes | hostname RabbitMQ (ưu tiên key này) |
| `RabbitMQ__Host` | Optional | fallback tương thích nếu môi trường cũ dùng `Host` |
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

### 3.4 Config contract RabbitMQ (chuẩn hóa)

- Ưu tiên đặt đầy đủ theo contract mới:
  - `RabbitMQ__HostName`
  - `RabbitMQ__UserName`
  - `RabbitMQ__Password`
  - `RabbitMQ__VirtualHost`
  - `RabbitMQ__Port`
- Chỉ dùng `RabbitMQ__Host`/`RabbitMQ__Username` như fallback tương thích; không dùng làm cấu hình chính cho môi trường mới.

### 3.5 OpenAI model + key setup (public API)

- Service đang gọi OpenAI public API endpoint `/v1/embeddings`.
- Model embedding đã được cấu hình qua env:
  - `OpenAI__EmbeddingModel` (ví dụ: `text-embedding-3-small`)
- API key bắt buộc:
  - `OpenAI__ApiKey`
- Base URL:
  - `OpenAI__BaseUrl=https://api.openai.com`

> Nếu chưa set `OpenAI__EmbeddingModel`, service sẽ fallback về `text-embedding-3-small` để tương thích ngược; vẫn nên set explicit trên production để tránh drift giữa môi trường.

---

## 4. Checklist deploy Azure

1. Deploy shared code kèm Portfolio + Company image mới.
2. Cập nhật đầy đủ env vars/secrets ở Container Apps.
3. Restart revision của:
   - portfolio-service
   - company-service
4. Verify startup log:
    - không lỗi OpenAI key
   - model OpenAI load đúng từ config (`OpenAI__EmbeddingModel`)
   - nếu RabbitMQ lỗi: service **vẫn start API core**, consumer retry theo chu kỳ (non-fatal startup)
   - khi RabbitMQ sẵn sàng: consumer queue bắt đầu listen bình thường
5. Verify build context:
   - image phải include shared AI module (`src/Shared/RecruitmentPlatform.AI`) cho service có AI integration.

---

## 5. Verify sau deploy

## 5.1 Health & Swagger

- Portfolio swagger: `/swagger`
- Company swagger: `/swagger`

> Readiness policy: health endpoint của service phản ánh khả dụng API core; trạng thái AI consumer theo dõi qua log/metric riêng.

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
- Kiểm tra `OpenAI__EmbeddingModel` đúng tên model OpenAI public API.
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
- Nếu log báo retry RabbitMQ nhưng API vẫn healthy: đây là expected behavior sau hardening (consumer non-fatal).

### 6.4 Cache stale
- Cache key đang version-aware:
  - `portfolio:{id}:{embeddingVersion}:matched-jobs`
  - `job:{id}:{embeddingVersion}:matched-portfolios`
- Khi embeddingVersion tăng, key mới sẽ được dùng tự động.

---

## 7. Ghi chú vận hành

- Không gọi AI trong runtime matching.
- Embedding chỉ tạo khi create/update + async event xử lý lại.
- Portfolio/Company embedding consumers đã harden theo hướng retry + non-fatal startup để không block API rollout khi MQ tạm thời lỗi.
- Nếu muốn giảm chi phí OpenAI:
  - batch update ngoài giờ cao điểm
  - hạn chế update không cần thiết trên portfolio/job content.
