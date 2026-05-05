# Firebase Configuration Helper Script

This script automates the Firebase configuration and deployment steps.

## Prerequisites

- PowerShell 7+
- Azure CLI authenticated (`az login`)
- kubectl configured for your cluster
- Docker installed and authenticated with ACR

## Usage

```powershell
# Run all steps interactively
.\firebase-deploy.ps1

# Run specific steps
.\firebase-deploy.ps1 -Step ConfigureAppSettings
.\firebase-deploy.ps1 -Step VerifyMigrations
.\firebase-deploy.ps1 -Step DeployStaging
.\firebase-deploy.ps1 -Step DeployProduction
```

## Features

- Validates Firebase credentials
- Verifies database migrations
- Builds and deploys Docker image
- Monitors deployment health
- Provides rollback options

---

## Configuration Files Needed

### 1. Firebase Service Account JSON

File: `firebase-credentials-prod.json`
```json
{
  "type": "service_account",
  "project_id": "recruitment-platform-prod",
  "private_key_id": "xxxxx",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...",
  "client_email": "firebase-adminsdk-xxxxx@...",
  ...
}
```

### 2. appsettings.Production.json

```json
{
  "Firebase": {
    "Enabled": true,
    "ProjectId": "recruitment-platform-prod",
    "CredentialPath": "@Microsoft.KeyVault(SecretUri=https://...)"
  },
  "FCM": {
    "AndroidPriority": "high",
    "BatchSize": 500
  }
}
```

### 3. Azure Key Vault Setup

```powershell
# Store credentials
Set-AzKeyVaultSecret -VaultName "your-vault" `
  -Name "FirebaseServiceAccountKey" `
  -SecretValue (Get-Content firebase-credentials-prod.json | ConvertTo-SecureString -AsPlainText -Force)
```

---

## Manual Steps (if script fails)

### 1. Create Firebase Project

1. Visit: https://console.firebase.google.com
2. Click "Add project"
3. Name: `recruitment-platform-prod`
4. Region: Southeast Asia
5. Click Create

### 2. Register Apps

- Android: Package `com.recruitment.app`
- iOS: Bundle ID `com.recruitment.app`

### 3. Download Credentials

- Download `google-services.json` (Android)
- Download `GoogleService-Info.plist` (iOS)
- Download Service Account Key JSON

### 4. Store in Key Vault

```powershell
$key = Get-Content firebase-credentials-prod.json -Raw
Set-AzKeyVaultSecret -VaultName your-vault `
  -Name FirebaseServiceAccountKey `
  -SecretValue $key
```

### 5. Run Migrations

```powershell
cd src/Services/Notification/Notification.Infrastructure
dotnet ef database update --context NotificationDbContext
```

### 6. Deploy Service

```powershell
docker build -t notification-service:prod-v1.0 .
docker tag notification-service:prod-v1.0 your-registry.azurecr.io/notification-service:prod-v1.0
docker push your-registry.azurecr.io/notification-service:prod-v1.0
kubectl set image deployment/notification-service \
  notification-service=your-registry.azurecr.io/notification-service:prod-v1.0 -n production
```

---

## Verification Commands

```powershell
# Check Firebase tables exist
SELECT COUNT(*) FROM DEVICE_TOKENS;
SELECT COUNT(*) FROM PUSH_NOTIFICATION_LOG;
SELECT COUNT(*) FROM NOTIFICATION_SETTINGS;

# Check Pod status
kubectl get pods -n production -l app=notification-service

# Check logs
kubectl logs -f deployment/notification-service -n production

# Test FCM endpoint
curl -X POST https://api.production/api/device-tokens/register \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d '{"token":"test_token","deviceType":"Android"}'

# Check metrics
kubectl top pods -n production -l app=notification-service
```

---

## Troubleshooting

### Firebase Connection Failed

```powershell
# Verify Key Vault secret
$secret = Get-AzKeyVaultSecret -VaultName your-vault -Name FirebaseServiceAccountKey
$secret.SecretValue | ConvertFrom-SecureString -AsPlainText | ConvertFrom-Json

# Verify project ID matches
# Verify credentials JSON format is valid
```

### Migration Failed

```powershell
# Check existing migrations
dotnet ef migrations list

# List applied migrations
SELECT * FROM __EFMigrationsHistory

# If table exists, skip migration
dotnet ef migrations remove
```

### Pod Crash Loop

```powershell
# Check pod logs
kubectl logs -f pod-name -n production --previous

# Check events
kubectl describe pod pod-name -n production

# Check resource limits
kubectl top pod pod-name -n production
```

---

## Rollback Steps

### Quick Disable FCM

```powershell
kubectl set env deployment/notification-service \
  FCM_ENABLED=false \
  -n production
```

### Restore Previous Version

```powershell
# List deployment history
kubectl rollout history deployment/notification-service -n production

# Rollback to previous
kubectl rollout undo deployment/notification-service -n production
```

### Restore Database

```powershell
Restore-SqlDatabase -ServerInstance production-server \
  -DatabaseName RecruitmentDB \
  -BackupFile "D:\Backups\RecruitmentDB_PreFCM.bak" \
  -ReplaceDatabase
```

---

## Reference Links

- Firebase Console: https://console.firebase.google.com
- Google Cloud Console: https://console.cloud.google.com
- Azure Portal: https://portal.azure.com
- Firebase Docs: https://firebase.google.com/docs
- FCM Guide: https://firebase.google.com/docs/cloud-messaging

---

## Next Steps

After deployment:
1. Monitor metrics for 24 hours
2. Gradual rollout to users (Android 15% → 100%, iOS 5% → 100%)
3. Collect user feedback
4. Optimize based on metrics
5. Document lessons learned

Status: Ready for Phase 5 Implementation
