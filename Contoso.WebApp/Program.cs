using Contoso.WebApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Azure.Storage.Blobs;
using Azure.Extensions.AspNetCore.DataProtection.Blobs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;





 

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Home/Home", "/");
                });  
// Prefer connection string-based access for blob persistence (explicit credentials)
var dpConnectionString = builder.Configuration["DataProtection:BlobConnectionString"]; // e.g. DefaultEndpointsProtocol=...;AccountKey=...;
var dpContainerName   = builder.Configuration["DataProtection:ContainerName"] ?? "dataprotection";
var dpBlobName        = builder.Configuration["DataProtection:BlobName"] ?? "keys.xml";

if (!string.IsNullOrWhiteSpace(dpConnectionString))
{
    var containerClient = new BlobContainerClient(dpConnectionString, dpContainerName);
    // Ensure container exists (optional; remove if handled externally)
    try { containerClient.CreateIfNotExists(); } catch { /* ignore */ }
    var blobClient = containerClient.GetBlobClient(dpBlobName);
    builder.Services.AddDataProtection().PersistKeysToAzureBlobStorage(blobClient);
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

// Add Microsoft Entra ID (Azure AD) authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Require authentication by default (pages without [AllowAnonymous] will trigger sign-in)
// builder.Services.AddAuthorization(options =>
// {
//     options.FallbackPolicy = options.DefaultPolicy;
// });

// Add services to the container.
builder.Services.AddRazorPages()
                .AddMicrosoftIdentityUI()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Home", "/");
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

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // Use HTTP for local development
    options.RequireHttpsMetadata = false;
 
    // Save tokens for later use
    options.SaveTokens = true;
 
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.CorrelationCookie.Path = "/";
    options.CorrelationCookie.HttpOnly = true;
 
    options.NonceCookie.SameSite = SameSiteMode.Lax;
    options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.NonceCookie.Path = "/";
    options.NonceCookie.HttpOnly = true;
 
    // Set response type
    options.ResponseType = "code";
 
    // Use query response mode instead of form_post to avoid cookie issues
    options.ResponseMode = "query";
 
    // Configure scopes
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
 
    // Add detailed event logging for debugging
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            Console.WriteLine($"🔄 Redirecting to identity provider: {context.ProtocolMessage.IssuerAddress}");
            return Task.CompletedTask;
        },
        OnAuthorizationCodeReceived = context =>
        {
            Console.WriteLine("✅ Authorization code received successfully");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ Authentication failed: {context.Exception?.Message}");
            context.Response.Redirect("/");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

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

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.Run();
