using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 🔗 Replace with your AWS API URL
const string AwsApiUrl = "https://abc123xyz.execute-api.us-east-1.amazonaws.com/presign";

app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Azure to AWS S3 Uploader</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 600px;
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
            margin-bottom: 30px;
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
        .info {
            background: #f0f4ff;
            padding: 15px;
            border-radius: 5px;
            margin-top: 20px;
            font-size: 14px;
            color: #555;
        }
    </style>
</head>
<body>
    <div class='container'>
        <h2>📤 Upload PDF to AWS S3</h2>
        <form action='/upload' method='post' enctype='multipart/form-data' class='upload-form'>
            <input type='file' name='file' accept='application/pdf' required />
            <button type='submit'>Upload to S3</button>
        </form>
        <div class='info'>
            ℹ️ This app runs on Azure and uploads PDFs to AWS S3 using presigned URLs.
        </div>
    </div>
</body>
</html>", "text/html"));

app.MapPost("/upload", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var file = form.Files["file"];
    if (file == null) return Results.BadRequest("No file uploaded.");

    var http = new HttpClient();
    var presignResponse = await http.GetAsync(AwsApiUrl);
    var json = await presignResponse.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<PresignResponse>(json);

    using var stream = file.OpenReadStream();
    var put = new HttpRequestMessage(HttpMethod.Put, data.url)
    {
        Content = new StreamContent(stream)
    };
    put.Content.Headers.ContentType =
        new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

    var resp = await http.SendAsync(put);
    if (!resp.IsSuccessStatusCode)
        return Results.BadRequest($"Upload failed: {resp.StatusCode}");

    return Results.Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>Upload Success</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 600px;
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
            text-align: center;
        }}
        .success {{
            color: #10b981;
            font-size: 48px;
            margin-bottom: 20px;
        }}
        h2 {{
            color: #333;
            margin-bottom: 20px;
        }}
        .key {{
            background: #f0f4ff;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            word-break: break-all;
            font-family: monospace;
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
    </style>
</head>
<body>
    <div class='container'>
        <div class='success'>✅</div>
        <h2>Upload Successful!</h2>
        <p>Your file has been uploaded to AWS S3</p>
        <div class='key'>Key: {data.key}</div>
        <a href='/'>← Upload Another File</a>
    </div>
</body>
</html>", "text/html");
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

record PresignResponse([property: JsonPropertyName("url")] string url,
                       [property: JsonPropertyName("key")] string key,
                       [property: JsonPropertyName("expiresIn")] int expiresIn);
