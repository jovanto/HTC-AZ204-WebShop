using Contoso.WebApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Azure.Storage.Blobs;
using Azure.Extensions.AspNetCore.DataProtection.Blobs;





 

var builder = WebApplication.CreateBuilder(args);
// Prefer connection string-based access for blob persistence (explicit credentials)
var dpConnectionString = builder.Configuration["DataProtection:BlobConnectionString"]; // e.g. DefaultEndpointsProtocol=...;AccountKey=...;
var dpContainerName   = builder.Configuration["DataProtection:ContainerName"] ?? "dataprotection";
var dpBlobName        = builder.Configuration["DataProtection:BlobName"] ?? "keys.xml";

if (!string.IsNullOrWhiteSpace(dpConnectionString))
{
    var containerClient = new BlobContainerClient(dpConnectionString, dpContainerName);
    // Ensure container exists (optional; remove if handled externally)
    try { containerClient.CreateIfNotExists(); } catch { /* ignore */ }
    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(containerClient, dpBlobName);
}
else
{
    // Fallback to URI+credential or ephemeral if no connection string provided
    var blobUriValue = builder.Configuration["DataProtection:BlobUri"]; // legacy config
    if (!string.IsNullOrWhiteSpace(blobUriValue))
    {
        var blobUri = new Uri(blobUriValue);
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(blobUri, new DefaultAzureCredential());
    }
    else
    {
        builder.Services.AddDataProtection();
    }
}
// Add services to the container.
builder.Services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Home/Home", "/");
                });   
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<AuthHandler>();
builder.Services.AddTransient<LoggingHandler>();

builder.Services.AddHttpClient<IContosoAPI>(client => {
    client.BaseAddress = new Uri(builder.Configuration["BackendUrl"]);
})
// .AddHttpMessageHandler(() => new LoggingHandler())
.AddHttpMessageHandler<AuthHandler>()
.AddTypedClient(client => RestService.For<IContosoAPI>(client));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
