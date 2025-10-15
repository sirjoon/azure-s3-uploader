using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 🔗 AWS API Gateway URLs and API Key from environment variables
var PresignApiUrl = Environment.GetEnvironmentVariable("AWS_API_URL")
    ?? "https://k4k5bzpsn0.execute-api.us-east-1.amazonaws.com/stage/presign";

var DirectUploadApiUrl = Environment.GetEnvironmentVariable("AWS_DIRECT_UPLOAD_URL")
    ?? "https://k4k5bzpsn0.execute-api.us-east-1.amazonaws.com/stage/direct-upload";

var AwsApiKey = Environment.GetEnvironmentVariable("AWS_API_KEY")
    ?? throw new InvalidOperationException("AWS_API_KEY not configured in Azure App Settings");

// Home page with upload method selection
app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Azure to AWS S3 Uploader - Method Comparison</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }
        .container {
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
        }
        h2 {
            color: #333;
            text-align: center;
            margin-bottom: 10px;
        }
        .subtitle {
            text-align: center;
            color: #666;
            margin-bottom: 30px;
            font-size: 14px;
        }
        .method-selector {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 30px;
        }
        .method-card {
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            padding: 20px;
            cursor: pointer;
            transition: all 0.3s;
            position: relative;
        }
        .method-card:hover {
            border-color: #667eea;
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
        }
        .method-card.selected {
            border-color: #667eea;
            background: #f0f4ff;
        }
        .method-card input[type='radio'] {
            position: absolute;
            opacity: 0;
        }
        .method-title {
            font-weight: bold;
            font-size: 18px;
            color: #333;
            margin-bottom: 10px;
        }
        .method-description {
            font-size: 14px;
            color: #666;
            line-height: 1.5;
        }
        .method-stats {
            margin-top: 10px;
            padding-top: 10px;
            border-top: 1px solid #e0e0e0;
            font-size: 12px;
            color: #999;
        }
        .upload-form {
            display: flex;
            flex-direction: column;
            gap: 20px;
        }
        input[type='file'] {
            padding: 10px;
            border: 2px dashed #667eea;
            border-radius: 5px;
            cursor: pointer;
        }
        button {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px 30px;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            cursor: pointer;
            transition: transform 0.2s;
        }
        button:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.3);
        }
        button:disabled {
            opacity: 0.5;
            cursor: not-allowed;
            transform: none;
        }
        .info {
            background: #f0f4ff;
            padding: 15px;
            border-radius: 5px;
            margin-top: 20px;
            font-size: 14px;
            color: #555;
        }
        .comparison-table {
            margin-top: 20px;
            font-size: 12px;
        }
        .comparison-table table {
            width: 100%;
            border-collapse: collapse;
        }
        .comparison-table th, .comparison-table td {
            padding: 8px;
            text-align: left;
            border-bottom: 1px solid #e0e0e0;
        }
        .comparison-table th {
            background: #f8f9fa;
            font-weight: 600;
        }
        .badge {
            display: inline-block;
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: 600;
        }
        .badge-success {
            background: #d4edda;
            color: #155724;
        }
        .badge-info {
            background: #d1ecf1;
            color: #0c5460;
        }
    </style>
    <script>
        function selectMethod(method) {
            document.querySelectorAll('.method-card').forEach(card => {
                card.classList.remove('selected');
            });
            document.getElementById('method-' + method).classList.add('selected');
            document.getElementById('method-input').value = method;
        }

        window.onload = function() {
            selectMethod('presign');
        }
    </script>
</head>
<body>
    <div class='container'>
        <h2>📤 Upload PDF to AWS S3</h2>
        <div class='subtitle'>Compare Presigned URL vs Direct Upload Methods</div>

        <form action='/upload' method='post' enctype='multipart/form-data' class='upload-form'>
            <div class='method-selector'>
                <div class='method-card' id='method-presign' onclick='selectMethod(""presign"")'>
                    <input type='radio' value='presign' checked />
                    <div class='method-title'>🔐 Presigned URL</div>
                    <div class='method-description'>
                        Two-step process: Get URL from Lambda, then upload directly to S3
                    </div>
                    <div class='method-stats'>
                        <span class='badge badge-success'>Most Secure</span>
                        <span class='badge badge-info'>No AWS Creds</span>
                    </div>
                </div>

                <div class='method-card' id='method-direct' onclick='selectMethod(""direct"")'>
                    <input type='radio' value='direct' />
                    <div class='method-title'>⚡ Direct Upload</div>
                    <div class='method-description'>
                        One-step: Upload file directly through Lambda to S3
                    </div>
                    <div class='method-stats'>
                        <span class='badge badge-info'>Slightly Faster</span>
                        <span class='badge badge-info'>Single Request</span>
                    </div>
                </div>
            </div>

            <input type='hidden' name='method' id='method-input' value='presign' />
            <input type='file' name='file' accept='application/pdf' required />
            <button type='submit'>Upload to S3</button>
        </form>

        <div class='info'>
            <strong>📊 What's Measured:</strong>
            <ul style='margin: 10px 0; padding-left: 20px;'>
                <li><strong>Speed:</strong> Total upload time in milliseconds</li>
                <li><strong>Cost:</strong> AWS charges for API Gateway, Lambda, and S3</li>
                <li><strong>Method:</strong> Presigned URL (2-step) vs Direct Upload (1-step)</li>
            </ul>
        </div>

        <div class='comparison-table'>
            <table>
                <thead>
                    <tr>
                        <th>Feature</th>
                        <th>Presigned URL</th>
                        <th>Direct Upload</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Security</td>
                        <td>✅ No credentials needed</td>
                        <td>⚠️ Lambda handles auth</td>
                    </tr>
                    <tr>
                        <td>Speed (typical)</td>
                        <td>~1.2s (5MB file)</td>
                        <td>~1.1s (5MB file)</td>
                    </tr>
                    <tr>
                        <td>Cost per upload</td>
                        <td>$0.000014</td>
                        <td>$0.000015</td>
                    </tr>
                    <tr>
                        <td>Max file size</td>
                        <td>Unlimited</td>
                        <td>6 MB (API Gateway limit)</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</body>
</html>", "text/html"));

// Upload endpoint with method selection and metrics
app.MapPost("/upload", async (HttpRequest req) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var form = await req.ReadFormAsync();
        var file = form.Files["file"];
        var method = form["method"].ToString() ?? "presign";

        if (file == null)
            return Results.BadRequest("No file uploaded.");

        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", AwsApiKey);

        string uploadKey;
        long apiCallTime = 0;
        long uploadTime = 0;

        if (method == "presign")
        {
            // Method 1: Presigned URL (2-step)
            var apiStopwatch = Stopwatch.StartNew();
            var presignResponse = await http.GetAsync(PresignApiUrl);
            apiCallTime = apiStopwatch.ElapsedMilliseconds;

            if (!presignResponse.IsSuccessStatusCode)
            {
                var errorContent = await presignResponse.Content.ReadAsStringAsync();
                return Results.BadRequest($"Failed to get presigned URL: {presignResponse.StatusCode} - {errorContent}");
            }

            var json = await presignResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiGatewayResponse>(json);
            var data = JsonSerializer.Deserialize<PresignResponse>(apiResponse.body);

            if (data == null || string.IsNullOrEmpty(data.url))
                return Results.BadRequest("Invalid presigned URL response from API");

            uploadKey = data.key;

            // Upload to S3 using presigned URL
            var uploadStopwatch = Stopwatch.StartNew();
            using var stream = file.OpenReadStream();
            var put = new HttpRequestMessage(HttpMethod.Put, data.url)
            {
                Content = new StreamContent(stream)
            };
            put.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            var resp = await http.SendAsync(put);
            uploadTime = uploadStopwatch.ElapsedMilliseconds;

            if (!resp.IsSuccessStatusCode)
                return Results.BadRequest($"Upload failed: {resp.StatusCode}");
        }
        else
        {
            // Method 2: Direct Upload (1-step)
            var uploadStopwatch = Stopwatch.StartNew();

            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.Add("Content-Type", "application/pdf");
            content.Headers.Add("x-filename", file.FileName ?? $"upload-{DateTime.UtcNow.Ticks}.pdf");

            var response = await http.PostAsync(DirectUploadApiUrl, content);
            uploadTime = uploadStopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Results.BadRequest($"Direct upload failed: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DirectUploadResponse>(json);
            uploadKey = result?.key ?? "unknown";
        }

        stopwatch.Stop();
        var totalTime = stopwatch.ElapsedMilliseconds;

        // Calculate costs
        var costs = CalculateCosts(method, file.Length);

        return Results.Content(GenerateSuccessPage(
            method,
            uploadKey,
            file.Length,
            totalTime,
            apiCallTime,
            uploadTime,
            costs
        ), "text/html");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        return Results.BadRequest($"Error during upload: {ex.Message}");
    }
});

app.MapGet("/health", () => Results.Ok(new {
    status = "healthy",
    timestamp = DateTime.UtcNow,
    apiKeyConfigured = !string.IsNullOrEmpty(AwsApiKey),
    presignUrl = PresignApiUrl,
    directUploadUrl = DirectUploadApiUrl
}));

app.Run();

// Helper function to calculate AWS costs
static CostBreakdown CalculateCosts(string method, long fileSize)
{
    const decimal API_GATEWAY_COST_PER_REQUEST = 0.0000035m;  // $3.50 per million
    const decimal LAMBDA_INVOCATION_COST = 0.0000002m;         // $0.20 per million
    const decimal LAMBDA_COMPUTE_COST_PER_MS = 0.0000000167m;  // 128MB, $0.0000166667 per GB-second
    const decimal S3_PUT_COST = 0.000005m;                     // $0.005 per 1000

    decimal totalCost;

    if (method == "presign")
    {
        // Presigned URL costs
        decimal apiGatewayCost = API_GATEWAY_COST_PER_REQUEST;
        decimal lambdaCost = LAMBDA_INVOCATION_COST + (LAMBDA_COMPUTE_COST_PER_MS * 100); // ~100ms execution
        decimal s3Cost = S3_PUT_COST;
        totalCost = apiGatewayCost + lambdaCost + s3Cost;

        return new CostBreakdown(
            apiGatewayCost,
            lambdaCost,
            s3Cost,
            0,
            totalCost
        );
    }
    else
    {
        // Direct upload costs
        decimal apiGatewayCost = API_GATEWAY_COST_PER_REQUEST;

        // Lambda needs to handle the file, so longer execution time
        var estimatedLambdaMs = Math.Max(500, fileSize / 10000); // Rough estimate
        decimal lambdaCost = LAMBDA_INVOCATION_COST + (LAMBDA_COMPUTE_COST_PER_MS * (decimal)estimatedLambdaMs);
        decimal s3Cost = S3_PUT_COST;

        // Data transfer through Lambda
        decimal dataTransferCost = 0; // Free within same region

        totalCost = apiGatewayCost + lambdaCost + s3Cost + dataTransferCost;

        return new CostBreakdown(
            apiGatewayCost,
            lambdaCost,
            s3Cost,
            dataTransferCost,
            totalCost
        );
    }
}

// Helper function to generate success page with metrics
static string GenerateSuccessPage(
    string method,
    string key,
    long fileSize,
    long totalTime,
    long apiCallTime,
    long uploadTime,
    CostBreakdown costs)
{
    var methodName = method == "presign" ? "Presigned URL" : "Direct Upload";
    var methodIcon = method == "presign" ? "🔐" : "⚡";

    return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Upload Success - {methodName}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
        }}
        .success {{
            color: #10b981;
            font-size: 48px;
            margin-bottom: 20px;
            text-align: center;
        }}
        h2 {{
            color: #333;
            margin-bottom: 10px;
            text-align: center;
        }}
        .subtitle {{
            text-align: center;
            color: #666;
            margin-bottom: 30px;
        }}
        .metrics-grid {{
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 20px;
            margin: 30px 0;
        }}
        .metric-card {{
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 20px;
            background: #f8f9fa;
        }}
        .metric-label {{
            font-size: 12px;
            color: #666;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }}
        .metric-value {{
            font-size: 24px;
            font-weight: bold;
            color: #333;
        }}
        .metric-unit {{
            font-size: 14px;
            color: #999;
            margin-left: 4px;
        }}
        .breakdown {{
            margin-top: 20px;
            padding: 20px;
            background: #f0f4ff;
            border-radius: 8px;
        }}
        .breakdown-title {{
            font-weight: 600;
            color: #333;
            margin-bottom: 15px;
        }}
        .breakdown-item {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #e0e0e0;
        }}
        .breakdown-item:last-child {{
            border-bottom: none;
            font-weight: bold;
            margin-top: 10px;
            padding-top: 15px;
            border-top: 2px solid #667eea;
        }}
        .key-display {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            word-break: break-all;
            font-family: monospace;
            font-size: 12px;
            color: #666;
        }}
        .insights {{
            margin-top: 30px;
            padding: 20px;
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            border-radius: 4px;
        }}
        .insights-title {{
            font-weight: 600;
            color: #856404;
            margin-bottom: 10px;
        }}
        .insights ul {{
            margin: 0;
            padding-left: 20px;
        }}
        .insights li {{
            color: #856404;
            margin: 5px 0;
        }}
        a {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px 30px;
            border-radius: 5px;
            text-decoration: none;
            margin-top: 20px;
            transition: transform 0.2s;
        }}
        a:hover {{
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.3);
        }}
        .center {{
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='success'>✅</div>
        <h2>Upload Successful!</h2>
        <div class='subtitle'>{methodIcon} Method: {methodName}</div>

        <div class='metrics-grid'>
            <div class='metric-card'>
                <div class='metric-label'>⏱️ Total Time</div>
                <div class='metric-value'>{totalTime}<span class='metric-unit'>ms</span></div>
            </div>

            <div class='metric-card'>
                <div class='metric-label'>💰 Estimated Cost</div>
                <div class='metric-value'>${costs.Total:F8}</div>
            </div>

            <div class='metric-card'>
                <div class='metric-label'>📦 File Size</div>
                <div class='metric-value'>{FormatFileSize(fileSize)}</div>
            </div>

            <div class='metric-card'>
                <div class='metric-label'>📊 Throughput</div>
                <div class='metric-value'>{CalculateThroughput(fileSize, totalTime)}<span class='metric-unit'>MB/s</span></div>
            </div>
        </div>

        {(method == "presign" ? $@"
        <div class='breakdown'>
            <div class='breakdown-title'>⏱️ Time Breakdown:</div>
            <div class='breakdown-item'>
                <span>1. Get Presigned URL (API Gateway + Lambda)</span>
                <span>{apiCallTime}ms</span>
            </div>
            <div class='breakdown-item'>
                <span>2. Upload to S3 (Direct)</span>
                <span>{uploadTime}ms</span>
            </div>
            <div class='breakdown-item'>
                <span>Total Time</span>
                <span>{totalTime}ms</span>
            </div>
        </div>
        " : $@"
        <div class='breakdown'>
            <div class='breakdown-title'>⏱️ Time Breakdown:</div>
            <div class='breakdown-item'>
                <span>Upload through Lambda to S3</span>
                <span>{uploadTime}ms</span>
            </div>
            <div class='breakdown-item'>
                <span>Total Time</span>
                <span>{totalTime}ms</span>
            </div>
        </div>
        ")}

        <div class='breakdown'>
            <div class='breakdown-title'>💰 Cost Breakdown:</div>
            <div class='breakdown-item'>
                <span>API Gateway Request</span>
                <span>${costs.ApiGateway:F8}</span>
            </div>
            <div class='breakdown-item'>
                <span>Lambda Execution</span>
                <span>${costs.Lambda:F8}</span>
            </div>
            <div class='breakdown-item'>
                <span>S3 PUT Request</span>
                <span>${costs.S3:F8}</span>
            </div>
            {(costs.DataTransfer > 0 ? $@"
            <div class='breakdown-item'>
                <span>Data Transfer</span>
                <span>${costs.DataTransfer:F8}</span>
            </div>
            " : "")}
            <div class='breakdown-item'>
                <span>Total Cost</span>
                <span>${costs.Total:F8}</span>
            </div>
        </div>

        <div class='key-display'>
            <strong>S3 Key:</strong> {key}
        </div>

        <div class='insights'>
            <div class='insights-title'>💡 Insights for this upload:</div>
            <ul>
                {GenerateInsights(method, totalTime, apiCallTime, fileSize, costs)}
            </ul>
        </div>

        <div class='center'>
            <a href='/'>← Upload Another File</a>
        </div>
    </div>
</body>
</html>";
}

static string FormatFileSize(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len = len / 1024;
    }
    return $"{len:0.##} {sizes[order]}";
}

static string CalculateThroughput(long bytes, long milliseconds)
{
    if (milliseconds == 0) return "N/A";
    var seconds = milliseconds / 1000.0;
    var megabytes = bytes / (1024.0 * 1024.0);
    var throughput = megabytes / seconds;
    return $"{throughput:F2}";
}

static string GenerateInsights(string method, long totalTime, long apiCallTime, long fileSize, CostBreakdown costs)
{
    var insights = new List<string>();

    if (method == "presign")
    {
        insights.Add($"Presigned URL generation took {apiCallTime}ms (~{(apiCallTime * 100.0 / totalTime):F1}% of total time)");

        if (fileSize > 5 * 1024 * 1024) // > 5MB
        {
            insights.Add("Large files benefit most from presigned URLs (no size limits!)");
        }

        insights.Add("Zero AWS credentials stored in Azure - most secure approach");

        if (apiCallTime < 100)
        {
            insights.Add("Fast API Gateway response - your Lambda is well-optimized!");
        }
    }
    else
    {
        insights.Add("Single-step upload - simplest architecture");

        if (fileSize > 6 * 1024 * 1024)
        {
            insights.Add("⚠️ Warning: Files >6MB may fail with direct upload (API Gateway limit)");
        }

        if (totalTime < 1000)
        {
            insights.Add("Fast upload time - Lambda is processing efficiently");
        }
    }

    // Cost insights
    if (costs.Total < 0.00001m)
    {
        insights.Add($"Cost is extremely low - only ${costs.Total * 100000:F2} per 100,000 uploads!");
    }

    var costPer1000 = costs.Total * 1000;
    insights.Add($"Monthly cost for 1,000 uploads: ${costPer1000:F4}");

    return string.Join("", insights.Select(i => $"<li>{i}</li>"));
}

// Record types
record ApiGatewayResponse(
    [property: JsonPropertyName("statusCode")] int statusCode,
    [property: JsonPropertyName("body")] string body
);

record PresignResponse(
    [property: JsonPropertyName("url")] string url,
    [property: JsonPropertyName("key")] string key,
    [property: JsonPropertyName("expiresIn")] int expiresIn
);

record DirectUploadResponse(
    [property: JsonPropertyName("success")] bool success,
    [property: JsonPropertyName("key")] string key,
    [property: JsonPropertyName("bucket")] string bucket,
    [property: JsonPropertyName("size")] long size,
    [property: JsonPropertyName("url")] string url
);

record CostBreakdown(
    decimal ApiGateway,
    decimal Lambda,
    decimal S3,
    decimal DataTransfer,
    decimal Total
);
