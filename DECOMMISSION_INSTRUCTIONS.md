# Azure Decommission via GitHub Actions

## Quick Start

This GitHub Actions workflow will delete your Azure App Service automatically.

---

## Step 1: Set Up Azure Credentials (One-Time Setup)

### Option A: Using Azure CLI (Recommended - 2 minutes)

```bash
# Login to Azure
az login

# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo "Subscription ID: $SUBSCRIPTION_ID"

# Create a service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "github-actions-decommission" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth

# Copy the JSON output (you'll need this in Step 2)
```

**Expected Output** (copy this entire JSON):
```json
{
  "clientId": "12345678-1234-1234-1234-123456789012",
  "clientSecret": "your-secret-here",
  "subscriptionId": "87654321-4321-4321-4321-210987654321",
  "tenantId": "abcdefab-abcd-abcd-abcd-abcdefabcdef",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

---

### Option B: Using Azure Portal (5 minutes)

1. **Go to Azure Portal**: https://portal.azure.com
2. **Navigate to**: Azure Active Directory → App registrations → New registration
3. **Create App**:
   - Name: `github-actions-decommission`
   - Click "Register"
4. **Create Secret**:
   - Certificates & secrets → New client secret
   - Description: "GitHub Actions"
   - Copy the secret value (shown only once!)
5. **Assign Role**:
   - Subscriptions → Your subscription → Access control (IAM)
   - Add role assignment → Contributor
   - Select your app: `github-actions-decommission`
6. **Get IDs**:
   - Copy: Application (client) ID
   - Copy: Directory (tenant) ID
   - Copy: Subscription ID

**Create JSON manually**:
```json
{
  "clientId": "YOUR_CLIENT_ID",
  "clientSecret": "YOUR_CLIENT_SECRET",
  "subscriptionId": "YOUR_SUBSCRIPTION_ID",
  "tenantId": "YOUR_TENANT_ID",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

---

## Step 2: Add Secret to GitHub

1. **Go to GitHub Repository**:
   - https://github.com/sirjoon/azure-s3-uploader

2. **Navigate to Settings**:
   - Settings → Secrets and variables → Actions

3. **Add New Secret**:
   - Click "New repository secret"
   - Name: `AZURE_CREDENTIALS`
   - Value: Paste the entire JSON from Step 1
   - Click "Add secret"

✅ **Secret added successfully**

---

## Step 3: Run the Decommission Workflow

### Via GitHub Website:

1. **Go to Actions Tab**:
   - https://github.com/sirjoon/azure-s3-uploader/actions

2. **Select Workflow**:
   - Click "Decommission Azure Resources" (left sidebar)

3. **Run Workflow**:
   - Click "Run workflow" button (top right)
   - Branch: `master` (or `main`)
   - Type: `DELETE` (to confirm deletion)
   - Delete resource group?: `false` (or `true` to delete everything)
   - Click "Run workflow"

4. **Monitor Progress**:
   - Workflow starts immediately
   - Click on the running workflow to see live logs
   - Completes in ~1-2 minutes

✅ **Azure resources deleted**

---

### Via GitHub CLI (Optional):

```bash
# Install GitHub CLI
brew install gh

# Login to GitHub
gh auth login

# Run the decommission workflow
gh workflow run decommission-azure.yml \
  -f confirm_deletion=DELETE \
  -f delete_resource_group=false

# View workflow run
gh run list --workflow=decommission-azure.yml
gh run watch
```

---

## Step 4: Verify Deletion

### Check in Azure Portal:

1. **Go to**: https://portal.azure.com
2. **Search**: "s3uploader-plan"
3. **Expected**: No results (app deleted)

### Check via Azure CLI:

```bash
az webapp list --query "[?name=='s3uploader-plan']"
# Expected: []

az appservice plan list --query "[?name=='s3uploader-plan']"
# Expected: []
```

✅ **Verification complete**

---

## What Gets Deleted

| Resource | Action |
|----------|--------|
| **App Service** | ✅ Deleted |
| **App Service Plan** | ✅ Deleted |
| **Environment Variables** | ✅ Deleted (with app) |
| **Custom Domains** | ✅ Deleted (with app) |
| **SSL Certificates** | ✅ Deleted (with app) |
| **Resource Group** | ⚠️ Optional (set to `true` to delete) |

---

## Cost Savings

**Before Decommission**:
- Azure App Service (Standard S1): $69.35/month

**After Decommission**:
- All Azure costs: $0.00/month

**Savings**: $69.35/month = **$832/year** 💰

---

## Troubleshooting

### Issue 1: "Secret AZURE_CREDENTIALS not found"

**Cause**: GitHub secret not configured

**Fix**:
1. Go to: Settings → Secrets and variables → Actions
2. Add secret: `AZURE_CREDENTIALS`
3. Paste JSON from Step 1

---

### Issue 2: "Insufficient permissions"

**Cause**: Service principal doesn't have Contributor role

**Fix**:
```bash
# Get service principal ID
SP_ID=$(az ad sp list --display-name "github-actions-decommission" --query "[0].id" -o tsv)

# Assign Contributor role
az role assignment create \
  --assignee $SP_ID \
  --role Contributor \
  --scope /subscriptions/$(az account show --query id -o tsv)
```

---

### Issue 3: "App Service not found"

**Cause**: App already deleted or name incorrect

**Fix**: Check app name in Azure portal, or manually delete via portal

---

## Next Steps After Azure Deletion

Your Azure resources are now deleted. To complete the decommission:

### 1. Delete AWS Resources (Optional):

```bash
# Delete S3 bucket
aws s3 rm s3://my-secure-bucket-test-sirj --recursive --region us-east-1
aws s3 rb s3://my-secure-bucket-test-sirj --region us-east-1

# Delete Lambda functions
aws lambda delete-function --function-name s3-presign-lambda --region us-east-1
aws lambda delete-function --function-name s3-direct-upload-lambda --region us-east-1

# Delete API Gateway
aws apigateway delete-rest-api --rest-api-id k4k5bzpsn0 --region us-east-1
```

### 2. Delete Local Files (Optional):

```bash
# Delete source code
rm -rf /Users/siru/Desktop/azure-s3-uploader

# Delete documentation (if not needed)
rm -f /Users/siru/Documents/*UPLOAD*.md
rm -f /Users/siru/Documents/*LAMBDA*.md
```

### 3. Delete GitHub Repository (Optional):

```bash
# Via GitHub CLI
gh repo delete sirjoon/azure-s3-uploader --yes

# Or via web: https://github.com/sirjoon/azure-s3-uploader/settings
```

---

## Rollback (If Needed)

If you need to restore the app:

1. **Redeploy from GitHub**:
   - The code is still in your repository
   - Use the original deployment workflow

2. **Recreate manually**:
   - Clone repo: `git clone https://github.com/sirjoon/azure-s3-uploader.git`
   - Deploy: `az webapp up --name s3uploader-plan --runtime "DOTNET:8.0"`

**Time to restore**: ~10 minutes

---

## Security Note

**After decommissioning**:

- [ ] Delete the service principal: `az ad sp delete --id <SP_ID>`
- [ ] Remove GitHub secret: Settings → Secrets → Delete `AZURE_CREDENTIALS`
- [ ] Revoke any API keys used

---

## Summary

✅ **GitHub Actions Workflow Created**: `.github/workflows/decommission-azure.yml`

**To run**:
1. Add `AZURE_CREDENTIALS` secret to GitHub
2. Go to Actions → Decommission Azure Resources
3. Run workflow (type `DELETE` to confirm)
4. Wait 1-2 minutes
5. Verify deletion

**Result**: Azure App Service deleted, $69/month savings

---

**Questions?** Check the workflow logs in GitHub Actions for detailed output.
