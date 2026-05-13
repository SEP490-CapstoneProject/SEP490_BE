# ✅ FCM Complete Setup Checklist

## Pre-Execution Checklist

### 1. Prerequisites
- [ ] PowerShell 7+ installed (`pwsh --version`)
- [ ] Azure CLI installed (`az --version`)
- [ ] Docker installed (`docker --version`)
- [ ] .NET 8 SDK installed (`dotnet --version`)
- [ ] Git installed (`git --version`)

### 2. Azure Authentication
- [ ] Azure CLI authenticated (`az login` completed)
- [ ] Correct subscription selected:
  - [ ] Expected: `d73e86d1-62d3-4a34-acf0-47a52f788f8d`
  - [ ] Verify: `az account show --query id`

### 3. Credentials & Secrets
- [ ] Firebase Service Account JSON downloaded
  - [ ] File: `firebase-credentials-prod.json`
  - [ ] Project ID: `skillsnap-notification` ✓
  - [ ] Validated JSON format

- [ ] Database credentials ready:
  - [ ] Server: `skillsnapsql2604282023545.database.windows.net` ✓
  - [ ] Database: `skillsnap-db` ✓
  - [ ] Username: `sqladmin` ✓
  - [ ] Password: Ready to enter ✓

- [ ] ACR credentials ready:
  - [ ] URL: `skillsnapacr2604282023545.azurecr.io` ✓
  - [ ] Username: `skillsnapacr2604282023545` ✓
  - [ ] Password: Ready to enter ✓

### 4. Azure Resources Exist
- [ ] Key Vault exists: `sskv2604282023545` ✓
- [ ] SQL Database exists: `skillsnap-db` ✓
- [ ] ACR exists: `skillsnapacr2604282023545` ✓
- [ ] Container Apps environment exists (if not, create first)
- [ ] Container App `notification-service` exists (if not, create first)

### 5. Repository Setup
- [ ] Working directory: `D:\Capstone` ✓
- [ ] Git repo initialized
- [ ] All project files in place:
  - [ ] Solution file: `SEP490_BE.sln`
  - [ ] Notification service: `src/Services/Notification/`
  - [ ] Dockerfile present (or will copy)

---

## Execution Steps

### Step 1: Firebase Setup (5-10 minutes)

#### Manual Preparation
- [ ] Navigate to Firebase Console: https://console.firebase.google.com
- [ ] Select project: `skillsnap-notification`
- [ ] Register Android app:
  - [ ] Package name: `com.skillsnap.app` ✓
  - [ ] Download: `google-services.json`
  - [ ] Share with mobile team
- [ ] Generate Service Account Key:
  - [ ] Go to: Settings → Service Accounts
  - [ ] Generate new private key
  - [ ] Download as JSON
  - [ ] Save as: `firebase-credentials-prod.json` in `D:\Capstone`

#### Script Execution
```powershell
cd D:\Capstone
.\Setup-FCM-Complete-Automation.ps1 -Step Firebase
```

#### Verification
- [ ] Script completes without errors
- [ ] Key Vault secret created: `firebase-adminsdk-json`
- [ ] Secret URI displayed: `https://sskv2604282023545.vault.azure.net/secrets/firebase-adminsdk-json/`
- [ ] Log message: `✓ Firebase credentials stored in Key Vault`

---

### Step 2: Database Migration (10-15 minutes)

#### Pre-checks
- [ ] SQL Server accessible:
  ```powershell
  Test-NetConnection -ComputerName skillsnapsql2604282023545.database.windows.net -Port 1433
  ```
  Expected: `TcpTestSucceeded: True`

- [ ] Database exists:
  ```sql
  SELECT name FROM sys.databases WHERE name = 'skillsnap-db'
  ```
  Expected: 1 row returned

#### Script Execution
```powershell
.\Setup-FCM-Complete-Automation.ps1 -Step Database
```

#### Verification
- [ ] Script completes without errors
- [ ] Log messages:
  - [ ] `✓ Database connection successful`
  - [ ] `✓ Database migrations applied`
- [ ] Tables created and verified:
  ```sql
  SELECT COUNT(*) FROM DEVICE_TOKENS
  SELECT COUNT(*) FROM PUSH_NOTIFICATION_LOG
  SELECT COUNT(*) FROM NOTIFICATION_SETTINGS
  ```
  Expected: 3 rows (each table exists)

---

### Step 3: Docker Build & ACR Push (15-25 minutes)

#### Pre-checks
- [ ] Docker daemon running:
  ```powershell
  docker ps
  ```
  Expected: No "Cannot connect" error

- [ ] ACR reachable:
  ```powershell
  docker login skillsnapacr2604282023545.azurecr.io -u skillsnapacr2604282023545
  ```
  Enter password when prompted

#### Script Execution
```powershell
.\Setup-FCM-Complete-Automation.ps1 -Step Docker
```

#### Verification
- [ ] Script completes without errors
- [ ] Log messages:
  - [ ] `✓ ACR login successful`
  - [ ] `✓ Docker image built: ...`
  - [ ] `✓ Image pushed to ACR`
  - [ ] `✓ Latest tag pushed`

- [ ] Image in ACR:
  ```powershell
  az acr repository show-tags \
    --name skillsnapacr2604282023545 \
    --repository notification-service
  ```
  Expected: Latest tag + timestamp tag visible

---

### Step 4: Deploy to Container Apps (10-20 minutes)

#### Pre-checks
- [ ] Container Apps environment exists:
  ```powershell
  az containerapp env show \
    --resource-group skillsnap-rg-2604282023 \
    --name skillsnap-container-env
  ```

- [ ] Container App exists:
  ```powershell
  az containerapp show \
    --resource-group skillsnap-rg-2604282023 \
    --name notification-service
  ```

#### Script Execution
```powershell
.\Setup-FCM-Complete-Automation.ps1 -Step Deploy
```

#### Verification
- [ ] Script completes without errors
- [ ] Log messages:
  - [ ] `✓ Container App exists`
  - [ ] `✓ Container App updated`
  - [ ] `✓ Deployment complete`
  - [ ] Application URL displayed

- [ ] Deployment status:
  ```powershell
  az containerapp show \
    --resource-group skillsnap-rg-2604282023 \
    --name notification-service \
    --query properties.provisioningState
  ```
  Expected: `Succeeded`

- [ ] Container is running (wait 1-2 minutes):
  ```powershell
  az containerapp replica list \
    --resource-group skillsnap-rg-2604282023 \
    --container-app-name notification-service
  ```
  Expected: Replicas with `Ready: true`

---

### Step 5: Verification (5-10 minutes)

#### Script Execution
```powershell
.\Setup-FCM-Complete-Automation.ps1 -Step Verify
```

#### Manual Verification

**1. Database Tables**
```sql
-- Check DEVICE_TOKENS
SELECT COUNT(*) as TokenCount FROM DEVICE_TOKENS
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DEVICE_TOKENS'

-- Check PUSH_NOTIFICATION_LOG
SELECT COUNT(*) as LogCount FROM PUSH_NOTIFICATION_LOG
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PUSH_NOTIFICATION_LOG'

-- Check NOTIFICATION_SETTINGS
SELECT COUNT(*) as SettingsCount FROM NOTIFICATION_SETTINGS
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NOTIFICATION_SETTINGS'
```
Expected: All 3 tables exist with columns

**2. API Health Check**
```bash
# Wait 2-3 minutes for container to warm up
curl -X GET https://<app-url>/health \
  -H "Accept: application/json"
```
Expected: `200 OK` with health status

**3. Container App Logs**
```powershell
az containerapp logs show \
  --resource-group skillsnap-rg-2604282023 \
  --name notification-service \
  --tail 50
```
Expected: No `ERROR` or `FATAL` messages

**4. Configuration Verification**
- [ ] Key Vault secrets exist:
  ```powershell
  az keyvault secret list \
    --vault-name sskv2604282023545 \
    --query "[?contains(name, 'firebase')].name"
  ```
  Expected: `firebase-adminsdk-json`

---

## Post-Setup Steps

### 1. Mobile Team Integration (External)
- [ ] Download `google-services.json` from Firebase Console
- [ ] Share `FCM_MOBILE_SETUP_GUIDE.md` with mobile team
- [ ] Provide API endpoint URL (from Container Apps)
- [ ] Mobile team implements device token registration

### 2. Testing
- [ ] Test device token registration:
  ```bash
  curl -X POST https://<app-url>/api/device-tokens/register \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
      "token": "test_token_123",
      "deviceType": "Android",
      "appVersion": "12"
    }'
  ```

- [ ] Check database:
  ```sql
  SELECT * FROM DEVICE_TOKENS ORDER BY CreatedAt DESC
  ```

- [ ] Send test push from Firebase Console:
  - [ ] Go to: Cloud Messaging → Send test message
  - [ ] Enter token from above
  - [ ] Click Send
  - [ ] Check logs: `SELECT * FROM PUSH_NOTIFICATION_LOG ORDER BY CreatedAt DESC`

### 3. Monitoring Setup
- [ ] Configure Azure Monitor alerts:
  ```powershell
  az monitor metrics list \
    --resource-group skillsnap-rg-2604282023 \
    --resource notification-service
  ```

- [ ] Enable logging:
  - [ ] Check Container App logs daily
  - [ ] Monitor FCM error rates
  - [ ] Track push delivery success rate

### 4. Documentation
- [ ] Update team wiki with:
  - [ ] API endpoint URL
  - [ ] Firebase project details
  - [ ] Database connection string (if needed)
  - [ ] Troubleshooting guide

---

## Troubleshooting Checklist

### If Firebase Step Fails
- [ ] Validate Firebase credentials JSON:
  ```powershell
  $json = Get-Content firebase-credentials-prod.json | ConvertFrom-Json
  $json.project_id  # Should output: skillsnap-notification
  ```
- [ ] Check Key Vault access:
  ```powershell
  az keyvault secret show \
    --vault-name sskv2604282023545 \
    --name firebase-adminsdk-json
  ```
- [ ] Retry step: `.\Setup-FCM-Complete-Automation.ps1 -Step Firebase`

### If Database Step Fails
- [ ] Verify SQL connection:
  ```powershell
  Test-NetConnection -ComputerName skillsnapsql2604282023545.database.windows.net -Port 1433
  ```
- [ ] Check credentials:
  - [ ] Username: `sqladmin`
  - [ ] Password: Verify in Key Vault (if stored)
- [ ] Check database name:
  ```sql
  SELECT name FROM sys.databases WHERE name = 'skillsnap-db'
  ```
- [ ] Retry step: `.\Setup-FCM-Complete-Automation.ps1 -Step Database`

### If Docker Step Fails
- [ ] Verify Docker is running: `docker ps`
- [ ] Check ACR login:
  ```powershell
  docker login skillsnapacr2604282023545.azurecr.io
  ```
- [ ] Verify Dockerfile exists:
  ```powershell
  ls Dockerfile.notification
  ```
- [ ] Retry step: `.\Setup-FCM-Complete-Automation.ps1 -Step Docker`

### If Deploy Step Fails
- [ ] Check Container App exists:
  ```powershell
  az containerapp show \
    --resource-group skillsnap-rg-2604282023 \
    --name notification-service
  ```
- [ ] Check deployment status:
  ```powershell
  az containerapp show \
    --resource-group skillsnap-rg-2604282023 \
    --name notification-service \
    --query properties.provisioningState
  ```
- [ ] Check recent revisions:
  ```powershell
  az containerapp revision list \
    --resource-group skillsnap-rg-2604282023 \
    --container-app-name notification-service
  ```
- [ ] Retry step: `.\Setup-FCM-Complete-Automation.ps1 -Step Deploy`

### If Verify Step Fails
- [ ] Database table verification:
  - [ ] Connect to SQL Database manually
  - [ ] Query: `SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'NOTIFICATION%'`
  - [ ] All 3 tables should exist

- [ ] API health check:
  - [ ] Wait another 2 minutes (container warming up)
  - [ ] Check Container App logs for startup errors
  - [ ] Verify endpoint URL is correct

---

## Performance Targets

After successful setup, you should see:

| Metric | Target | How to Check |
|--------|--------|-------------|
| API Response Time | < 200ms | Azure Monitor |
| Push Delivery Rate | > 99% | `PUSH_NOTIFICATION_LOG` |
| Device Token Success Rate | > 98% | Database queries |
| Container CPU | 10-30% | Azure Portal |
| Container Memory | 50-100 MB | Azure Portal |
| Database Connections | 1-5 | Database metrics |

---

## Success Indicators

### ✅ Setup is Complete When:

- [ ] All 5 steps execute successfully (Firebase, Database, Docker, Deploy, Verify)
- [ ] No errors in any step output
- [ ] All 3 database tables created and accessible
- [ ] Docker image successfully pushed to ACR
- [ ] Container App deployed with latest image
- [ ] API endpoint responds to health checks
- [ ] All log messages show `✓ Success` indicators

### ✅ Ready for Production When:

- [ ] Mobile team tested device token registration
- [ ] Test push notifications delivered successfully
- [ ] Monitoring configured and alerts working
- [ ] Team trained on deployment and monitoring
- [ ] Rollback procedure documented and tested
- [ ] Performance metrics within targets
- [ ] Incident response plan prepared

---

## Rollback Checklist

If you need to revert changes:

### Quick Disable (5 minutes)
- [ ] Disable FCM via environment variable
- [ ] Restart Container App
- [ ] Verify app still works with SignalR only

### Full Rollback (30 minutes)
- [ ] Activate previous Container App revision:
  ```powershell
  az containerapp revision activate \
    --resource-group skillsnap-rg-2604282023 \
    --container-app-name notification-service \
    --revision <previous-revision-name>
  ```
- [ ] Verify app functionality
- [ ] Investigate failure
- [ ] Re-attempt setup after fix

### Database Rollback (1 hour)
- [ ] If database state is corrupted:
  - [ ] Restore from backup
  - [ ] Or manually delete and recreate tables
  - [ ] Re-run migrations

---

## Final Sign-Off

- [ ] All checklist items completed
- [ ] No outstanding issues
- [ ] Documentation updated
- [ ] Team trained
- [ ] Monitoring configured
- [ ] Ready for production

**Setup Date:** _________________

**Approved By:** _________________

**Notes:** _________________________________________________________________________

