# Quizzy API Endpoints

This document describes all API endpoints required for the Quizzy application. All endpoints return JSON responses.

## Authentication

All endpoints require authentication via JWT Bearer tokens (ASP.NET Core Identity).

```
Authorization: Bearer <token>
```

---

## Table of Contents

1. [Categories](#categories)
2. [Quizzes](#quizzes)
3. [Quiz Attempts](#quiz-attempts)
4. [User Profiles](#user-profiles)
5. [Admin](#admin)

---

## Categories

### List All Categories

```http
GET /api/categories
```

**Response:**
```json
{
  "categories": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Science",
      "description": "Science quizzes",
      "quizCount": 15,
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ]
}
```

---

### Get Category by ID

```http
GET /api/categories/{id}
```

**Response:**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Science",
  "description": "Science quizzes",
  "quizCount": 15,
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-02-20T14:00:00Z"
}
```

---

### Create Category (Teacher/Admin)

```http
POST /api/categories
Content-Type: application/json
```

**Request:**
```json
{
  "name": "History",
  "description": "Historical events and figures"
}
```

**Response:** `201 Created`
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "History",
  "description": "Historical events and figures",
  "createdAt": "2025-03-01T09:00:00Z"
}
```

---

### Update Category (Teacher/Admin)

```http
PUT /api/categories/{id}
Content-Type: application/json
```

**Request:**
```json
{
  "name": "World History",
  "description": "Updated description"
}
```

**Response:** `200 OK`

---

### Delete Category (Admin)

```http
DELETE /api/categories/{id}
```

**Response:** `204 No Content`

> **Note:** Soft delete. Sets `IsDeleted = true` and `DeletedAt`.

---

## Quizzes

### List Published Quizzes (Students)

```http
GET /api/quizzes?categoryId={id}&search={term}&page={page}&pageSize={size}
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `categoryId` | uuid | Filter by category (optional) |
| `search` | string | Search in title/description (optional) |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 10, max: 50) |

**Response:**
```json
{
  "quizzes": [
    {
      "id": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
      "title": "Biology 101",
      "description": "Introduction to biology",
      "category": {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "name": "Science"
      },
      "teacher": {
        "id": "t1t1t1t1-t1t1-t1t1-t1t1-t1t1t1t1t1t1",
        "name": "Dr. Smith"
      },
      "timeLimitMinutes": 30,
      "passingScore": 70,
      "maxAttempts": 3,
      "hasAccessCode": false,
      "questionCount": 20,
      "createdAt": "2025-02-01T08:00:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

> **Note:** `hasAccessCode` indicates if the quiz requires an access code. Does NOT expose the actual code.

---

### List Teacher's Quizzes (Teachers)

```http
GET /api/quizzes/my
```

**Response:**
```json
{
  "quizzes": [
    {
      "id": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
      "title": "Biology 101",
      "isPublished": true,
      "hasAccessCode": true,
      "attemptCount": 25,
      "averageScore": 82.5,
      "createdAt": "2025-02-01T08:00:00Z",
      "updatedAt": "2025-02-15T10:00:00Z"
    }
  ]
}
```

---

### Get Quiz by ID (Students)

```http
GET /api/quizzes/{id}
```

**Response:**
```json
{
  "id": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "title": "Biology 101",
  "description": "Introduction to biology",
  "category": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "Science"
  },
  "teacher": {
    "id": "t1t1t1t1-t1t1-t1t1-t1t1-t1t1t1t1t1t1",
    "name": "Dr. Smith"
  },
  "timeLimitMinutes": 30,
  "passingScore": 70,
  "maxAttempts": 3,
  "hasAccessCode": true,
  "questionCount": 20,
  "totalPoints": 100,
  "userAttemptsCount": 1,
  "userBestScore": 85,
  "createdAt": "2025-02-01T08:00:00Z"
}
```

> **Note:** Does not include questions/choices. Only quiz metadata.

---

### Get Quiz with Questions (Students - After Access Granted)

```http
GET /api/quizzes/{id}/questions
```

**Response:**
```json
{
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "title": "Biology 101",
  "timeLimitMinutes": 30,
  "totalPoints": 100,
  "questions": [
    {
      "id": "ques1-ques1-ques1-ques1-ques1ques1",
      "content": "What is the powerhouse of the cell?",
      "orderIndex": 0,
      "points": 5,
      "choices": [
        {
          "id": "choi1-choi1-choi1-choi1-choi1choi1",
          "content": "Mitochondria",
          "orderIndex": 0
        },
        {
          "id": "choi2-choi2-choi2-choi2-choi2choi2",
          "content": "Nucleus",
          "orderIndex": 1
        }
      ]
    }
  ]
}
```

> **Note:** `IsCorrect` is NOT included in choices. Returns questions only after access is validated.

---

### Get Quiz Details (Teachers - Full Edit View)

```http
GET /api/quizzes/{id}/full
```

**Response:**
```json
{
  "id": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "title": "Biology 101",
  "description": "Introduction to biology",
  "categoryId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timeLimitMinutes": 30,
  "passingScore": 70,
  "maxAttempts": 3,
  "isPublished": true,
  "hasAccessCode": true,
  "accessCode": "BIO2025",
  "questions": [
    {
      "id": "ques1-ques1-ques1-ques1-ques1ques1",
      "content": "What is the powerhouse of the cell?",
      "orderIndex": 0,
      "points": 5,
      "choices": [
        {
          "id": "choi1-choi1-choi1-choi1-choi1choi1",
          "content": "Mitochondria",
          "orderIndex": 0,
          "isCorrect": true
        },
        {
          "id": "choi2-choi2-choi2-choi2-choi2choi2",
          "content": "Nucleus",
          "orderIndex": 1,
          "isCorrect": false
        }
      ]
    }
  ]
}
```

> **Note:** Teachers see `accessCode` and all questions with `isCorrect` flags.

---

### Create Quiz (Teachers)

```http
POST /api/quizzes
Content-Type: application/json
```

**Request:**
```json
{
  "title": "Biology 101",
  "description": "Introduction to biology",
  "categoryId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timeLimitMinutes": 30,
  "passingScore": 70,
  "maxAttempts": 3,
  "isPublished": false,
  "accessCode": null,
  "questions": [
    {
      "content": "What is the powerhouse of the cell?",
      "points": 5,
      "choices": [
        { "content": "Mitochondria", "isCorrect": true },
        { "content": "Nucleus", "isCorrect": false }
      ]
    }
  ]
}
```

**Response:** `201 Created`
```json
{
  "id": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "title": "Biology 101",
  "createdAt": "2025-03-01T09:00:00Z"
}
```

---

### Update Quiz (Teachers)

```http
PUT /api/quizzes/{id}
Content-Type: application/json
```

**Request:** Same structure as Create Quiz

**Response:** `200 OK`

---

### Publish/Unpublish Quiz (Teachers)

```http
PATCH /api/quizzes/{id}/publish
Content-Type: application/json
```

**Request:**
```json
{
  "isPublished": true
}
```

**Response:** `200 OK`

---

### Generate/Update Access Code (Teachers)

```http
POST /api/quizzes/{id}/access-code
Content-Type: application/json
```

**Request:**
```json
{
  "accessCode": "BIO2025"
}
```

**Response:** `200 OK`
```json
{
  "accessCode": "BIO2025",
  "updatedAt": "2025-03-01T10:00:00Z"
}
```

---

### Remove Access Code (Teachers)

```http
DELETE /api/quizzes/{id}/access-code
```

**Response:** `204 No Content`

---

### Validate Access Code (Students)

```http
POST /api/quizzes/{id}/validate-access-code
Content-Type: application/json
```

**Request:**
```json
{
  "accessCode": "BIO2025"
}
```

**Response (Valid):** `200 OK`
```json
{
  "valid": true,
  "message": "Access granted"
}
```

**Response (Invalid):** `403 Forbidden`
```json
{
  "valid": false,
  "message": "Invalid access code"
}
```

---

### Delete Quiz (Teachers)

```http
DELETE /api/quizzes/{id}
```

**Response:** `204 No Content`

> **Note:** Soft delete. Sets `IsDeleted = true` and `DeletedAt`.

---

## Quiz Attempts

### List User's Attempts for a Quiz (Students)

```http
GET /api/quizzes/{quizId}/attempts
```

**Response:**
```json
{
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "quizTitle": "Biology 101",
  "maxAttempts": 3,
  "attempts": [
    {
      "id": "att1-att1-att1-att1-att1att1att1",
      "attemptNumber": 1,
      "status": "Completed",
      "score": 85,
      "totalPossibleScore": 100,
      "percentage": 85,
      "passed": true,
      "startedAt": "2025-02-10T14:00:00Z",
      "completedAt": "2025-02-10T14:25:00Z",
      "timeSpentSeconds": 1500
    }
  ],
  "canAttemptAgain": true,
  "remainingAttempts": 2
}
```

---

### Get In-Progress Attempt (Students)

```http
GET /api/attempts/{id}
```

**Response:**
```json
{
  "id": "att1-att1-att1-att1-att1att1att1",
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "quizTitle": "Biology 101",
  "attemptNumber": 2,
  "status": "InProgress",
  "timeLimitMinutes": 30,
  "startedAt": "2025-02-15T10:00:00Z",
  "timeRemainingSeconds": 1200,
  "questions": [
    {
      "id": "ques1-ques1-ques1-ques1-ques1ques1",
      "content": "What is the powerhouse of the cell?",
      "orderIndex": 0,
      "points": 5,
      "answerId": null,
      "choices": [
        {
          "id": "choi1-choi1-choi1-choi1-choi1choi1",
          "content": "Mitochondria",
          "orderIndex": 0
        }
      ]
    }
  ]
}
```

> **Note:** Returns questions with user's saved answers (if any). `answerId` is the user's selected choice.

---

### Create Attempt (Students)

```http
POST /api/quizzes/{id}/attempts
```

**Response:** `201 Created`
```json
{
  "attemptId": "att1-att1-att1-att1-att1att1att1",
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "status": "InProgress",
  "startedAt": "2025-03-01T11:00:00Z"
}
```

> **Note:** Validates access code server-side if quiz requires one. Returns `403 Forbidden` if access denied.

---

### Submit Answer (Students)

```http
POST /api/attempts/{attemptId}/answers
Content-Type: application/json
```

**Request:**
```json
{
  "questionId": "ques1-ques1-ques1-ques1-ques1ques1",
  "choiceId": "choi1-choi1-choi1-choi1-choi1choi1"
}
```

**Response:** `200 OK`
```json
{
  "answerId": "ans1-ans1-ans1-ans1-ans1ans1ans1",
  "saved": true
}
```

---

### Submit Attempt (Students)

```http
POST /api/attempts/{attemptId}/submit
```

**Response:** `200 OK`
```json
{
  "attemptId": "att1-att1-att1-att1-att1att1att1",
  "status": "Completed",
  "score": 85,
  "totalPossibleScore": 100,
  "percentage": 85,
  "passed": true,
  "completedAt": "2025-03-01T11:25:00Z",
  "timeSpentSeconds": 1500,
  "answers": [
    {
      "questionId": "ques1-ques1-ques1-ques1-ques1ques1",
      "questionContent": "What is the powerhouse of the cell?",
      "selectedChoice": "Mitochondria",
      "isCorrect": true,
      "points": 5
    }
  ]
}
```

---

### Abandon Attempt (Students)

```http
POST /api/attempts/{attemptId}/abandon
```

**Response:** `200 OK`
```json
{
  "attemptId": "att1-att1-att1-att1-att1att1att1",
  "status": "Abandoned",
  "abandonedAt": "2025-03-01T11:15:00Z"
}
```

---

### Get Attempt Results (Students)

```http
GET /api/attempts/{id}/results
```

**Response:**
```json
{
  "attemptId": "att1-att1-att1-att1-att1att1att1",
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "quizTitle": "Biology 101",
  "attemptNumber": 1,
  "status": "Completed",
  "score": 85,
  "totalPossibleScore": 100,
  "percentage": 85,
  "passed": true,
  "passingScore": 70,
  "startedAt": "2025-02-10T14:00:00Z",
  "completedAt": "2025-02-10T14:25:00Z",
  "timeSpentSeconds": 1500,
  "timeLimitMinutes": 30,
  "answers": [
    {
      "questionId": "ques1-ques1-ques1-ques1-ques1ques1",
      "questionContent": "What is the powerhouse of the cell?",
      "selectedChoice": "Mitochondria",
      "correctChoice": "Mitochondria",
      "isCorrect": true,
      "points": 5,
      "maxPoints": 5
    }
  ]
}
```

---

### Get Teacher's Quiz Attempts Overview (Teachers)

```http
GET /api/quizzes/{quizId}/attempts/overview
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string | Filter by status: `Completed`, `InProgress`, `Abandoned` (optional) |

**Response:**
```json
{
  "quizId": "q1q1q1q1-q1q1-q1q1-q1q1-q1q1q1q1q1q1",
  "quizTitle": "Biology 101",
  "totalAttempts": 25,
  "completedAttempts": 20,
  "inProgressAttempts": 3,
  "abandonedAttempts": 2,
  "averageScore": 82.5,
  "passRate": 75,
  "averageTimeSpentSeconds": 1400,
  "attempts": [
    {
      "id": "att1-att1-att1-att1-att1att1att1",
      "student": {
        "id": "s1s1s1s1-s1s1-s1s1-s1s1-s1s1s1s1s1s1",
        "name": "John Doe"
      },
      "attemptNumber": 1,
      "status": "Completed",
      "score": 85,
      "totalPossibleScore": 100,
      "percentage": 85,
      "passed": true,
      "startedAt": "2025-02-10T14:00:00Z",
      "completedAt": "2025-02-10T14:25:00Z",
      "timeSpentSeconds": 1500
    }
  ]
}
```

---

### Get Attempt Details (Teachers)

```http
GET /api/attempts/{id}/details
```

**Response:** Similar to student's results view, but with additional student info.

---

## User Profiles

### Get Current User Profile

```http
GET /api/profile
```

**Response:**
```json
{
  "userId": "u1u1u1u1-u1u1-u1u1-u1u1-u1u1u1u1u1u1",
  "userName": "johndoe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Student at University",
  "profileImageUrl": "https://example.com/avatar.jpg",
  "roles": ["Student"],
  "createdAt": "2025-01-01T00:00:00Z"
}
```

---

### Update User Profile

```http
PUT /api/profile
Content-Type: application/json
```

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Updated bio"
}
```

**Response:** `200 OK`

---

### Get User Statistics (Students)

```http
GET /api/profile/stats
```

**Response:**
```json
{
  "totalAttempts": 15,
  "completedAttempts": 12,
  "averageScore": 78.5,
  "bestScore": 95,
  "quizzesPassed": 10,
  "quizzesFailed": 2,
  "totalTimeSpentSeconds": 18000
}
```

---

## Admin

### List All Users

```http
GET /api/admin/users?role={role}&page={page}&pageSize={size}
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `role` | string | Filter by role: `Teacher`, `Student` (optional) |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 10) |

**Response:**
```json
{
  "users": [
    {
      "id": "u1u1u1u1-u1u1-u1u1-u1u1-u1u1u1u1u1u1",
      "userName": "johndoe",
      "email": "john@example.com",
      "roles": ["Student"],
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 10,
  "totalPages": 15
}
```

---

### Assign Role to User (Admin)

```http
POST /api/admin/users/{userId}/roles
Content-Type: application/json
```

**Request:**
```json
{
  "role": "Teacher"
}
```

**Response:** `200 OK`

---

### Remove Role from User (Admin)

```http
DELETE /api/admin/users/{userId}/roles/{role}
```

**Response:** `204 No Content`

---

### Get Dashboard Statistics (Admin)

```http
GET /api/admin/dashboard
```

**Response:**
```json
{
  "totalUsers": 150,
  "totalTeachers": 25,
  "totalStudents": 125,
  "totalQuizzes": 75,
  "totalAttempts": 500,
  "totalCategories": 10,
  "recentActivity": [
    {
      "type": "QuizAttempt",
      "userName": "John Doe",
      "quizTitle": "Biology 101",
      "score": 85,
      "timestamp": "2025-03-01T11:25:00Z"
    }
  ]
}
```

---

## Error Responses

### Standard Error Format

```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "errors": {
    "title": ["Title is required"],
    "passingScore": ["Passing score must be between 0 and 100"]
  }
}
```

### Common Status Codes

| Code | Meaning |
|------|---------|
| `200 OK` | Request successful |
| `201 Created` | Resource created successfully |
| `204 No Content` | Request successful, no content to return |
| `400 Bad Request` | Invalid request data |
| `401 Unauthorized` | Authentication required |
| `403 Forbidden` | Access denied (insufficient permissions or invalid access code) |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Conflict (e.g., quiz already has an active attempt) |
| `500 Internal Server Error` | Server error |

---

## Rate Limiting

| Endpoint Category | Limit |
|-------------------|-------|
| Authentication | 10 requests/minute |
| Quiz Attempts | 60 requests/minute |
| All other endpoints | 100 requests/minute |

Rate limit headers are included in responses:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1677676800
```

---

## Versioning

API version is specified in the URL path:
```
/api/v1/quizzes
```

Current version: `v1`
