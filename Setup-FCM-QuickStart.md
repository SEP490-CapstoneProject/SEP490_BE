# 🚀 FCM Complete Setup - Quick Start Guide

## Overview

This guide automates the entire FCM setup process. Just run one command and everything is configured:

```powershell
.\Setup-FCM-Complete-Automation.ps1
```

---

## ✅ What Gets Automated

### Phase 1: Firebase & Azure Key Vault
- ✓ Validate Azure CLI and authentication
- ✓ Create Firebase credentials in Key Vault
- ✓ Generate Key Vault Secret URI for configuration

### Phase 2: Database Migration
- ✓ Test SQL Server connection
- ✓ Apply Entity Framework migrations
- ✓ Create 3 new tables:
  - `DEVICE_TOKENS` - Device registration data
  - `PUSH_NOTIFICATION_LOG` - Delivery tracking
  - `NOTIFICATION_SETTINGS` - User preferences

### Phase 3: Docker Build & ACR Push
- ✓ Authenticate to Azure Container Registry
- ✓ Build Docker image for notification-service
- ✓ Push to ACR with timestamp tag
- ✓ Tag as 'latest' for easy updates

### Phase 4: Azure Container Apps Deployment
- ✓ Update Container App with new image
- ✓ Monitor deployment progress
- ✓ Provide application URL

### Phase 5: Verification
- ✓ Verify database tables exist
- ✓ Test API endpoint health
- ✓ Confirm FCM configuration

---

## 🎯 Quick Start (3 Steps)

### Step 1: Prepare Files

Place these files in the project root:
```
D:\Capstone\
├── Setup-FCM-Complete-Automation.ps1    ← Main script
├── appsettings.Production.json          ← Config file
├── firebase-credentials-prod.json       ← Your Firebase JSON (place here)
└── Dockerfile.notification              ← Docker build file
```

### Step 2: Copy Firebase Credentials

Get your Firebase Service Account Key from:
1. Go to Firebase Console: https://console.firebase.google.com
2. Select project: `skillsnap-notification`
3. Settings → Service Accounts → Generate New Private Key
4. Save as `firebase-credentials-prod.json` in project root

### Step 3: Run Setup

```powershell
# Navigate to project root
cd D:\Capstone

# Make script executable (if on PowerShell 7)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Run complete setup
.\Setup-FCM-Complete-Automation.ps1

# Or run specific steps
.\Setup-FCM-Complete-Automation.ps1 -Step Firebase
.\Setup-FCM-Complete-Automation.ps1 -Step Database
.\Setup-FCM-Complete-Automation.ps1 -Step Docker
.\Setup-FCM-Complete-Automation.ps1 -Step Deploy
.\Setup-FCM-Complete-Automation.ps1 -Step Verify
```

---

## 📋 Configuration

All settings are in `appsettings.Production.json`:

```json
{
  "Firebase": {
    "Enabled": true,
    "ProjectId": "skillsnap-notification",
    "CredentialPath": "@Microsoft.KeyVault(SecretUri=...)",
    "Region": "asia-southeast1"
  },
  "FCM": {
    "Enabled": true,
    "AndroidPriority": "high",
    "BatchSize": 500,
    "MaxRetries": 3
  },
  "MobileApp": {
    "Android": {
      "PackageName": "com.skillsnap.app",
      "MinVersion": 12
    }
  }
}
```

---

## 🔐 Azure Key Vault Secrets

The script creates these secrets in Key Vault:

| Secret Name | Purpose |
|---|---|
| `firebase-adminsdk-json` | Firebase Service Account |
| `db-password` | Database password |
| `redis-connection` | Redis cache connection |
| `rabbitmq-host` | RabbitMQ broker host |
| `rabbitmq-username` | RabbitMQ username |
| `rabbitmq-password` | RabbitMQ password |

To manually set any secret:
```powershell
az keyvault secret set \
  --vault-name sskv2604282023545 \
  --name secret-name \
  --value "secret-value"
```

---

## 📊 Database Tables Created

### DEVICE_TOKENS
```sql
- DeviceTokenId (PK)
- UserId (FK)
- Token (unique, global)
- DeviceType (Android/iOS)
- AppVersion
- IsActive
- LastUsedAt
- CreatedAt
```

### PUSH_NOTIFICATION_LOG
```sql
- LogId (PK)
- DeviceTokenId (FK)
- NotificationId (FK)
- MessageId (Firebase)
- Status (Sent/Failed/Delivered)
- ErrorMessage
- CreatedAt
```

### NOTIFICATION_SETTINGS
```sql
- SettingsId (PK)
- UserId (FK)
- PushEnabled (default: true)
- SoundEnabled (default: true)
- VibrationEnabled (default: true)
- UpdatedAt
```

---

## 🧪 Testing After Setup

### 1. Register Device Token
```bash
curl -X POST https://<container-app-url>/api/device-tokens/register \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "test_token_abc123",
    "deviceType": "Android",
    "appVersion": "12"
  }'
```

Expected response:
```json
{
  "success": true,
  "message": "Token registered successfully"
}
```

### 2. Send Test Push
```bash
curl -X POST https://console.firebase.google.com \
  -H "Authorization: Bearer $FIREBASE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": {
      "token": "test_token_abc123",
      "notification": {
        "title": "Test Notification",
        "body": "This is a test push notification"
      },
      "android": {
        "priority": "high"
      }
    }
  }'
```

### 3. Check Logs
```powershell
# View Container App logs
az containerapp logs show \
  --resource-group skillsnap-rg-2604282023 \
  --name notification-service \
  --follow

# Check database
SELECT * FROM PUSH_NOTIFICATION_LOG ORDER BY CreatedAt DESC
```

---

## ⚠️ Troubleshooting

### Error: Azure CLI not found
```powershell
# Install Azure CLI
irm https://aka.ms/install-azcliv2.ps1 | iex
```

### Error: Firebase credentials invalid
```powershell
# Validate credentials
$json = Get-Content firebase-credentials-prod.json | ConvertFrom-Json
$json.project_id  # Should be: skillsnap-notification
```

### Error: Database connection failed
```powershell
# Test connection
$server = "skillsnapsql2604282023545.database.windows.net"
Test-NetConnection -ComputerName $server -Port 1433
```

### Error: Docker login failed
```powershell
# Re-authenticate
docker logout skillsnapacr2604282023545.azurecr.io
docker login skillsnapacr2604282023545.azurecr.io

# Provide username and password when prompted
```

### Error: Container App deployment timeout
```powershell
# Check status manually
az containerapp show \
  --resource-group skillsnap-rg-2604282023 \
  --name notification-service \
  --query properties.provisioningState

# Check pods
az containerapp replica list \
  --resource-group skillsnap-rg-2604282023 \
  --container-app-name notification-service
```

---

## 📱 Mobile App Integration

After setup completes, provide mobile team with:

1. **API Endpoint**: `https://<container-app-url>/api`
2. **Device Token Registration**:
   - Endpoint: `POST /device-tokens/register`
   - Body: `{ "token": "fcm_token", "deviceType": "Android" }`

3. **FCM Configuration** (Android):
   - Download `google-services.json` from Firebase Console
   - Place in `android/app/` directory
   - Add Firebase dependencies to build.gradle

See: `FCM_MOBILE_SETUP_GUIDE.md` for complete mobile integration

---

## 🔄 Rollback Procedure

If something goes wrong:

### Disable FCM Quickly
```powershell
az containerapp update \
  --resource-group skillsnap-rg-2604282023 \
  --name notification-service \
  --set-env-vars FCM:Enabled=false
```

### Restore Previous Image
```powershell
# View deployment history
az containerapp revision list \
  --resource-group skillsnap-rg-2604282023 \
  --container-app-name notification-service

# Activate previous revision
az containerapp revision activate \
  --resource-group skillsnap-rg-2604282023 \
  --container-app-name notification-service \
  --revision previous-revision-name
```

### Restore Database
```powershell
# Rollback migrations
cd src/Services/Notification/Notification.Infrastructure
dotnet ef database update PreFcmMigration

# Or restore from backup
Restore-SqlDatabase -ServerInstance skillsnapsql2604282023545.database.windows.net \
  -DatabaseName skillsnap-db \
  -BackupFile "backup_pre_fcm.bak" \
  -ReplaceDatabase
```

---

## 📈 Monitoring

After deployment, monitor these metrics:

```powershell
# Container App metrics
az monitor metrics list \
  --resource-group skillsnap-rg-2604282023 \
  --resource notification-service \
  --metric-names "Requests" "ResponseTime" "Errors"

# Database metrics
az sql server metrics list \
  --resource-group skillsnap-rg-2604282023 \
  --server skillsnapsql2604282023545

# Container App logs
az containerapp logs show \
  --resource-group skillsnap-rg-2604282023 \
  --name notification-service \
  --follow \
  --tail 100
```

---

## ✅ Checklist - Setup Complete

- [ ] Firebase credentials in Key Vault
- [ ] Database tables created (`DEVICE_TOKENS`, `PUSH_NOTIFICATION_LOG`, `NOTIFICATION_SETTINGS`)
- [ ] Docker image built and pushed to ACR
- [ ] Container App deployed with new image
- [ ] API endpoint responding to health check
- [ ] Device token registration working
- [ ] Push notifications sending successfully
- [ ] Metrics and logs accessible
- [ ] Mobile team configured with API details

---

## 🎉 Success!

Your FCM notification system is now:
- ✅ Fully automated
- ✅ Production-ready
- ✅ Monitoring-enabled
- ✅ Scalable on Azure Container Apps

**Next Step:** Configure mobile app for FCM integration (see `FCM_MOBILE_SETUP_GUIDE.md`)

---

## 📞 Support

For issues or questions:
1. Check `FIREBASE_SETUP_HELPER.md` for common troubleshooting
2. Review logs: `az containerapp logs show ...`
3. Check database: Query `PUSH_NOTIFICATION_LOG` for delivery status
4. Verify configuration: Check `appsettings.Production.json`
