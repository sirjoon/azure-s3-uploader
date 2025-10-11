# Azure Deployment Guide

## 🎯 Step-by-Step Deployment to Azure App Service

### Method 1: Azure Portal (GUI)

#### Step 1: Create App Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"**
3. Search for **"Web App"** → Click **"Create"**

#### Step 2: Configure Basics

- **Subscription:** Select your subscription
- **Resource Group:** Create new → `s3-uploader-rg`
- **Name:** `my-s3-uploader` (must be globally unique)
- **Publish:** **Code**
- **Runtime stack:** **.NET 8 (LTS)**
- **Operating System:** **Linux**
- **Region:** Choose closest to you (e.g., `East US`)

#### Step 3: Configure App Service Plan

- **Linux Plan:** Create new → `s3-uploader-plan`
- **Sku and size:**
  - For testing: **F1 (Free)**
  - For production: **B1 (Basic)** - ~$13/month

#### Step 4: Review + Create

- Click **"Review + create"**
- Click **"Create"**
- Wait ~2 minutes for deployment

#### Step 5: Configure GitHub Deployment

1. Go to your App Service → **Deployment Center**
2. **Source:** Select **GitHub**
3. **Organization:** Select your GitHub account
4. **Repository:** Select `azure-s3-uploader`
5. **Branch:** Select `main`
6. Click **"Save"**

Azure will automatically:
- Create GitHub Actions workflow
- Build and deploy your app
- Configure continuous deployment

#### Step 6: Wait for Deployment

- Go to **Deployment Center** → **Logs**
- Wait for first deployment to complete (~3-5 minutes)
- Status should show **"Success (Active)"**

#### Step 7: Test Your App

- Go to **Overview** page
- Click on **URL** (e.g., `https://my-s3-uploader.azurewebsites.net`)
- You should see the upload form!

---

### Method 2: Azure CLI (Command Line)

```bash
# 1. Login to Azure
az login

# 2. Set variables
RESOURCE_GROUP="s3-uploader-rg"
APP_NAME="my-s3-uploader-$(date +%s)"  # Adds timestamp for uniqueness
LOCATION="eastus"
PLAN_NAME="s3-uploader-plan"
GITHUB_REPO="https://github.com/sirjoon/azure-s3-uploader"

# 3. Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# 4. Create App Service plan
az appservice plan create \
  --name $PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# 5. Create Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $PLAN_NAME \
  --runtime "DOTNET:8.0"

# 6. Configure deployment from GitHub
az webapp deployment source config \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --repo-url $GITHUB_REPO \
  --branch main \
  --manual-integration

# 7. Enable HTTPS only
az webapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true

# 8. Get the URL
echo "Your app is deployed at:"
az webapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query defaultHostName \
  --output tsv
```

---

### Method 3: GitHub Actions (Automated CI/CD)

This is automatically set up when you configure GitHub deployment in Azure Portal.

The workflow file is created at: `.github/workflows/main_your-app-name.yml`

**Manual Setup:**

1. Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Publish
      run: dotnet publish -c Release -o ./publish

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'your-app-name'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

2. Get Publish Profile:
   - Go to Azure Portal → Your App Service → **Download publish profile**

3. Add to GitHub Secrets:
   - Go to GitHub → Repository → **Settings** → **Secrets and variables** → **Actions**
   - Click **"New repository secret"**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: Paste publish profile content

---

## 🔧 Post-Deployment Configuration

### Update AWS API URL

After deployment, you need to update the AWS API endpoint:

#### Option 1: Update in Code
Edit `Program.cs` and redeploy:
```csharp
const string AwsApiUrl = "https://YOUR-ACTUAL-API.execute-api.us-east-1.amazonaws.com/presign";
```

#### Option 2: Use Environment Variables (Better)

```bash
# Set environment variable in Azure
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings AWS_API_URL="https://your-api.execute-api.us-east-1.amazonaws.com/presign"

# Then update Program.cs to read from environment:
var AwsApiUrl = Environment.GetEnvironmentVariable("AWS_API_URL")
    ?? "https://default-api.execute-api.us-east-1.amazonaws.com/presign";
```

---

## 🔍 Verify Deployment

### Check Application Status
```bash
# Get app status
az webapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query state \
  --output tsv

# Should return: Running
```

### View Logs
```bash
# Stream logs in real-time
az webapp log tail \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Download logs
az webapp log download \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --log-file logs.zip
```

### Test Endpoints
```bash
# Health check
curl https://$APP_NAME.azurewebsites.net/health

# Should return: {"status":"healthy","timestamp":"..."}
```

---

## 📊 Monitor Your App

### Enable Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app $APP_NAME-insights \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Link to Web App
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APP_NAME-insights \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey \
  --output tsv)

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

---

## 🧹 Cleanup Resources

### Delete Everything
```bash
# Delete resource group (deletes everything inside)
az group delete \
  --name $RESOURCE_GROUP \
  --yes \
  --no-wait
```

### Delete Just the App
```bash
# Keep resource group, delete only app
az webapp delete \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP
```

---

## 💡 Tips & Best Practices

1. **Use Environment Variables** for configuration
2. **Enable Always On** for production (prevents cold starts)
3. **Enable HTTPS Only** for security
4. **Set up Custom Domain** for professional URL
5. **Configure Auto-scaling** for high traffic
6. **Enable Diagnostic Logs** for debugging
7. **Use Deployment Slots** for staging/production

---

## 🚨 Common Issues

### Issue: App shows "Application Error"
```bash
# Check logs
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP

# Common fixes:
# 1. Wrong runtime stack → Change to .NET 8
# 2. Missing files → Redeploy from GitHub
# 3. Port binding issue → App should listen on PORT environment variable
```

### Issue: Deployment Fails
```bash
# Check deployment logs
az webapp deployment list-publishing-credentials \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Redeploy
az webapp deployment source sync \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP
```

---

## 📞 Need Help?

- Azure Support: https://azure.microsoft.com/support/
- GitHub Issues: https://github.com/sirjoon/azure-s3-uploader/issues
- Stack Overflow: Tag with `azure-app-service` and `.net`

---

**Deployment Complete! 🎉**

Your app should now be live at: `https://your-app-name.azurewebsites.net`
