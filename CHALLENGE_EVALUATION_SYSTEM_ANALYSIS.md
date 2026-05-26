# Phân Tích Hệ Thống Đánh Giá Thử Thách - Challenge Evaluation System

**Ngày**: 2026-05-25  
**Dự Án**: Recruitment Platform - Skill Snap  
**Phiên Bản**: 1.0

---

## 📋 Câu Hỏi 1: Các tiêu chí đánh giá thử thách từ đâu mà ra?

### 🎯 Nguồn gốc Tiêu Chí Đánh Giá

#### **1. AI Tự Sinh Ra (Primary Source)**
Tiêu chí đánh giá **được tạo tự động bởi Gemini AI** dựa trên nội dung của thử thách.

**Flow:**
```
Challenge Creator (User)
         ↓
   Nhập Challenge (Title + Description + Expected Solution)
         ↓
   GeminiAIClient.AnalyzeChallengeAsync()
         ↓
   Gemini AI xử lý & sinh ra:
      - Difficulty Level (1-10)
      - Evaluation Criteria (danh sách các tiêu chí)
      - Skill Weights (kĩ năng cần thiết)
         ↓
   Lưu vào Database:
      - EvaluationCriteria table
      - CriteriaSkillMapping table
      - ChallengeVersion.SkillWeightMapping
```

#### **2. Qui Trình Tạo Tiêu Chí Chi Tiết**

**File**: `Challenge.Infrastructure/Clients/GeminiAIClient.cs` (lines 28-98)

**Prompt được gửi tới AI:**
```
Analyze this challenge and provide:
1. Difficulty level (1-10)
2. Required PROFESSIONAL COMPETENCIES (reusable skills) with weights
3. Evaluation criteria (measurable, specific to this challenge version)
```

**AI trả về JSON:**
```json
{
  "difficultyLevel": 7,
  "difficultyLabel": "Hard",
  "skills": {
    "C#": 3,
    "ASP.NET Core": 4,
    "SignalR": 5
  },
  "criteria": [
    "SignalR implementation",
    "Concurrency handling",
    "Error handling"
  ]
}
```

#### **3. Luật Tạo Tiêu Chí (AI Generation Rules)**

AI được **ràng buộc bởi các luật nghiêm ngặt** để tạo tiêu chí chất lượng:

**Một tiêu chí hợp lệ PHẢI:**
1. **REUSABLE** - Có thể dùng cho nhiều thử thách, không riêng biệt
2. **MEASURABLE** - Có thể định lượng được bởi AI
3. **CANONICAL** - Sử dụng tên tiêu chuẩn trong ngành
4. **MID-LEVEL** - Không quá rộng (Programming) hay quá hẹp (If Statement)
5. **NO CHALLENGE-SPECIFIC** - Tránh tên riêng của thử thách
6. **SHORT NAMES** - Tên ngắn, rõ ràng
7. **NO SOFT LABELS** - Tránh khái niệm mơ hồ

**Ví dụ GOOD Tiêu Chí:**
- C#
- ASP.NET Core
- SignalR
- Database Design
- REST API Design
- Authentication
- Unit Testing
- Entity Framework Core

**Ví dụ BAD Tiêu Chí (AI sẽ KHÔNG sinh ra):**
- ❌ SecurityEngineering → ✅ Web Security
- ❌ PasswordHashing → ✅ Cryptography
- ❌ Persistence Mechanisms → ✅ Database Design
- ❌ Chat App Logic → ✅ SignalR
- ❌ Good Coding → ❌ (quá mơ hồ)

---

## 🔗 Câu Hỏi 2: Làm thế nào để biết kĩ năng nhất định sẽ ảnh hưởng tới các thử thách?

### 📊 Mapping Giữa Kĩ Năng và Tiêu Chí

#### **1. Cấu Trúc Dữ Liệu**

**Database Schema:**
```
EvaluationCriteria
├── Id (Guid)
├── Name (string) - e.g., "SignalR implementation"
├── Description (string)
├── CreatedAt, UpdatedAt

CriteriaSkillMapping
├── Id (Guid)
├── CriteriaId → EvaluationCriteria.Id
├── SkillId → Skill.Id
├── Weight (decimal) - 0.1 to 10.0
└── CreatedAt

Skill
├── Id (Guid)
├── Name (string) - e.g., "C#", "SignalR"
├── Category (string) - e.g., "Backend", "Database"
├── CreatedAt
```

#### **2. Ví Dụ Cụ Thể**

**Challenge: "Build Real-time Chat with SignalR"**

| Kĩ Năng | Tiêu Chí | Weight | Ảnh Hưởng |
|---------|---------|--------|----------|
| C# | Code Quality | 3.0 | Trung Bình |
| ASP.NET Core | API Structure | 3.5 | Trung Bình |
| SignalR | Real-time Handling | 5.0 | **Cao** |
| Database Design | Data Persistence | 3.0 | Trung Bình |
| Error Handling | Exception Management | 4.0 | Cao |

**Công thức Ảnh Hưởng:**
```
Skill Impact Score = (Weight / Total Weights) * 100
                   = (5.0 / 18.5) * 100 = 27%

SignalR ảnh hưởng 27% đến việc đánh giá Challenge này
```

#### **3. Code Implementation**

**File**: `Challenge.Domain/Entities/CriteriaSkillMapping.cs`
```csharp
public class CriteriaSkillMapping
{
    public Guid CriteriaId { get; set; }      // "SignalR implementation"
    public Guid SkillId { get; set; }         // "SignalR" skill
    public decimal Weight { get; set; }       // 5.0 (cao)
}
```

**Repository**: `CriteriaSkillMappingRepository`
```csharp
// Lấy tất cả skills ảnh hưởng đến một tiêu chí
var mappings = await _repository
    .GetByCriteriaIdAsync(criteriaId);

foreach (var mapping in mappings)
{
    Console.WriteLine($"{mapping.Skill.Name}: Weight = {mapping.Weight}");
    // SignalR: Weight = 5.0
    // Concurrency Control: Weight = 4.0
}
```

#### **4. Scoring Logic - Làm thế nào Weight ảnh hưởng đến Điểm**

**File**: `Challenge.Application/Services/GradingServiceImpl.cs`

**Công thức tính điểm:**
```
CriteriaScore = (SkillMastery × SkillWeight) + (SubmissionQuality × CriteriaWeight)

Ví dụ:
  SignalR Mastery = 8/10 (user đã học được 80%)
  SignalR Weight = 5.0
  
  Contribution = (8 × 5) / (tổng weight) = 40 / 18.5 = 21.6 điểm từ SignalR
```

---

## 🤖 Câu Hỏi 3: Các Prompt để AI Duyệt Nội Dung như Thế Nào, AI Chấm Điểm như Thế Nào?

### 📝 AI Prompts - Chi Tiết Đầy Đủ

#### **A. PROMPT 1: Challenge Analysis (Phân Tích Thử Thách)**

**File**: `Challenge.Infrastructure/Clients/GeminiAIClient.cs` (line 34-83)

**Khi User Tạo Challenge, AI được gửi:**

```
Analyze this challenge and provide:
1. Difficulty level (1-10)
2. Required PROFESSIONAL COMPETENCIES (reusable skills) with weights
3. Evaluation criteria (measurable, specific to this challenge version)

=== CRITICAL SKILL GENERATION RULES ===
Generate ONLY canonical, reusable, measurable professional competencies.

A valid skill MUST be:
1. REUSABLE - works across many challenges (NOT challenge-specific)
2. MEASURABLE - can realistically be evaluated by AI
3. CANONICAL - use standard industry/professional naming
4. MID-LEVEL - not too broad (Programming) or too narrow (If Statement)
5. NO CHALLENGE-SPECIFIC WORDING - avoid scenario labels
6. SHORT NAMES - competency names, not sentences/explanations
7. NO SOFT LABELS - avoid vague non-measurable concepts

GOOD SKILLS (examples):
- C#, ASP.NET Core, SignalR, Database Design, REST API Design, Authentication, 
  Cryptography, Unit Testing, Entity Framework Core

BAD SKILLS (DO NOT GENERATE):
- SecurityEngineering (use "Web Security")
- PasswordHashing (use "Cryptography")
- Good Coding (vague, non-measurable)

Challenge Description:
[USER CHALLENGE TEXT]

Expected Solution:
[USER EXPECTED SOLUTION]

Respond in this exact JSON format:
{
  "difficultyLevel": <number 1-10>,
  "difficultyLabel": "<Easy/Medium/Hard/Expert>",
  "skills": {"skillName": <weight as number>, ...},
  "criteria": ["criterion1", "criterion2", ...]
}
```

**Ví Dụ Request -> Response:**

**Request Input:**
```
Challenge Title: Real-time Chat Application
Challenge Description: Build a real-time chat app using SignalR that supports:
- Multiple connected users
- Message broadcasting
- User presence tracking
- Connection management

Expected Solution: Solution should use SignalR hubs, async/await patterns, 
and proper error handling for disconnections.
```

**AI Response:**
```json
{
  "difficultyLevel": 7,
  "difficultyLabel": "Hard",
  "skills": {
    "C#": 3,
    "ASP.NET Core": 4,
    "SignalR": 5,
    "Async Programming": 4,
    "Error Handling": 3
  },
  "criteria": [
    "SignalR Hub Implementation",
    "Concurrency and State Management",
    "Connection Lifecycle Management",
    "Error Handling and Recovery",
    "Code Structure and Best Practices"
  ]
}
```

---

#### **B. PROMPT 2: Submission Grading (Chấm Bài)**

**File**: `Challenge.Infrastructure/Clients/GeminiAIClient.cs` (line 100-143)

**Khi User Submit Bài Giải, AI được gửi:**

```
Grade this code submission against the provided criteria. 
Score each criterion from 0-10.

Challenge:
Title: [CHALLENGE TITLE]
Description: [CHALLENGE DESCRIPTION]
Expected Solution: [EXPECTED SOLUTION]

Evaluation Criteria:
1. [Criterion 1]
2. [Criterion 2]
3. [Criterion 3]
...

User Submission:
[USER'S CODE/SOLUTION]

Respond in this exact JSON format:
{
  "overallScore": <0-10>,
  "criteriaScores": {"criterion_name": <0-10>, ...},
  "feedback": "Detailed feedback about the submission",
  "strengths": ["strength1", "strength2"],
  "improvements": ["improvement1", "improvement2"]
}
```

**Ví Dụ Request -> Response:**

**Request:**
```
Challenge Title: Real-time Chat with SignalR
Description: Build a real-time chat application...
Expected Solution: Use SignalR hubs with proper async handling...

Evaluation Criteria:
1. SignalR Hub Implementation
2. Concurrency and State Management
3. Connection Lifecycle Management
4. Error Handling and Recovery
5. Code Structure and Best Practices

User Submission:
[USER CODE - có thể là C# code, configuration files, etc.]
```

**AI Response:**
```json
{
  "overallScore": 8.5,
  "criteriaScores": {
    "SignalR Hub Implementation": 9,
    "Concurrency and State Management": 8,
    "Connection Lifecycle Management": 8.5,
    "Error Handling and Recovery": 8,
    "Code Structure and Best Practices": 8.5
  },
  "feedback": "Well-structured SignalR implementation with good error handling. 
             Consider improving concurrency patterns with locks or semaphores 
             for shared state management.",
  "strengths": [
    "Clear hub methods with proper async/await",
    "Good use of groups for message broadcasting",
    "Proper connection tracking with dictionaries"
  ],
  "improvements": [
    "Add thread-safe collections for concurrent access",
    "Implement graceful shutdown handling",
    "Add logging for connection lifecycle events"
  ]
}
```

---

#### **C. AI Configuration Parameters**

**File**: `Challenge.Infrastructure/Clients/GeminiAIClient.cs` (line 145-182)

```csharp
var request = new
{
    contents = new[] { /* prompt */ },
    generationConfig = new
    {
        temperature = 0.7,              // Moderate creativity (not too strict)
        topP = 0.9,                     // Diversity
        topK = 40,                      // Vocabulary range
        maxOutputTokens = 2048,         // Detailed responses
        responseMimeType = "application/json"  // Structured output
    },
    safetySettings = new[]
    {
        new { 
            category = "HARM_CATEGORY_HARASSMENT",
            threshold = "BLOCK_MEDIUM_AND_ABOVE" 
        },
        new { 
            category = "HARM_CATEGORY_HATE_SPEECH",
            threshold = "BLOCK_MEDIUM_AND_ABOVE" 
        }
    }
};
```

---

### 🎯 Scoring Flow Chi Tiết

#### **Step 1: Sanitization (Làm Sạch Input)**
```
User Submission → IPromptSanitizationService.SanitizePromptAsync()
   ↓
[Remove malicious content, normalize formatting]
   ↓
Clean Submission
```

#### **Step 2: Extract Criteria**
```csharp
// Từ ChallengeVersion.SkillWeightMapping JSON
var criteria = new List<string> 
{ 
    "SignalR implementation",
    "Concurrency handling",
    "Error handling" 
};
```

#### **Step 3: Build Challenge Context**
```csharp
string context = $"
Title: Real-time Chat with SignalR
Description: [challenge description]
Expected Solution: [expected solution]
";
```

#### **Step 4: Call Gemini AI**
```csharp
var gradingResult = await _geminiClient.GradeSubmissionAsync(
    challengeContext,
    criteria,
    sanitizedSubmission
);
```

#### **Step 5: Store Results**
```csharp
var submission = new ChallengeSubmission
{
    UserId = userId,
    ChallengeVersionId = versionId,
    SubmissionText = userCode,
    OverallScore = 8.5,
    Feedback = "Well-structured implementation...",
    SubmittedAt = DateTime.UtcNow
};

// Lưu điểm cho mỗi tiêu chí
foreach (var (criterion, score) in gradingResult.CriteriaScores)
{
    var criteriaScore = new SubmissionCriteriaScore
    {
        SubmissionId = submission.Id,
        CriteriaName = criterion,
        Score = score,
        Feedback = gradingResult.Feedback
    };
}
```

---

## 🎨 Câu Hỏi 4 (Bonus): Thương Hiệu Cá Nhân Của Dự Án Thể Hiện Ở Đâu?

### **1. Project Name & Branding**

| Yếu Tố | Giá Trị |
|--------|--------|
| **Official Name** | Recruitment Platform |
| **Short Name** | Skill Snap (từ repo context) |
| **Type** | Microservices Architecture |
| **Theme** | Professional Skill Assessment & Matching |

### **2. Technology Stack (Tech Branding)**

```
Backend Stack:
├── ASP.NET Core 8.0    (Modern, Enterprise-Grade)
├── Microservices       (Scalable Architecture)
├── SQL Server          (Enterprise Database)
├── RabbitMQ            (Event-Driven)
├── Redis               (High Performance)
├── SignalR             (Real-time Communication)
├── Docker              (Cloud-Native)
└── YARP                (Modern API Gateway)

Frontend (Implied):
├── Vercel Deployment   (from README line 210)
└── React/Next.js       (likely)
```

### **3. Core Features (Functional Branding)**

```
Unique Capabilities:
- ✅ AI-Powered Skill Assessment (Gemini AI)
- ✅ Real-time Notifications (SignalR)
- ✅ Microservices Architecture (10 independent services)
- ✅ Skill-Challenge Mapping (CriteriaSkillMapping)
- ✅ Automated Evaluation (Prompt Sanitization + AI Grading)
- ✅ Portfolio Integration (Multiple template support)
- ✅ Company Recruitment Tools (Job posting, applications)
```

### **4. UI/UX Branding Elements (Inferred)**

```
From codebase references:
- Color Palette: Blues and professional neutrals (ASP.NET Core default)
- Typography: Modern, clean (Swagger default UI)
- Tone: Professional, structured, technical
- Key Pages:
  ├── Challenge Library
  ├── Real-time Notifications
  ├── Portfolio Showcase
  ├── Company Dashboard
  └── Skill Assessment Results
```

### **5. Database Branding (Schema Naming)**

```
Service Naming Pattern:
[ServiceName]ServiceDb

Examples:
- AuthServiceDb
- PortfolioServiceDb
- ChallengeDb
- NotificationServiceDb
- CompanyServiceDb

Convention:
Service → [Service].API
Database → [Service]ServiceDb
```

### **6. Where Brand Appears**

```
Frontend (Web):
- Vercel deployment: https://sep-490-web-fork.vercel.app
- CORS configuration (line 210 in Program.cs)
- Logo & theme colors (frontend repository)

Backend:
- README.md (Project introduction)
- Swagger UI (each service at /swagger)
- API Response Headers (RecruitmentPlatform identifier)
- Log Messages (brand mentions in logs)

Documentation:
- Service descriptions
- API endpoints naming
- Database schema

Deployment:
- ACR: skillsnapacr2604282023545
- Azure Resource Group: skillsnap-rg-*
- Docker image tags
```

---

## 📚 Summary Table

| Khía Cạnh | Chi Tiết |
|-----------|---------|
| **Tiêu Chí Từ Đâu** | AI Gemini sinh ra tự động từ challenge description |
| **Kĩ Năng Ảnh Hưởng Như Thế** | Qua CriteriaSkillMapping với Weight system (0-10) |
| **AI Duyệt Nội Dung** | 2 Prompts: Challenge Analysis + Submission Grading |
| **Chấm Điểm** | Gemini AI theo JSON schema, lưu CriteriaScores |
| **Thương Hiệu** | Recruitment Platform, Skill Snap, Modern Tech Stack |

---

**Generated**: 2026-05-25 16:17:17 UTC+7  
**Status**: ✅ Complete Analysis
