# Azure Production Deployment Security Guide

## 🔒 Security Overview

This guide covers secure deployment to Azure with **zero secrets in Git**.

**Security Levels:**
1. ✅ **Local Development**: `.env` file (gitignored)
2. ✅ **Azure Staging/Production**: Azure Key Vault + Managed Identity

---

## Part 1: Remove Secrets from Git (DONE ✅)

### Files Created:
- `.env.example` - Template with placeholder values
- `.env` - Local development secrets (gitignored)
- `.gitignore` - Updated to ignore all secret files

### Docker Compose Updated:
- All hardcoded secrets replaced with `${VARIABLE}` references
- Reads from `.env` file automatically

**Usage:**
```bash
# 1. Copy example file
cp .env.example .env

# 2. Edit .env with your actual credentials
nano .env

# 3. Run docker compose
docker compose up -d payment-service
```

---

## Part 2: Azure Key Vault Integration (CODE READY ✅)

### Prerequisites

1. **Azure CLI installed**:
```bash
# Install Azure CLI
# Windows: Download from https://aka.ms/installazurecliwindows
# Mac: brew install azure-cli
# Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login
```

2. **Set subscription**:
```bash
az account set --subscription "<Your-Subscription-Name>"
```

---

## Part 3: Setup Azure Key Vault

### Step 1: Create Key Vault

```bash
# Variables
RESOURCE_GROUP="skillsnap-prod-rg"
LOCATION="southeastasia"
KEY_VAULT_NAME="skillsnap-kv-prod"  # Must be globally unique

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true
```

### Step 2: Add Secrets to Key Vault

```bash
# Database Password
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "SqlServerPassword" \
  --value "YourProductionPassword@123!"

# JWT Secret
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "JwtSecret" \
  --value "your-production-jwt-secret-min-32-chars"

# VNPay Credentials
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "VNPayTmnCode" \
  --value "YOUR_PRODUCTION_VNPAY_CODE"

az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "VNPayHashSecret" \
  --value "YOUR_PRODUCTION_VNPAY_SECRET"

# MoMo Credentials
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoPartnerCode" \
  --value "YOUR_PRODUCTION_MOMO_CODE"

az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoAccessKey" \
  --value "YOUR_PRODUCTION_MOMO_ACCESS"

az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoSecretKey" \
  --value "YOUR_PRODUCTION_MOMO_SECRET"

# RabbitMQ Password
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "RabbitMqPassword" \
  --value "your-production-rabbitmq-password"
```

### Step 3: List Secrets (Verify)

```bash
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table
```

---

## Part 4: Deploy to Azure App Service

### Option A: Container Apps (Recommended for Microservices)

```bash
# Create Container Apps Environment
ENVIRONMENT_NAME="skillsnap-env-prod"

az containerapp env create \
  --name $ENVIRONMENT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Deploy Payment Service
az containerapp create \
  --name payment-service \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image youracr.azurecr.io/payment-service:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --registry-server youracr.azurecr.io \
  --registry-username <ACR-USERNAME> \
  --registry-password <ACR-PASSWORD>
```

### Option B: Azure App Service (Web Apps)

```bash
# Create App Service Plan
az appservice plan create \
  --name skillsnap-plan-prod \
  --resource-group $RESOURCE_GROUP \
  --is-linux \
  --sku P1V3

# Create Web App
az webapp create \
  --name skillsnap-payment-prod \
  --resource-group $RESOURCE_GROUP \
  --plan skillsnap-plan-prod \
  --deployment-container-image-name youracr.azurecr.io/payment-service:latest
```

---

## Part 5: Configure Managed Identity

### Enable System-Assigned Managed Identity

**For Container Apps:**
```bash
az containerapp identity assign \
  --name payment-service \
  --resource-group $RESOURCE_GROUP \
  --system-assigned
```

**For App Service:**
```bash
az webapp identity assign \
  --name skillsnap-payment-prod \
  --resource-group $RESOURCE_GROUP
```

### Grant Key Vault Access

```bash
# Get the Principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name payment-service \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Or for App Service:
# PRINCIPAL_ID=$(az webapp identity show \
#   --name skillsnap-payment-prod \
#   --resource-group $RESOURCE_GROUP \
#   --query principalId -o tsv)

# Assign Key Vault Secrets User role
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $PRINCIPAL_ID \
  --scope /subscriptions/<SUBSCRIPTION-ID>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

---

## Part 6: Configure Application Settings

### Add Key Vault Reference

**For Container Apps:**
```bash
az containerapp update \
  --name payment-service \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    "Azure__KeyVault__Url=https://$KEY_VAULT_NAME.vault.azure.net/" \
    "ConnectionStrings__PaymentDb=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/SqlServerPassword/)" \
    "Jwt__Secret=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/JwtSecret/)" \
    "VNPay__TmnCode=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/VNPayTmnCode/)" \
    "VNPay__HashSecret=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/VNPayHashSecret/)"
```

**For App Service:**
```bash
az webapp config appsettings set \
  --name skillsnap-payment-prod \
  --resource-group $RESOURCE_GROUP \
  --settings \
    Azure__KeyVault__Url="https://$KEY_VAULT_NAME.vault.azure.net/" \
    ConnectionStrings__PaymentDb="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/SqlServerPassword/)" \
    Jwt__Secret="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/JwtSecret/)"
```

---

## Part 7: Update Application Code

### Payment.API/Program.cs

Add Key Vault configuration **at the very top** of Program.cs:

```csharp
using Payment.Infrastructure.Azure;
using Payment.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Azure Key Vault (if configured)
builder.Configuration.AddAzureKeyVault();

// ✅ Validate required secrets in Production
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.ValidateRequiredSecrets(
        "Jwt:Secret",
        "VNPay:TmnCode",
        "VNPay:HashSecret",
        "MoMo:PartnerCode",
        "MoMo:AccessKey",
        "MoMo:SecretKey"
    );
}

// Rest of your existing code...
```

### Add NuGet Packages

```bash
cd src/Services/Payment/Payment.Infrastructure
dotnet add package Azure.Identity --version 1.10.4
dotnet add package Azure.Security.KeyVault.Secrets --version 4.5.0
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets --version 1.3.0
```

---

## Part 8: Secret Naming Convention

### Key Vault Secret Names

Azure Key Vault only allows alphanumeric and dashes. Use this mapping:

| Config Key | Key Vault Secret Name |
|------------|----------------------|
| `Jwt:Secret` | `JwtSecret` |
| `VNPay:TmnCode` | `VNPayTmnCode` |
| `VNPay:HashSecret` | `VNPayHashSecret` |
| `MoMo:PartnerCode` | `MoMoPartnerCode` |
| `MoMo:AccessKey` | `MoMoAccessKey` |
| `MoMo:SecretKey` | `MoMoSecretKey` |
| `ConnectionStrings:PaymentDb` | `ConnectionStrings--PaymentDb` |

**Note:** Azure automatically converts `--` to `:` in configuration.

---

## Part 9: Deployment Checklist

### Pre-Deployment

- [ ] All secrets removed from `appsettings.json`
- [ ] `.env` file is gitignored
- [ ] `.env.example` has placeholders only
- [ ] Build succeeds locally with `.env`

### Azure Setup

- [ ] Resource Group created
- [ ] Key Vault created
- [ ] All secrets added to Key Vault
- [ ] Managed Identity enabled
- [ ] Key Vault access granted

### Post-Deployment

- [ ] Application starts without errors
- [ ] Can read secrets from Key Vault (check logs)
- [ ] VNPay/MoMo webhooks work
- [ ] Payment creation succeeds

---

## Part 10: Local Development with Azure

### Option 1: Azure CLI Authentication

```bash
# Login with your Azure account
az login

# Your app will use your Azure credentials locally
dotnet run
```

### Option 2: Visual Studio Authentication

1. Tools → Options → Azure Service Authentication
2. Select your account
3. Run application (F5)

### Option 3: Use .env file (Easiest)

Keep `.env` file for local development - it takes precedence over Key Vault.

---

## Troubleshooting

### "Access denied" when reading Key Vault

**Check Managed Identity:**
```bash
az containerapp identity show \
  --name payment-service \
  --resource-group $RESOURCE_GROUP
```

**Check RBAC assignment:**
```bash
az role assignment list \
  --assignee <PRINCIPAL-ID> \
  --scope /subscriptions/<SUB-ID>/resourceGroups/$RESOURCE_GROUP
```

### Secrets not loading

**Check Key Vault URL in app settings:**
```bash
az containerapp show \
  --name payment-service \
  --resource-group $RESOURCE_GROUP \
  --query properties.template.containers[0].env
```

### Local development can't access Key Vault

1. Login: `az login`
2. Check account: `az account show`
3. Your account needs "Key Vault Secrets User" role

---

## Security Best Practices

1. ✅ **Never commit secrets to Git**
2. ✅ **Use different Key Vaults for dev/staging/prod**
3. ✅ **Rotate secrets regularly** (90 days)
4. ✅ **Enable Key Vault audit logging**
5. ✅ **Use Managed Identity** (no connection strings)
6. ✅ **Restrict Key Vault network** (allow only Azure services)
7. ✅ **Enable soft-delete** on Key Vault

### Enable Soft Delete

```bash
az keyvault update \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --enable-soft-delete true \
  --enable-purge-protection true
```

### Enable Audit Logging

```bash
# Create Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name skillsnap-logs

# Enable diagnostics
az monitor diagnostic-settings create \
  --name KeyVault-Diagnostics \
  --resource /subscriptions/<SUB-ID>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME \
  --logs '[{"category": "AuditEvent", "enabled": true}]' \
  --workspace /subscriptions/<SUB-ID>/resourceGroups/$RESOURCE_GROUP/providers/microsoft.operationalinsights/workspaces/skillsnap-logs
```

---

## Quick Reference

### Environment Variable Priority (Lowest to Highest)

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment Variables
4. **Azure Key Vault** (highest priority)

### Useful Commands

```bash
# List all secrets
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table

# Get secret value
az keyvault secret show --vault-name $KEY_VAULT_NAME --name JwtSecret --query value -o tsv

# Update secret
az keyvault secret set --vault-name $KEY_VAULT_NAME --name JwtSecret --value "new-value"

# Delete secret (soft delete)
az keyvault secret delete --vault-name $KEY_VAULT_NAME --name JwtSecret

# Purge deleted secret (permanent)
az keyvault secret purge --vault-name $KEY_VAULT_NAME --name JwtSecret
```

---

## Cost Optimization

**Key Vault Pricing:**
- Operations: ~$0.03 per 10,000 operations
- For typical app: **<$5/month**

**Managed Identity:**
- **FREE** ✅

**Recommendation:** Use Key Vault for all environments (cost is negligible vs security benefit).
