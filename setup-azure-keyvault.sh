#!/bin/bash
# =============================================================================
# Azure Key Vault Setup Script
# =============================================================================
# This script automates Azure Key Vault setup for Payment Service
# Usage: ./setup-azure-keyvault.sh
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}Azure Key Vault Setup for SkillSnap${NC}"
echo -e "${GREEN}=====================================${NC}"
echo

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Install from: https://aka.ms/installazurecli"
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}Not logged in to Azure. Please login...${NC}"
    az login
fi

# Configuration
echo -e "${YELLOW}Enter configuration details:${NC}"
read -p "Resource Group Name [skillsnap-prod-rg]: " RESOURCE_GROUP
RESOURCE_GROUP=${RESOURCE_GROUP:-skillsnap-prod-rg}

read -p "Azure Region [southeastasia]: " LOCATION
LOCATION=${LOCATION:-southeastasia}

read -p "Key Vault Name (must be globally unique) [skillsnap-kv-$(date +%s)]: " KEY_VAULT_NAME
KEY_VAULT_NAME=${KEY_VAULT_NAME:-skillsnap-kv-$(date +%s)}

read -p "Environment (dev/staging/prod) [prod]: " ENVIRONMENT
ENVIRONMENT=${ENVIRONMENT:-prod}

echo
echo -e "${GREEN}Configuration:${NC}"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Key Vault: $KEY_VAULT_NAME"
echo "  Environment: $ENVIRONMENT"
echo

read -p "Continue? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

# Create Resource Group
echo -e "${GREEN}Creating Resource Group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Key Vault
echo -e "${GREEN}Creating Key Vault...${NC}"
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true \
  --enable-soft-delete true \
  --enable-purge-protection true

# Add Secrets
echo
echo -e "${YELLOW}Enter secrets (they will be hidden):${NC}"

read -sp "SQL Server Password: " SQL_PASSWORD
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "SqlServerPassword" \
  --value "$SQL_PASSWORD" > /dev/null

read -sp "JWT Secret (min 32 chars): " JWT_SECRET
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "JwtSecret" \
  --value "$JWT_SECRET" > /dev/null

read -p "VNPay TMN Code: " VNPAY_CODE
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "VNPayTmnCode" \
  --value "$VNPAY_CODE" > /dev/null

read -sp "VNPay Hash Secret: " VNPAY_SECRET
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "VNPayHashSecret" \
  --value "$VNPAY_SECRET" > /dev/null

read -p "MoMo Partner Code: " MOMO_CODE
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoPartnerCode" \
  --value "$MOMO_CODE" > /dev/null

read -sp "MoMo Access Key: " MOMO_ACCESS
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoAccessKey" \
  --value "$MOMO_ACCESS" > /dev/null

read -sp "MoMo Secret Key: " MOMO_SECRET
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "MoMoSecretKey" \
  --value "$MOMO_SECRET" > /dev/null

read -sp "RabbitMQ Password: " RABBITMQ_PASSWORD
echo
az keyvault secret set --vault-name $KEY_VAULT_NAME \
  --name "RabbitMqPassword" \
  --value "$RABBITMQ_PASSWORD" > /dev/null

# Verify secrets
echo
echo -e "${GREEN}Secrets created successfully!${NC}"
echo
echo -e "${YELLOW}Verifying...${NC}"
az keyvault secret list --vault-name $KEY_VAULT_NAME --output table

# Output summary
echo
echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}Setup Complete!${NC}"
echo -e "${GREEN}=====================================${NC}"
echo
echo "Key Vault URL: https://$KEY_VAULT_NAME.vault.azure.net/"
echo
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Enable Managed Identity on your App Service/Container App"
echo "2. Grant Key Vault access:"
echo "   az role assignment create \\"
echo "     --role \"Key Vault Secrets User\" \\"
echo "     --assignee <PRINCIPAL-ID> \\"
echo "     --scope /subscriptions/<SUB-ID>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"
echo
echo "3. Add to application settings:"
echo "   Azure__KeyVault__Url=https://$KEY_VAULT_NAME.vault.azure.net/"
echo
echo -e "${GREEN}Done!${NC}"
