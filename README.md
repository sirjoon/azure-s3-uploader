# Azure to AWS S3 PDF Uploader

A minimal .NET 8 web application that runs on Azure App Service and uploads PDFs to AWS S3 using presigned URLs.

## 🚀 Quick Deploy to Azure

### Option 1: Deploy from GitHub (Recommended)

1. **Fork or use this repository**
2. **Go to Azure Portal** → Create **Web App**
3. **Configure:**
   - Runtime: **.NET 8**
   - Operating System: **Linux** or **Windows**
4. **Deployment:**
   - Source: **GitHub**
   - Repository: This repo
   - Branch: `main`
5. **Deploy!**

### Option 2: Azure CLI Deployment

```bash
# Login to Azure
az login

# Create resource group
az group create \
  --name s3-uploader-rg \
  --location eastus

# Create App Service Plan
az appservice plan create \
  --name s3-uploader-plan \
  --resource-group s3-uploader-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg \
  --plan s3-uploader-plan \
  --runtime "DOTNET|8.0"

# Deploy from GitHub
az webapp deployment source config \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg \
  --repo-url https://github.com/sirjoon/azure-s3-uploader \
  --branch main \
  --manual-integration
```

### Option 3: One-Click Deploy

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.WebSite)

---

## 📋 Prerequisites

### On AWS Side (Create First):
1. **AWS Lambda Function** that generates presigned URLs
2. **API Gateway** endpoint that triggers the Lambda
3. **S3 Bucket** for file storage

### On Azure Side:
1. **Azure Subscription**
2. **App Service** (created during deployment)

---

## 🔧 Configuration

### Update AWS API URL

Before deploying, update the AWS API endpoint in `Program.cs`:

```csharp
// 🔗 Replace with your AWS API URL
const string AwsApiUrl = "https://YOUR-API-ID.execute-api.us-east-1.amazonaws.com/presign";
```

### Environment Variables (Optional)

You can also configure via Azure App Service Environment Variables:

```bash
az webapp config appsettings set \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg \
  --settings AWS_API_URL="https://your-api.execute-api.us-east-1.amazonaws.com/presign"
```

Then update `Program.cs`:
```csharp
var AwsApiUrl = Environment.GetEnvironmentVariable("AWS_API_URL")
    ?? "https://abc123xyz.execute-api.us-east-1.amazonaws.com/presign";
```

---

## 🏗️ Architecture

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Browser   │────────▶│   Azure     │────────▶│   AWS API   │
│             │         │  App Service│         │   Gateway   │
└─────────────┘         └─────────────┘         └─────────────┘
                                                        │
                                                        ▼
                                                 ┌─────────────┐
                                                 │   Lambda    │
                                                 │  (Presign)  │
                                                 └─────────────┘
                                                        │
                                                        ▼
                        ┌─────────────┐         ┌─────────────┐
                        │   Browser   │────────▶│   AWS S3    │
                        │  (Upload)   │  PUT    │   Bucket    │
                        └─────────────┘         └─────────────┘
```

**Flow:**
1. User uploads PDF via Azure web app
2. Azure app requests presigned URL from AWS API Gateway
3. AWS Lambda generates presigned S3 URL
4. Azure app uploads file directly to S3 using presigned URL
5. Success confirmation returned to user

---

## 🧪 Testing Locally

### Prerequisites
- .NET 8 SDK installed
- AWS Lambda + API Gateway already deployed

### Run Locally
```bash
# Clone repository
git clone https://github.com/sirjoon/azure-s3-uploader.git
cd azure-s3-uploader

# Update AWS API URL in Program.cs

# Run application
dotnet run

# Open browser
open http://localhost:5000
```

---

## 📊 Endpoints

### `GET /`
- **Description:** Upload form UI
- **Response:** HTML page with file upload form

### `POST /upload`
- **Description:** Upload PDF file
- **Body:** `multipart/form-data` with `file` field
- **Response:** Success page or error message

### `GET /health`
- **Description:** Health check endpoint
- **Response:** JSON with status and timestamp

---

## 💰 Cost Estimate

### Azure Costs:
- **App Service (B1 Basic):** ~$13/month
- **App Service (F1 Free):** $0/month (limited)

### AWS Costs (Not Included):
- **Lambda:** ~$0.20 per million requests
- **API Gateway:** ~$3.50 per million requests
- **S3 Storage:** ~$0.023 per GB/month
- **S3 PUT Requests:** ~$0.005 per 1,000 requests

**Total for 1,000 uploads/month:** ~$13-15/month

---

## 🔐 Security Best Practices

### Current Implementation:
✅ Uses presigned URLs (no AWS credentials in Azure)
✅ File type validation (PDF only)
✅ HTTPS enforced by Azure App Service

### Recommended Additions:
- [ ] Add authentication (Azure AD, Auth0)
- [ ] Implement rate limiting
- [ ] Add file size limits
- [ ] Validate file content (not just extension)
- [ ] Add CORS configuration
- [ ] Implement logging (Application Insights)

---

## 🐛 Troubleshooting

### Issue: 404 Not Found After Deployment
**Solution:**
```bash
# Check deployment status
az webapp deployment source show \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg

# View logs
az webapp log tail \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg
```

### Issue: Upload Fails with 403 Forbidden
**Cause:** Presigned URL expired or invalid AWS API endpoint
**Solution:**
1. Verify AWS API Gateway URL is correct
2. Check Lambda function is generating valid presigned URLs
3. Verify S3 bucket permissions

### Issue: File Upload Hangs
**Cause:** File too large or network timeout
**Solution:**
```bash
# Increase timeout in Azure App Service
az webapp config set \
  --name my-s3-uploader \
  --resource-group s3-uploader-rg \
  --web-sockets-enabled true \
  --always-on true
```

---

## 📦 Project Structure

```
azure-s3-uploader/
├── Program.cs                      # Main application code
├── azure-s3-uploader.csproj       # .NET project file
├── appsettings.json               # Configuration
├── .gitignore                     # Git ignore rules
└── README.md                      # This file
```

---

## 🚀 Deployment Steps Summary

1. **Create AWS Infrastructure:**
   - Lambda function for presigned URLs
   - API Gateway endpoint
   - S3 bucket

2. **Update Code:**
   - Replace `AwsApiUrl` with your API Gateway URL

3. **Deploy to Azure:**
   - Create App Service
   - Configure GitHub deployment
   - Deploy!

4. **Test:**
   - Navigate to `https://your-app.azurewebsites.net`
   - Upload a PDF
   - Verify upload in S3

---

## 🔗 Related Resources

- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [AWS S3 Presigned URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html)
- [.NET Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

---

## 📄 License

MIT License - Free to use and modify

---

## 👤 Author

**Siru**
Created: October 2025
Version: 1.0.0

---

## 🙏 Credits

- Built with **.NET 8 Minimal APIs**
- Hosted on **Azure App Service**
- Integrates with **AWS S3**
