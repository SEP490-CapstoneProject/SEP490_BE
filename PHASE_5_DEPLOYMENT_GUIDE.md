# Phase 5: Firebase Configuration & Production Deployment

## Overview

Phase 5 is the final step before go-live. This phase includes:
1. Firebase project setup (console configuration)
2. Service account credentials generation
3. Azure Key Vault integration
4. Database migrations (staging → production)
5. Gradual production rollout

---

## Part 1: Firebase Console Setup

### Prerequisites
- Google Cloud Account (or create new)
- Billing enabled on GCP
- Access to Firebase Console
- Project owner permissions

### Step 1: Create Firebase Project

**Navigate to Firebase Console:**
1. Go to https://console.firebase.google.com
2. Click **"Add project"**

**Project Configuration:**
```
Project Name: recruitment-platform-prod
Location: Southeast Asia (Vietnam)
Analytics: Enable
```

3. Click **"Create project"** and wait (2-3 minutes)

### Step 2: Register Android App

**In Firebase Console:**
1. Click **Android** icon (or ⊕ Add app)
2. Enter package name: `com.recruitment.app`
3. Enter app nickname: `Recruitment Mobile`
4. Leave signing certificate SHA-1 empty (optional for testing)
5. Click **"Register app"**

**Download Configuration:**
- Click **"Download google-services.json"**
- **SAVE FILE** - Send to mobile team
- File location: `android/app/google-services.json`

**Note:** Do NOT commit to git (it contains API keys)

### Step 3: Register iOS App

**In Firebase Console:**
1. Click **iOS** icon (or ⊕ Add app)
2. Enter bundle ID: `com.recruitment.app`
3. Enter app nickname: `Recruitment Mobile iOS`
4. Leave Team ID empty (optional)
5. Click **"Register app"**

**Download Configuration:**
- Click **"Download GoogleService-Info.plist"**
- **SAVE FILE** - Send to mobile team
- File location: `ios/Runner/GoogleService-Info.plist`

**Note:** Do NOT commit to git (it contains API keys)

### Step 4: Enable Cloud Messaging API

**In Firebase Console:**
1. Go to **"Project Settings"** (gear icon)
2. Click **"APIs and services"** → **"Cloud APIs"**
3. Search for **"Firebase Cloud Messaging API"**
4. Click **"Enable"**

**Verify Status:**
- Status shows: ✅ Enabled

---

## Part 2: Service Account Setup

### Step 1: Create Service Account

**In Firebase Console:**
1. Go to **"Project Settings"** → **"Service Accounts"**
2. Click **"Generate New Private Key"**
3. Select **"Python"** or **"Node.js"** (doesn't matter)
4. Click **"Generate Key"**

**File Downloaded:** `recruitment-platform-prod-xxxxx.json`

**Contents Example:**
```json
{
  "type": "service_account",
  "project_id": "recruitment-platform-prod",
  "private_key_id": "xxxxx",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk-xxxxx@recruitment-platform-prod.iam.gserviceaccount.com",
  "client_id": "123456789",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

### Step 2: Verify Service Account Permissions

**In Google Cloud Console:**
1. Go to https://console.cloud.google.com
2. Select project: `recruitment-platform-prod`
3. Go to **"IAM & Admin"** → **"Service Accounts"**
4. Find service account ending in `.iam.gserviceaccount.com`
5. Verify roles:
   - ✅ Firebase Admin
   - ✅ Cloud Messaging Editor

**If roles missing:**
1. Click the service account
2. Click **"Edit"**
3. Add roles (Firebase Admin, Cloud Messaging Editor)
4. Click **"Save"**

---

## Part 3: Azure Key Vault Integration

### Step 1: Store Service Account in Key Vault

**Via Azure Portal:**
1. Open Azure Portal → Key Vault → your vault
2. Click **"Secrets"** → **"Generate/Import"**
3. Create secret:
   ```
   Name: FirebaseServiceAccountKey
   Value: [Paste entire JSON content]
   Content Type: application/json
   ```
4. Click **"Create"**

**Record Secret Identifier:**
```
https://your-keyvault.vault.azure.net/secrets/FirebaseServiceAccountKey/xxxxx
```

### Step 2: Verify Key Vault Access

**Test Secret Retrieval:**
```powershell
$secret = Get-AzKeyVaultSecret -VaultName "your-vault" -Name "FirebaseServiceAccountKey"
$secretValue = $secret.SecretValue | ConvertFrom-SecureString -AsPlainText
$secretValue | ConvertFrom-Json | Select-Object project_id
```

**Expected Output:**
```
project_id: recruitment-platform-prod
```

---

## Part 4: Application Configuration

### Step 1: Update appsettings.json (Staging)

**File:** `src/Services/Notification/Notification.API/appsettings.Staging.json`

```json
{
  "Firebase": {
    "Enabled": true,
    "ProjectId": "recruitment-platform-prod",
    "CredentialPath": "firebase-staging-credentials.json"
  },
  "FCM": {
    "AndroidPriority": "high",
    "AndroidTTL": "3600s",
    "IOSPriority": "10",
    "BatchSize": 100,
    "MaxRetries": 3,
    "RetryDelayMs": 1000
  }
}
```

### Step 2: Update appsettings.json (Production)

**File:** `src/Services/Notification/Notification.API/appsettings.Production.json`

```json
{
  "Firebase": {
    "Enabled": true,
    "ProjectId": "recruitment-platform-prod",
    "CredentialPath": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/FirebaseServiceAccountKey/)"
  },
  "FCM": {
    "AndroidPriority": "high",
    "AndroidTTL": "3600s",
    "IOSPriority": "10",
    "BatchSize": 500,
    "MaxRetries": 3,
    "RetryDelayMs": 2000
  }
}
```

### Step 3: Register Firebase Service in DI

**File:** `Notification.API/Program.cs`

```csharp
// Add before app.Run()
if (configuration.GetValue<bool>("Firebase:Enabled"))
{
    var firebaseProjectId = configuration["Firebase:ProjectId"];
    
    // Initialize Firebase Admin SDK
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(configuration["Firebase:CredentialPath"])
            .CreateScoped(new[] { "https://www.googleapis.com/auth/cloud-platform" }),
        ProjectId = firebaseProjectId
    });
    
    Console.WriteLine($"✅ Firebase initialized for project: {firebaseProjectId}");
}
```

---

## Part 5: Database Migrations

### Step 1: Verify Migration File Exists

**Expected File:** `Notification.Infrastructure/Migrations/20260504000000_AddFcmTables.cs`

**Verify Contents:**
```csharp
public partial class AddFcmTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Creates DEVICE_TOKENS table
        // Creates PUSH_NOTIFICATION_LOG table
        // Creates NOTIFICATION_SETTINGS table
        // Creates indexes for performance
    }
}
```

### Step 2: Run Migration on Staging

**Via PowerShell:**

```powershell
# Navigate to Notification.Infrastructure project
cd "D:\Capstone\src\Services\Notification\Notification.Infrastructure"

# Apply migrations to staging database
$env:ASPNETCORE_ENVIRONMENT = "Staging"
dotnet ef database update --context NotificationDbContext `
  --connection "Server=staging-server;Database=RecruitmentDB;User Id=sa;Password=YourPassword;Encrypt=true"

# Verify tables created
Invoke-SqlCmd -ServerInstance "staging-server" `
  -Database "RecruitmentDB" `
  -Query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('DEVICE_TOKENS', 'PUSH_NOTIFICATION_LOG', 'NOTIFICATION_SETTINGS')"
```

**Expected Output:**
```
TABLE_NAME
-----------
DEVICE_TOKENS
PUSH_NOTIFICATION_LOG
NOTIFICATION_SETTINGS
```

### Step 3: Backup Production Database

**Via SQL Server Management Studio:**

```sql
BACKUP DATABASE [RecruitmentDB]
TO DISK = 'D:\Backups\RecruitmentDB_20260504_PreFCM.bak'
WITH COMPRESSION, 
     DESCRIPTION = 'Pre-FCM migration backup'
```

**Or via PowerShell:**

```powershell
Backup-SqlDatabase -ServerInstance "production-server" `
  -Database "RecruitmentDB" `
  -BackupFile "D:\Backups\RecruitmentDB_20260504_PreFCM.bak" `
  -CompressionOption On
```

**Save Backup Location:**
```
D:\Backups\RecruitmentDB_20260504_PreFCM.bak
```

### Step 4: Run Migration on Production

**Via PowerShell:**

```powershell
# Navigate to Notification.Infrastructure project
cd "D:\Capstone\src\Services\Notification\Notification.Infrastructure"

# Apply migrations to production database
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet ef database update --context NotificationDbContext `
  --connection "Server=production-server;Database=RecruitmentDB;User Id=sa;Password=YourPassword;Encrypt=true"

# Verify tables created
Invoke-SqlCmd -ServerInstance "production-server" `
  -Database "RecruitmentDB" `
  -Query "
SELECT 
  t.TABLE_NAME,
  COUNT(*) AS ColumnCount,
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = t.TABLE_NAME) AS KeyCount
FROM INFORMATION_SCHEMA.TABLES t
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_NAME IN ('DEVICE_TOKENS', 'PUSH_NOTIFICATION_LOG', 'NOTIFICATION_SETTINGS')
GROUP BY t.TABLE_NAME
"
```

**Expected Output:**
```
TABLE_NAME                | ColumnCount | KeyCount
--------------------------|-------------|----------
DEVICE_TOKENS             | 9           | 2
PUSH_NOTIFICATION_LOG      | 8           | 2
NOTIFICATION_SETTINGS      | 6           | 1
```

---

## Part 6: Staging Deployment & Testing

### Step 1: Build Docker Image for Staging

```powershell
# Build image
docker build -t notification-service:staging `
  -f src/Services/Notification/Dockerfile `
  .

# Tag for staging registry
docker tag notification-service:staging `
  your-registry.azurecr.io/notification-service:staging

# Push to ACR
docker push your-registry.azurecr.io/notification-service:staging
```

### Step 2: Deploy to Staging

```powershell
# Use kubectl or Azure Container Instances
kubectl set image deployment/notification-service `
  notification-service=your-registry.azurecr.io/notification-service:staging `
  -n staging

# Wait for rollout
kubectl rollout status deployment/notification-service -n staging
```

### Step 3: Verify Firebase Connection

**Test Endpoint:**
```powershell
$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type" = "application/json"
}

# Register test device token
$response = Invoke-RestMethod `
  -Uri "https://staging-api.recruitment.local/api/device-tokens/register" `
  -Method POST `
  -Headers $headers `
  -Body (@{
    token = "test_fcm_token_staging_123"
    deviceType = "Android"
    appVersion = "1.0.0"
  } | ConvertTo-Json)

# Expected response
$response | ConvertTo-Json -Depth 10
```

### Step 4: Send Test Push Notification

**Via Firebase Console:**
1. Go to Firebase Console → "Messaging"
2. Click **"Send your first message"**
3. Create test message:
   ```
   Title: Test Notification
   Body: FCM is working on staging!
   Target: Single device
   FCM Token: test_fcm_token_staging_123
   ```
4. Click **"Send"**

**Verification:**
- Check notification tray on test device
- Or review logs: `kubectl logs -n staging -l app=notification-service`

---

## Part 7: Production Deployment

### Step 1: Build Production Image

```powershell
# Build image with production tag
docker build -t notification-service:prod-v1.0 `
  --build-arg ENVIRONMENT=Production `
  -f src/Services/Notification/Dockerfile `
  .

# Tag for production registry
docker tag notification-service:prod-v1.0 `
  your-registry.azurecr.io/notification-service:prod-v1.0

# Push to ACR
docker push your-registry.azurecr.io/notification-service:prod-v1.0
```

### Step 2: Deploy to Production (Canary: 5%)

```powershell
# Deploy with 5% traffic initially
kubectl set image deployment/notification-service `
  notification-service=your-registry.azurecr.io/notification-service:prod-v1.0 `
  -n production

# Set replica count for canary (5% of 20 pods = 1 pod)
kubectl scale deployment notification-service --replicas=1 -n production

# Monitor for 30 minutes
for ($i=0; $i -lt 30; $i++) {
    kubectl top pods -n production -l app=notification-service
    Start-Sleep -Seconds 60
}
```

### Step 3: Monitor Metrics During Canary

**Check Key Metrics:**
```powershell
# Query Prometheus/Application Insights
$startTime = (Get-Date).AddMinutes(-30)
$endTime = Get-Date

# Error rate
$errorRate = Get-ApplicationInsightsMetric `
  -Name "ExceptionCount" `
  -StartTime $startTime `
  -EndTime $endTime

# FCM delivery success rate
$fcmSuccess = Get-ApplicationInsightsMetric `
  -Name "FCM.DeliverySuccess" `
  -StartTime $startTime `
  -EndTime $endTime

Write-Host "Error Rate: $errorRate"
Write-Host "FCM Success Rate: $fcmSuccess"
```

### Step 4: Scale to 100% if Canary Successful

```powershell
# If no issues after 30 min, scale to full
kubectl scale deployment notification-service --replicas=20 -n production

# Verify all pods running
kubectl get pods -n production -l app=notification-service

# Check rollout status
kubectl rollout status deployment/notification-service -n production
```

---

## Part 8: Gradual User Rollout

### Step 1: Android 15% Rollout

**Feature Flag Configuration:**
```json
{
  "FCM": {
    "Enabled": true,
    "AndroidUserPercentage": 15,
    "IOSUserPercentage": 0
  }
}
```

**Deploy Configuration Update:**
```powershell
kubectl set env deployment/notification-service `
  FCM_ANDROID_PERCENTAGE=15 `
  FCM_IOS_PERCENTAGE=0 `
  -n production
```

**Monitor for 24 hours:**
- Success rate > 99%
- Error rate < 0.5%
- No spikes in exceptions

### Step 2: Android 100% Rollout (if successful)

```powershell
kubectl set env deployment/notification-service `
  FCM_ANDROID_PERCENTAGE=100 `
  FCM_IOS_PERCENTAGE=0 `
  -n production
```

**Monitor for 48 hours:**
- Verify token registration working
- Verify push delivery working
- Check for any token validation issues

### Step 3: iOS 5% Rollout

```powershell
kubectl set env deployment/notification-service `
  FCM_ANDROID_PERCENTAGE=100 `
  FCM_IOS_PERCENTAGE=5 `
  -n production
```

**Monitor for 24 hours:**
- iOS-specific metrics
- APNS certificate verification
- Deep linking on iOS

### Step 4: iOS 100% Rollout (if successful)

```powershell
kubectl set env deployment/notification-service `
  FCM_ANDROID_PERCENTAGE=100 `
  FCM_IOS_PERCENTAGE=100 `
  -n production
```

**Final verification:**
- All metrics green
- User engagement up
- Support tickets low

---

## Rollback Plan

If issues occur at any stage:

### Option 1: Disable FCM (Keep Realtime)
```powershell
kubectl set env deployment/notification-service `
  FCM_ENABLED=false `
  -n production
```

**Result:** 
- Users online still get SignalR notifications
- Users offline don't get notifications (same as before FCM)
- Full rollback < 2 minutes

### Option 2: Restore from Backup (if DB corrupted)

```powershell
# Restore production database from backup
Restore-SqlDatabase -ServerInstance "production-server" `
  -DatabaseName "RecruitmentDB" `
  -BackupFile "D:\Backups\RecruitmentDB_20260504_PreFCM.bak" `
  -ReplaceDatabase
```

---

## Success Criteria - Production

✅ **Must Have:**
- Firebase credentials configured and working
- Database tables created and accessible
- Device tokens registering successfully
- Push notifications delivered successfully
- Error rate < 0.5%
- Success rate > 99.5%

✅ **Performance Targets:**
- Token registration < 500ms
- Push delivery latency < 5 seconds (p95)
- FCM API calls < 100ms (p95)

✅ **Monitoring:**
- Alerts configured for error rate
- Alerts configured for delivery failure
- Alerts configured for API quota
- Logs aggregated in Application Insights

---

## Post-Deployment Tasks

### Daily Monitoring (First Week)
- Review error logs
- Monitor success rate
- Check user feedback
- Verify token cleanup working

### Weekly Review (Weeks 2-4)
- Analyze delivery metrics
- Optimize configurations
- Plan next phase
- Document lessons learned

### Monthly Review
- Review analytics
- Plan optimizations
- Adjust rollout schedule
- Update documentation

---

## Checklist

### Pre-Deployment
- [ ] Firebase project created
- [ ] Service account credentials generated
- [ ] Credentials stored in Key Vault
- [ ] appsettings.json updated
- [ ] Migrations tested on staging
- [ ] Production database backed up
- [ ] Migrations run on production
- [ ] Staging deployment successful
- [ ] Staging tests passing

### Deployment
- [ ] Production image built
- [ ] 5% canary deployment successful
- [ ] Metrics monitored for 30 minutes
- [ ] Error rate acceptable
- [ ] 100% production deployment
- [ ] All pods running

### Post-Deployment
- [ ] Android 15% rollout successful
- [ ] Android 15% monitored 24 hours
- [ ] Android 100% rollout
- [ ] iOS 5% rollout successful
- [ ] iOS 100% rollout
- [ ] Documentation updated
- [ ] Team notified

---

## Emergency Contacts

**Firebase Support:** https://firebase.google.com/support
**Google Cloud Support:** https://cloud.google.com/support
**Azure Support:** https://azure.microsoft.com/en-us/support/

**Internal Escalation:**
- Infra Team Lead: [contact]
- Backend Lead: [contact]
- Mobile Lead: [contact]

---

**Document Version:** 1.0
**Status:** Ready for Phase 5 Deployment
**Last Updated:** 2026-05-04
