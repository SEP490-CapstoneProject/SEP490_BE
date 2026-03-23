# =============================================================================
# Azure Key Vault Setup Script (PowerShell)
# =============================================================================
# This script automates Azure Key Vault setup for Payment Service
# Usage: .\setup-azure-keyvault.ps1
# =============================================================================

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Green
Write-Host "Azure Key Vault Setup for SkillSnap" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Check if Azure CLI is installed
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Azure CLI is not installed" -ForegroundColor Red
    Write-Host "Install from: https://aka.ms/installazurecli"
    exit 1
}

# Check if logged in
try {
    az account show | Out-Null
} catch {
    Write-Host "Not logged in to Azure. Please login..." -ForegroundColor Yellow
    az login
}

# Configuration
Write-Host "Enter configuration details:" -ForegroundColor Yellow
$RESOURCE_GROUP = Read-Host "Resource Group Name [skillsnap-prod-rg]"
if ([string]::IsNullOrEmpty($RESOURCE_GROUP)) { $RESOURCE_GROUP = "skillsnap-prod-rg" }

$LOCATION = Read-Host "Azure Region [southeastasia]"
if ([string]::IsNullOrEmpty($LOCATION)) { $LOCATION = "southeastasia" }

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$defaultKV = "skillsnap-kv-$timestamp"
$KEY_VAULT_NAME = Read-Host "Key Vault Name (must be globally unique) [$defaultKV]"
if ([string]::IsNullOrEmpty($KEY_VAULT_NAME)) { $KEY_VAULT_NAME = $defaultKV }

$ENVIRONMENT = Read-Host "Environment (dev/staging/prod) [prod]"
if ([string]::IsNullOrEmpty($ENVIRONMENT)) { $ENVIRONMENT = "prod" }

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  Resource Group: $RESOURCE_GROUP"
Write-Host "  Location: $LOCATION"
Write-Host "  Key Vault: $KEY_VAULT_NAME"
Write-Host "  Environment: $ENVIRONMENT"
Write-Host ""

$continue = Read-Host "Continue? (y/n)"
if ($continue -ne "y" -and $continue -ne "Y") {
    exit 0
}

# Create Resource Group
Write-Host "Creating Resource Group..." -ForegroundColor Green
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Key Vault
Write-Host "Creating Key Vault..." -ForegroundColor Green
az keyvault create `
  --name $KEY_VAULT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --enable-rbac-authorization true `
  --enable-soft-delete true `
  --enable-purge-protection true

# Add Secrets
Write-Host ""
Write-Host "Enter secrets (they will be hidden):" -ForegroundColor Yellow

$SQL_PASSWORD = Read-Host "SQL Server Password" -AsSecureString
$SQL_PASSWORD_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($SQL_PASSWORD))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "SqlServerPassword" `
  --value $SQL_PASSWORD_Plain | Out-Null

$JWT_SECRET = Read-Host "JWT Secret (min 32 chars)" -AsSecureString
$JWT_SECRET_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($JWT_SECRET))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "JwtSecret" `
  --value $JWT_SECRET_Plain | Out-Null

$VNPAY_CODE = Read-Host "VNPay TMN Code"
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "VNPayTmnCode" `
  --value $VNPAY_CODE | Out-Null

$VNPAY_SECRET = Read-Host "VNPay Hash Secret" -AsSecureString
$VNPAY_SECRET_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($VNPAY_SECRET))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "VNPayHashSecret" `
  --value $VNPAY_SECRET_Plain | Out-Null

$MOMO_CODE = Read-Host "MoMo Partner Code"
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "MoMoPartnerCode" `
  --value $MOMO_CODE | Out-Null

$MOMO_ACCESS = Read-Host "MoMo Access Key" -AsSecureString
$MOMO_ACCESS_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($MOMO_ACCESS))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "MoMoAccessKey" `
  --value $MOMO_ACCESS_Plain | Out-Null

$MOMO_SECRET = Read-Host "MoMo Secret Key" -AsSecureString
$MOMO_SECRET_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($MOMO_SECRET))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "MoMoSecretKey" `
  --value $MOMO_SECRET_Plain | Out-Null

$RABBITMQ_PASSWORD = Read-Host "RabbitMQ Password" -AsSecureString
$RABBITMQ_PASSWORD_Plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($RABBITMQ_PASSWORD))
az keyvault secret set --vault-name $KEY_VAULT_NAME `
  --name "RabbitMqPassword" `
  --value $RABBITMQ_PASSWORD_Plain | Out-Null

# Verify secrets
Write-Host ""
Write-Host "Secrets created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Verifying..." -ForegroundColor Yellow
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table

# Output summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Key Vault URL: https://$KEY_VAULT_NAME.vault.azure.net/"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Enable Managed Identity on your App Service/Container App"
Write-Host "2. Grant Key Vault access:"
Write-Host "   az role assignment create \"
Write-Host "     --role `"Key Vault Secrets User`" \"
Write-Host "     --assignee <PRINCIPAL-ID> \"
Write-Host "     --scope /subscriptions/<SUB-ID>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"
Write-Host ""
Write-Host "3. Add to application settings:"
Write-Host "   Azure__KeyVault__Url=https://$KEY_VAULT_NAME.vault.azure.net/"
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
