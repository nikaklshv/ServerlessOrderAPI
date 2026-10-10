using Amazon.S3;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

// Підключаємо MVC та AWS
builder.Services.AddControllersWithViews();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var accessKey = builder.Configuration["AWS:AccessKey"];
var secretKey = builder.Configuration["AWS:SecretKey"];
var region = builder.Configuration["AWS:Region"];

// Якщо ключі є в appsettings.json (локальний запуск)
if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
    var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);

    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, regionEndpoint));
    builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, regionEndpoint));
}
// Якщо ключів немає (запуск в хмарі AWS Lambda через IAM Role)
else
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddAWSService<IAmazonSQS>();
}

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();