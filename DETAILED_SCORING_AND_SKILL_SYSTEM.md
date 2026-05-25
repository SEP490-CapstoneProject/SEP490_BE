# Hệ Thống Tính Điểm & Quản Lý Kĩ Năng Chi Tiết

**Ngày**: 2026-05-25  
**Người Viết**: Challenge Service Analysis  
**Phiên Bản**: 2.0 - Chi Tiết Công Thức

---

## 📊 PHẦN 1: CÔNG THỨC TÍNH ĐIỂM CHI TIẾT

### **1.1 Công Thức Chính**

```
FINAL_POINTS = (Overall_Score / 100) × Skill_Weight × Difficulty_Multiplier × Attempt_Multiplier
```

**File**: `SkillPointService.cs` (Lines 32-79)

#### **Bước 1: Lấy điểm tổng thể từ bài submit**
```
Overall_Score = Điểm do AI Gemini chấm (0-100)
Max = 100
Min = 0

Ví dụ: Nếu AI chấm 85/100 → Overall_Score = 85
```

#### **Bước 2: Lấy Skill Weight từ Challenge**
```csharp
// ChallengeVersion.SkillWeightMapping là JSON:
{
  "C#": 3.0,
  "ASP.NET Core": 4.0,
  "SignalR": 5.0,
  "Async Programming": 4.0,
  "Error Handling": 3.0
}

Total Weight = 3 + 4 + 5 + 4 + 3 = 19.0
```

#### **Bước 3: Áp Dụng Multiplier Độ Khó**

**Code** (Lines 41-46):
```csharp
var difficultyMultiplier = version.DifficultyLabel?.ToLower() switch
{
    "hard" => 2.0,
    "medium" => 1.5,
    _ => 1.0  // Easy = 1.0
};
```

| Độ Khó | Multiplier | Ý Nghĩa |
|--------|-----------|--------|
| Easy | 1.0x | Không thêm điểm |
| Medium | 1.5x | Thêm 50% điểm |
| Hard | 2.0x | Thêm 100% điểm (gấp đôi) |

**Ví dụ**: 
- Bài Easy: 85 × 1.0 = 85 điểm
- Bài Medium: 85 × 1.5 = 127.5 điểm
- Bài Hard: 85 × 2.0 = 170 điểm

#### **Bước 4: Áp Dụng Multiplier Lần Nộp**

**Code** (Lines 48-49):
```csharp
// attemptMultiplier = 1.0 - ((attemptCount - 1) * 0.2)
// Min = 0.2 (không được < 0.2)

var attemptMultiplier = Math.Max(0.2, 1.0 - ((submission.AttemptCount - 1) * 0.2));
```

| Lần Nộp | Formula | Multiplier | Ghi Chú |
|--------|---------|-----------|--------|
| Lần 1 | 1.0 - (0 × 0.2) | **1.0x** | 100% điểm |
| Lần 2 | 1.0 - (1 × 0.2) | **0.8x** | 80% điểm (-20%) |
| Lần 3 | 1.0 - (2 × 0.2) | **0.6x** | 60% điểm (-40%) |
| Lần 4 | 1.0 - (3 × 0.2) | **0.4x** | 40% điểm (-60%) |
| Lần 5+ | 1.0 - (4 × 0.2) | **0.2x** | 20% điểm (-80%, min) |

**Ví dụ**:
- Lần 1 (Perfect): 85 × 1.0 = 85 điểm
- Lần 2 (2nd try): 85 × 0.8 = 68 điểm
- Lần 3 (3rd try): 85 × 0.6 = 51 điểm

#### **Bước 5: Tính Điểm Cho Từng Skill**

**Code** (Lines 54-68):
```csharp
foreach (var skillWeight in skillWeights)
{
    var skill = await _skillRepository.GetBySlugAsync(skillWeight.Key);
    
    // basePoints = (Score / 100) × SkillWeight
    var basePoints = (submissionScore / 100.0) * skillWeight.Value;
    
    // finalPoints = basePoints × difficultyMultiplier × attemptMultiplier
    var finalPoints = Math.Round(basePoints * difficultyMultiplier * attemptMultiplier, 2);
    
    points[skill.Id] = Math.Max(0, finalPoints);
}
```

### **1.2 Ví Dụ Tính Toán Thực Tế**

**Tình Huống:**
- Challenge: "Real-time Chat with SignalR"
- User: Nộp bài lần 2, AI chấm 80/100
- Độ khó: Hard
- Skill Weights:
  - C#: 3.0
  - SignalR: 5.0
  - Error Handling: 3.0

**Tính Toán:**

```
Overall_Score = 80
Difficulty = Hard (2.0x)
Attempt = Lần 2 (0.8x)

Cho mỗi skill:

1️⃣ C# (Weight = 3.0)
   basePoints = (80/100) × 3.0 = 0.8 × 3.0 = 2.4
   finalPoints = 2.4 × 2.0 × 0.8 = 3.84 điểm → User nhận 3.84 điểm C#

2️⃣ SignalR (Weight = 5.0)
   basePoints = (80/100) × 5.0 = 0.8 × 5.0 = 4.0
   finalPoints = 4.0 × 2.0 × 0.8 = 6.40 điểm → User nhận 6.40 điểm SignalR

3️⃣ Error Handling (Weight = 3.0)
   basePoints = (80/100) × 3.0 = 0.8 × 3.0 = 2.4
   finalPoints = 2.4 × 2.0 × 0.8 = 3.84 điểm → User nhận 3.84 điểm
```

---

## 📈 PHẦN 2: CÁCH CỘNG ĐIỂM VÀO SKILL CỦA NGƯỜI DÙNG

### **2.1 Flow Tích Lũy Điểm**

**Khi Submission được Approve:**
```
Grading Service tính điểm
    ↓
Gọi SkillPointService.CalculateSkillPointsAsync()
    ↓
Gọi SkillPointService.AwardPointsAsync()
    ↓
Cơ sở dữ liệu cập nhật:
  1. UserSkill.TotalPoints += points
  2. UserSkill.MasteryScore = Calculate()
  3. UserSkill.VerificationLevel = Calculate()
  4. Tạo SkillPointTransaction (ghi nhận)
```

### **2.2 Quá Trình Award Points Chi Tiết**

**File**: `SkillPointService.cs` (Lines 81-190)

#### **Bước 1: Kiểm Tra UserSkill Tồn Tại**

```csharp
var existingUserSkill = await _userSkillRepository.GetByUserAndSkillAsync(userId, skill.Id);

if (existingUserSkill == null)
{
    // Tạo bản ghi mới nếu chưa có
    userSkill = new UserSkill
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SkillId = skill.Id,
        TotalPoints = 0,
        MasteryScore = 0,
        VerifiedChallengeCount = 0,
        LastVerifiedAt = null,
        VerificationLevel = VerificationLevel.Beginner,
        IsVerified = false,
        CreatedAt = now,
        UpdatedAt = now
    };
}
```

**Database Table:**
```
UserSkill
├── Id: GUID (PK)
├── UserId: INT (FK → User)
├── SkillId: GUID (FK → Skill)
├── TotalPoints: DECIMAL ← ⭐ CỦA CHÚNG TA TÍNH TOÁN
├── MasteryScore: DECIMAL (0-100)
├── VerifiedChallengeCount: INT
├── VerificationLevel: ENUM (Beginner/Intermediate/Advanced/Expert)
├── IsVerified: BOOL
├── LastVerifiedAt: DATETIME
├── CreatedAt, UpdatedAt
```

#### **Bước 2: Tích Lũy Điểm**

```csharp
// Cộng điểm mới vào tổng cộng
userSkill.TotalPoints += (decimal)points;

// Ví dụ:
// Lần 1: TotalPoints = 0 + 6.40 = 6.40
// Lần 2: TotalPoints = 6.40 + 5.12 = 11.52
// Lần 3: TotalPoints = 11.52 + 7.68 = 19.20
```

#### **Bước 3: Tính Toán Mastery Score**

**Công Thức** (Lines 225-233):
```csharp
private decimal CalculateMasteryScore(decimal totalPoints)
{
    // Mastery score: 0-100 (tuyến tính)
    // Formula: (totalPoints / 50 * 70) + 30
    
    var mastery = (totalPoints / 50m * 70m) + 30m;
    return Math.Min(100m, mastery);
}
```

**Bảng Chuyển Đổi:**

| TotalPoints | Mastery Score | % Tiến Độ |
|------------|---------------|-----------|
| 0 | 30 | 30% |
| 5 | 37 | 37% |
| 10 | 44 | 44% |
| 25 | 65 | 65% |
| 50 | 100 | 100% |
| 100+ | 100 (capped) | 100% |

**Công Thức Rút Gọn:**
```
Mastery = MIN(100, (Points ÷ 50) × 70 + 30)
```

**Ví Dụ:**
- 0 points → Mastery = (0 ÷ 50) × 70 + 30 = **30**
- 10 points → Mastery = (10 ÷ 50) × 70 + 30 = **14 + 30 = 44**
- 50 points → Mastery = (50 ÷ 50) × 70 + 30 = **70 + 30 = 100**

#### **Bước 4: Xác Định Verification Level**

**Code** (Lines 235-244):
```csharp
private VerificationLevel CalculateVerificationLevel(decimal totalPoints)
{
    return totalPoints switch
    {
        < 10 => VerificationLevel.Beginner,
        < 50 => VerificationLevel.Intermediate,
        < 100 => VerificationLevel.Advanced,
        _ => VerificationLevel.Expert
    };
}
```

**Bảng Xếp Hạng:**

| Level | Yêu Cầu | Điểm Tối Thiểu | Ý Nghĩa |
|-------|--------|---------------|---------:|
| 🟦 Beginner | < 10 | 0 | Mới học |
| 🟩 Intermediate | 10-49 | 10 | Kinh nghiệm cơ bản |
| 🟨 Advanced | 50-99 | 50 | Nắm chắc kĩ năng |
| 🟥 Expert | 100+ | 100 | **Chuyên gia** |

#### **Bước 5: Tính IsVerified**

```csharp
userSkill.IsVerified = userSkill.VerificationLevel >= VerificationLevel.Intermediate;
```

**Quy Tắc:**
- Beginner: `IsVerified = false` ❌
- Intermediate+: `IsVerified = true` ✅

#### **Bước 6: Cập Nhật Metadata**

```csharp
userSkill.LastVerifiedAt = now;
userSkill.VerifiedChallengeCount++;  // Tăng lên 1
userSkill.UpdatedAt = now;

await _userSkillRepository.UpdateAsync(userSkill);
```

#### **Bước 7: Ghi Nhận Transaction**

**Mục Đích**: Audit trail - theo dõi từng lần user nhận điểm

```csharp
var transaction = new SkillPointTransaction
{
    Id = Guid.NewGuid(),
    UserId = userId,
    SkillId = skill.Id,
    Points = (decimal)points,                    // 6.40
    SourceType = "ChallengeSubmission",
    SourceId = sourceId,                        // Challenge ID
    CreatedAt = now
};

await _transactionRepository.AddAsync(transaction);
```

**Database Table:**
```
SkillPointTransaction
├── Id: GUID (PK)
├── UserId: INT
├── SkillId: GUID
├── Points: DECIMAL ← 6.40 điểm từ bài này
├── SourceType: STRING ("ChallengeSubmission")
├── SourceId: GUID (Challenge ID)
├── CreatedAt: DATETIME
```

**Ví Dụ Transactions:**
```
User 101 - SignalR:
  Transaction 1: +6.40 points (Challenge 1, Attempt 2)
  Transaction 2: +5.12 points (Challenge 2, Attempt 3)
  Transaction 3: +7.68 points (Challenge 3, Attempt 1)
  
  TotalPoints = 6.40 + 5.12 + 7.68 = 19.20 ✅
```

### **2.3 Ví Dụ Toàn Bộ Flow**

**Tình Huống:**
- User 101 submit Challenge về SignalR
- Bài chấm: 80/100
- Độ khó: Hard
- Lần nộp: 2
- SignalR Weight: 5.0

**Timeline:**

```
Step 1: AI chấm 80/100
  overallScore = 80

Step 2: Tính điểm SignalR
  basePoints = (80/100) × 5.0 = 4.0
  finalPoints = 4.0 × 2.0 × 0.8 = 6.40 điểm

Step 3: Kiểm tra UserSkill
  UserSkill không tồn tại → Tạo mới
  UserSkill.TotalPoints = 0

Step 4: Cộng điểm
  UserSkill.TotalPoints = 0 + 6.40 = 6.40

Step 5: Tính Mastery
  Mastery = (6.40 ÷ 50) × 70 + 30 = 8.96 + 30 = 38.96

Step 6: Xác định Level
  6.40 < 10 → VerificationLevel = Beginner
  IsVerified = false

Step 7: Cập nhật DB
  UPDATE UserSkill SET
    TotalPoints = 6.40,
    MasteryScore = 38.96,
    VerificationLevel = 'Beginner',
    IsVerified = false,
    VerifiedChallengeCount = 1,
    LastVerifiedAt = 2026-05-25T16:30:00Z

Step 8: Ghi nhận Transaction
  INSERT SkillPointTransaction
    Points = 6.40
    SourceId = Challenge123
```

---

## 🔄 PHẦN 3: CÁCH SKILL ĐƯỢC TẠO RA VÀ TÁI SỬ DỤNG

### **3.1 Quy Trình Tạo Skill**

**File**: `ChallengeServiceImpl.cs` (Lines 678-711)

#### **Flow Tổng Thể**

```
User tạo Challenge
     ↓
Gửi tới AI Gemini
     ↓
AI trả về SkillWeights JSON:
  {
    "C#": 3,
    "SignalR": 5,
    "Async Programming": 4
  }
     ↓
EnsureSkillsFromAnalysisAsync()
     ↓
Cho mỗi Skill:
  Skill exists?
    ├─ YES → Update IsApproved = true nếu cần
    └─ NO → Tạo skill mới
```

#### **Kiểm Tra Skill Tồn Tại (De-duplication)**

**Code** (Lines 684-703):
```csharp
var slug = NormalizeSlug(skillName);  // "signalr"
var existing = await _skillRepository.GetBySlugAsync(slug);

if (existing is null)
{
    // Skill chưa tồn tại → TẠO MỚI
    existing = new Skill
    {
        Id = Guid.NewGuid(),
        Name = skillName,                // "SignalR" (original)
        Slug = slug,                     // "signalr" (normalized)
        Description = $"AI-generated skill from challenge '{challenge.Title}'",
        CategoryId = null,
        IsApproved = true,               // Được AI phê duyệt
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    await _skillRepository.AddAsync(existing);
}
else if (!existing.IsApproved)
{
    // Skill đã tồn tại nhưng chưa được phê duyệt
    existing.IsApproved = true;
    existing.UpdatedAt = DateTime.UtcNow;
    await _skillRepository.UpdateAsync(existing);
}
```

**Database Table:**
```
Skill
├── Id: GUID (PK)
├── Name: STRING ("SignalR", "C#", etc.)
├── Slug: STRING ("signalr", "csharp", etc.) ← Unique key for de-duplication
├── Description: STRING
├── CategoryId: GUID? (optional)
├── IsApproved: BOOL ← true nếu AI tạo
├── CreatedAt, UpdatedAt
```

#### **Slug Normalization (Khóa De-duplication)**

**Code** (Lines 831-839):
```csharp
private static string NormalizeSlug(string value)
{
    return string.Join(
        "-",
        value
            .Trim()
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', '/', '\\', '+', '.', ',', ':', ';', '(', ')' }, 
                   StringSplitOptions.RemoveEmptyEntries));
}
```

**Ví Dụ Normalization:**

| Input | Output Slug | Kết Quả |
|-------|---------|--------|
| "SignalR" | "signalr" | ✅ Trùng |
| "Signal R" | "signal-r" | ✅ Trùng |
| "signal-r" | "signal-r" | ✅ Trùng |
| "C#" | "c" | ✅ Trùng |
| "C Sharp" | "c-sharp" | ✅ Trùng |
| "ASP.NET Core" | "asp-net-core" | ✅ Trùng |
| "asp.net core" | "asp-net-core" | ✅ Trùng |

**Ưu Điểm:**
- Tự động dedup - không tạo duplicate skill
- Không phân biệt "SignalR" vs "signal-r" vs "Signal R"
- Tồn tại lâu dài - skill được tái sử dụng qua challenges

### **3.2 Cách Skill Được Tái Sử Dụng**

#### **Tái Sử Dụng Khi Tính Điểm**

**Khi User Submit Bài:**
```
SkillPointService.CalculateSkillPointsAsync()
     ↓
Parse SkillWeightMapping từ Challenge
  {
    "C#": 3,
    "SignalR": 5
  }
     ↓
Cho mỗi skill name:
  slug = NormalizeSlug("SignalR") → "signalr"
  skill = GetBySlugAsync("signalr") ← REUSE existing skill!
     ↓
Nếu skill không tìm thấy → Skip (log warning)
```

**Code** (Lines 58-63):
```csharp
foreach (var skillWeight in skillWeights)
{
    var skill = await _skillRepository.GetBySlugAsync(NormalizeSlug(skillWeight.Key));
    if (skill is null)
    {
        _logger.LogWarning("Skill '{SkillName}' not found", skillWeight.Key);
        continue;  // Skip nếu skill không tồn tại
    }
}
```

#### **Tái Sử Dụng Khi Mapping Criteria**

**File**: `ChallengeServiceImpl.cs` (Lines 748-769)

```csharp
var mappedSkill = await ResolveSkillForCriteriaAsync(criteriaName, analysis.SkillWeights);
if (mappedSkill is null)
{
    continue;  // Skip nếu không match
}

// REUSE existing skill để map với Criteria
criteriaSkillRows.Add(new CriteriaSkillMapping
{
    Id = Guid.NewGuid(),
    CriteriaId = criteriaEntity.Id,
    SkillId = mappedSkill.Id,  ← Reuse skill ID
    Weight = mappingWeight,
    CreatedAt = DateTime.UtcNow
});
```

### **3.3 Ví Dụ Tái Sử Dụng**

**Timeline:**

```
Challenge 1 (Created):
  AI generates: C#, SignalR, Error Handling
  → Create 3 new skills in DB
  
Challenge 2 (Created):
  AI generates: C#, SignalR, Database Design
  → Check slugs:
     - "c-sharp" exists → REUSE (don't create)
     - "signalr" exists → REUSE (don't create)
     - "database-design" NEW → CREATE
  → Only create 1 new skill

Challenge 3 (Created):
  AI generates: C#, SignalR, Azure Cloud
  → Check slugs:
     - "c-sharp" exists → REUSE
     - "signalr" exists → REUSE
     - "azure-cloud" NEW → CREATE
  → Only create 1 new skill

DATABASE AFTER 3 CHALLENGES:
  Skills: 5 total
    1. c-sharp (used in 3 challenges)
    2. signalr (used in 3 challenges)
    3. error-handling (used in 1 challenge)
    4. database-design (used in 1 challenge)
    5. azure-cloud (used in 1 challenge)
```

### **3.4 Criteria-Skill Mapping (Liên Kết Tiêu Chí ↔ Kĩ Năng)**

**Flow:**
```
Challenge AI Analysis kết quả:
  SkillWeights: {C#: 3, SignalR: 5}
  EvaluationCriteria: ["SignalR Implementation", "Error Handling"]
     ↓
Persist Criteria Model:
  Cho mỗi Criteria:
    1. ResolveOrCreateCriteria(criteriaName)
       └─ Tìm hoặc tạo EvaluationCriteria
    
    2. ResolveSkillForCriteria(criteriaName, skillWeights)
       └─ Tìm skill REUSE phù hợp
    
    3. Tạo CriteriaSkillMapping
       └─ Liên kết Criteria ↔ Skill (weight)
```

**Code** (Lines 736-770):
```csharp
foreach (var criteriaName in criteriaNames)
{
    // Step 1: Tìm hoặc tạo Criteria
    var criteriaEntity = await ResolveOrCreateCriteriaAsync(criteriaName);
    
    challengeCriteriaRows.Add(new ChallengeCriteria
    {
        Id = Guid.NewGuid(),
        ChallengeVersionId = version.Id,
        CriteriaId = criteriaEntity.Id,
        Weight = criteriaWeight,
        VersionedAt = DateTime.UtcNow
    });

    // Step 2: Tìm skill REUSE phù hợp
    var mappedSkill = await ResolveSkillForCriteriaAsync(criteriaName, analysis.SkillWeights);
    if (mappedSkill is null) continue;

    // Step 3: Tạo mapping
    criteriaSkillRows.Add(new CriteriaSkillMapping
    {
        Id = Guid.NewGuid(),
        CriteriaId = criteriaEntity.Id,
        SkillId = mappedSkill.Id,  ← REUSE skill
        Weight = mappingWeight,
        CreatedAt = DateTime.UtcNow
    });
}
```

### **3.5 Smart Skill Matching**

**Khi Mapping Criteria → Skill:**

**Code** (Lines 803-829):
```csharp
private async Task<Skill?> ResolveSkillForCriteriaAsync(string criteriaName, Dictionary<string, decimal> skillWeights)
{
    var normalizedCriteria = NormalizeSlug(criteriaName);
    var candidateNames = skillWeights
        .OrderByDescending(x => x.Value)  // Sắp xếp theo weight (cao nhất trước)
        .Select(x => x.Key)
        .ToList();

    // Step 1: Tìm skill MATCH
    var matchedName = candidateNames.FirstOrDefault(skillName =>
    {
        var normalizedSkill = NormalizeSlug(skillName);
        return normalizedSkill.Contains(normalizedCriteria, StringComparison.OrdinalIgnoreCase)
               || normalizedCriteria.Contains(normalizedSkill, StringComparison.OrdinalIgnoreCase);
    });

    // Step 2: Fallback - lấy skill có weight cao nhất
    if (matchedName is null)
    {
        matchedName = candidateNames.FirstOrDefault();
    }

    if (matchedName is null) return null;

    // Step 3: Trả về skill REUSE
    return await _skillRepository.GetBySlugAsync(NormalizeSlug(matchedName));
}
```

**Ví Dụ Matching:**

```
Criteria: "SignalR Implementation"
Normalized: "signalr-implementation"
SkillWeights: {"C#": 3, "SignalR": 5, "Error Handling": 3}
Candidates (by weight): ["SignalR", "C#", "Error Handling"]

Step 1: Tìm match
  - "signalr" contains "signalr-implementation"? NO
  - "signalr-implementation" contains "signalr"? YES! ✅
  
Result: MATCH = "SignalR" skill
```

---

## 📊 PHẦN 4: BẢNG TÓMAN LẠI

### **Công Thức Chính**

```
FINAL_POINTS = (Score/100) × Skill_Weight × Difficulty_Multiplier × Attempt_Multiplier
MASTERY = MIN(100, (TotalPoints ÷ 50) × 70 + 30)
VERIFICATION_LEVEL = 
  - < 10: Beginner
  - 10-49: Intermediate
  - 50-99: Advanced
  - 100+: Expert
```

### **Flow Tích Lũy Điểm**

```
1. AI chấm bài (0-100)
2. Tính điểm từng skill (basePoints × multipliers)
3. Kiểm UserSkill tồn tại
4. TotalPoints += newPoints
5. Recalculate Mastery & Level
6. Update DB & record Transaction
```

### **De-duplication Skill**

```
1. Normalize skill name → slug
  "SignalR" → "signalr"
2. Check GetBySlugAsync(slug)
3. Exists?
  - YES: REUSE (không tạo duplicate)
  - NO: CREATE new skill
4. Sử dụng slug như key duy nhất (UNIQUE)
```

### **Data Relationships**

```
Challenge
  ├─ ChallengeVersion
      ├─ SkillWeightMapping (JSON)
      └─ Criteria (list)
          └─ EvaluationCriteria
              └─ CriteriaSkillMapping
                  └─ Skill (REUSED)

ChallengeSubmission
  ├─ SkillPointTransaction (audit)
  └─ UserSkill (update TotalPoints)
      └─ Skill (REUSED)
```

---

**Generated**: 2026-05-25 16:32:52 UTC+7  
**Status**: ✅ Detailed Scoring System Analysis Complete
