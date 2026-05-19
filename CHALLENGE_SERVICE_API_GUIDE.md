# Challenge Service API Guide

This guide explains the Challenge Service APIs, the main flow by role, and the request/response shapes frontend should use.

## Base setup

- **Base URL**: `https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
- **Auth**: most endpoints require a JWT in `Authorization: Bearer <token>`
- **ID types**:
  - challenge IDs: `Guid`
  - user IDs: `int`

## Main flows

### Creator flow

1. Create a challenge as `Draft`
2. View own challenges
3. Submit a challenge for review
4. Approve and publish the challenge
5. View submissions for the challenge

### Participant flow

1. Browse published challenges
2. Open challenge detail
3. Submit a solution
4. View own submissions

### Admin flow

1. Review pending challenges
2. Approve or reject a challenge

## API reference

### 1) Create challenge

`POST /api/challenges`

**Auth**: required

**Request**
```json
{
  "title": "Build Authentication System",
  "description": "Create a secure authentication system with JWT tokens and password hashing",
  "expectedSolution": "public class AuthService { }",
  "deadline": "2026-05-26T23:59:59.000Z"
}
```

**Response** `201 Created`
```json
{
  "id": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
  "title": "Build Authentication System",
  "description": "Create a secure authentication system with JWT tokens and password hashing",
  "status": "Draft",
  "createdAt": "2026-05-19T05:59:09.0513629",
  "createdById": 12,
  "reviewedById": null,
  "deadline": "2026-05-26T05:59:05.4473943",
  "publishedAt": null
}
```

### 2) Get challenge by id

`GET /api/challenges/{id}`

**Auth**: required for private challenges, optional for published ones

**Response**
```json
{
  "id": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
  "title": "Build Authentication System",
  "description": "Create a secure authentication system with JWT tokens and password hashing",
  "status": "Draft",
  "createdAt": "2026-05-19T05:59:09.0513629",
  "createdById": 12,
  "reviewedById": null,
  "deadline": "2026-05-26T05:59:05.4473943",
  "publishedAt": null
}
```

### 3) List challenges

`GET /api/challenges?skip=0&take=20&status=Published&userId=12`

**Auth**: anonymous allowed

**Response**
```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

### 4) Submit challenge for review

`POST /api/challenges/{id}/submit-review`

**Auth**: required, creator only

**Response**: same shape as `ChallengeDto`

### 5) Approve challenge

`POST /api/challenges/{id}/approve`

**Auth**: required, admin only

**Response**: same shape as `ChallengeDto`

### 6) Reject challenge

`POST /api/challenges/{id}/reject`

**Auth**: required, admin only

**Request**
```json
{
  "reason": "Expected solution is incomplete"
}
```

**Response**: same shape as `ChallengeDto`

### 7) Creator self-approve and publish

`POST /api/creator/challenges/{challengeId}/approve-and-publish`

**Auth**: required, creator only

**Response**
```json
{
  "id": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
  "title": "Build Authentication System",
  "description": "Create a secure authentication system with JWT tokens and password hashing",
  "status": "Published",
  "currentVersionId": "....",
  "createdAt": "2026-05-19T05:59:09.0513629",
  "updatedAt": "2026-05-19T06:21:29.1735659",
  "deadline": "2026-05-26T05:59:05.4473943"
}
```

### 8) Creator list own challenges

`GET /api/creator/challenges?skip=0&take=20`

**Auth**: required

**Response**
```json
{
  "items": [
    {
      "id": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
      "title": "Build Authentication System",
      "description": "Create a secure authentication system with JWT tokens and password hashing",
      "status": "Draft",
      "currentVersionId": null,
      "createdAt": "2026-05-19T05:59:09.0513629",
      "updatedAt": "2026-05-19T05:59:09.0513629",
      "deadline": "2026-05-26T05:59:05.4473943"
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 20
}
```

### 9) Creator submissions with user profile

`GET /api/creator/challenges/{challengeId}/submissions?skip=0&take=20`

**Auth**: required, creator only

**Response**
```json
{
  "items": [
    {
      "id": "cfc62751-81f5-4f45-a50e-1e540d12ea1d",
      "challengeId": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
      "userId": 2,
      "userName": "Quyền Trịnh",
      "userEmail": "conbothi3@gmail.com",
      "userAvatar": "https://...",
      "submissionStatus": "Graded",
      "submittedAt": "2026-05-19T06:13:45.642429",
      "submissionContent": "public class AuthService { }",
      "gitHubLink": "https://github.com/...",
      "evaluationScore": 4,
      "evaluationStatus": "Completed",
      "evaluatedAt": "2026-05-19T06:13:51.8249647",
      "feedback": "....",
      "attemptCount": 1
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 20
}
```

### 10) Participant submit solution

`POST /api/submissions`

**Auth**: required, participant only

**Request**
```json
{
  "challengeId": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
  "code": "public class AuthService { }",
  "githubUrl": "https://github.com/..."
}
```

**Response**
```json
{
  "id": "cfc62751-81f5-4f45-a50e-1e540d12ea1d",
  "challengeId": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
  "userId": 2,
  "status": "Graded",
  "overallScore": 4,
  "aiFeedback": "....",
  "createdAt": "2026-05-19T06:13:45.642429",
  "gradedAt": "2026-05-19T06:13:51.8249647"
}
```

### 11) Participant list own submissions

`GET /api/submissions?skip=0&take=20`

**Auth**: required

**Response**
```json
{
  "items": [
    {
      "id": "cfc62751-81f5-4f45-a50e-1e540d12ea1d",
      "challengeId": "0b7cc3dc-9e2f-4699-a56b-7ae8179d9c45",
      "userId": 2,
      "status": "Graded",
      "overallScore": 4,
      "aiFeedback": "....",
      "createdAt": "2026-05-19T06:13:45.642429",
      "gradedAt": "2026-05-19T06:13:51.8249647"
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 20
}
```

### 12) Participant submissions for a challenge

`GET /api/challenges/{challengeId}/my-submissions?skip=0&take=20`

**Auth**: required

**Response**
```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

## Frontend notes

- Use `createdById` to show ownership and filter creator-only views.
- Use `userName`, `userEmail`, `userAvatar` from creator submission responses to render reviewer tables.
- Treat `Draft`, `PendingReview`, `Published`, `Rejected`, `Expired` as the main challenge statuses.
- `api/challenges` is the general discovery endpoint; creator-specific actions live under `api/creator/challenges`.
- Creator submission list is the only place where the backend enriches submissions with user profile data.

## Frontend samples

### Shared helper

```ts
const API_BASE = "https://challenge-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io";

function authHeaders(token: string) {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  };
}
```

### Create challenge

```ts
async function createChallenge(token: string) {
  const res = await fetch(`${API_BASE}/api/challenges`, {
    method: "POST",
    headers: authHeaders(token),
    body: JSON.stringify({
      title: "Build Authentication System",
      description: "Create a secure authentication system with JWT tokens and password hashing",
      expectedSolution: "public class AuthService { }",
      deadline: new Date("2026-05-26T23:59:59Z").toISOString(),
    }),
  });

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

### Load published challenges

```ts
async function loadPublishedChallenges() {
  const res = await fetch(`${API_BASE}/api/challenges?skip=0&take=20`);
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

### Load creator submissions

```ts
async function loadCreatorSubmissions(token: string, challengeId: string) {
  const res = await fetch(
    `${API_BASE}/api/creator/challenges/${challengeId}/submissions?skip=0&take=20`,
    { headers: { Authorization: `Bearer ${token}` } }
  );

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

### Submit solution

```ts
async function submitSolution(token: string, challengeId: string) {
  const res = await fetch(`${API_BASE}/api/submissions`, {
    method: "POST",
    headers: authHeaders(token),
    body: JSON.stringify({
      challengeId,
      code: "public class AuthService { }",
      githubUrl: "https://github.com/your-repo",
    }),
  });

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

### Approve and publish as creator

```ts
async function approveAndPublish(token: string, challengeId: string) {
  const res = await fetch(
    `${API_BASE}/api/creator/challenges/${challengeId}/approve-and-publish`,
    {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    }
  );

  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

