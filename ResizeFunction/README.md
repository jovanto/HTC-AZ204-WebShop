# Image Resize Azure Function

This Azure Function automatically resizes images when they are uploaded to a blob storage container. It creates multiple sizes (medium, thumbnail, small) for each uploaded image.

## Features

- **Blob Trigger**: Automatically processes images when uploaded to the `images` container
- **Multiple Sizes**: Creates 3 different sizes:
  - Medium: 800x600 max
  - Thumbnail: 300x300 max  
  - Small: 150x150 max
- **Aspect Ratio Preservation**: Maintains original image proportions
- **Smart Processing**: Skips non-image files and already processed images
- **Metadata**: Adds processing metadata to resized images

## Prerequisites

1. Azure Storage Account with blob containers:
   - `images` (source container)
   - `images-resized` (destination container - created automatically)
2. Azure Function App: `resizeImageFunctionApp`
3. .NET 8.0 SDK

## Configuration

### Environment Variables (for Azure Function App)

Set these application settings in your Azure Function App:

```
STORAGE_ACCOUNT_CONNECTION=<your-storage-account-connection-string>
SOURCE_CONTAINER=images
DESTINATION_CONTAINER=images-resized
```

### Local Development

Update `local.settings.json` with your storage account details:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "STORAGE_ACCOUNT_CONNECTION": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=yourkey;EndpointSuffix=core.windows.net",
    "SOURCE_CONTAINER": "images",
    "DESTINATION_CONTAINER": "images-resized"
  }
}
```

## Deployment

### Option 1: Using the deployment script

1. Update the deployment script with your Azure details:
   ```bash
   # Edit deploy.sh
   RESOURCE_GROUP="your-resource-group"
   SUBSCRIPTION_ID="your-subscription-id"
   ```

2. Run the deployment:
   ```bash
   cd ResizeFunction
   ./deploy.sh
   ```

### Option 2: Manual deployment

1. Build and publish:
   ```bash
   dotnet build --configuration Release
   dotnet publish --configuration Release --output ./publish
   ```

2. Deploy using Azure CLI:
   ```bash
   cd publish
   zip -r ../deploy.zip .
   cd ..
   
   az functionapp deployment source config-zip \
     --resource-group your-resource-group \
     --name resizeImageFunctionApp \
     --src deploy.zip
   ```

### Option 3: Using Visual Studio Code

1. Install the Azure Functions extension
2. Right-click on the ResizeFunction folder
3. Select "Deploy to Function App"
4. Choose your existing function app: `resizeImageFunctionApp`

## How It Works

1. **Upload Trigger**: When an image is uploaded to the `images` container, the function is triggered
2. **Image Processing**: The function loads the image and creates resized versions
3. **Storage**: Resized images are saved to the `images-resized` container with descriptive names:
   - `original-image_medium.jpg`
   - `original-image_thumbnail.jpg`
   - `original-image_small.jpg`
4. **Metadata**: Each resized image includes metadata about the processing

## Integration with Your WebApp

To use the resized images in your Contoso.WebApp, you can modify the `Product.cshtml.cs` to reference the appropriate sized images:

```csharp
// Example: Get thumbnail URL
var thumbnailUrl = $"https://yourstorageaccount.blob.core.windows.net/images-resized/{productImageName}_thumbnail.jpg";

// Example: Get medium size URL  
var mediumUrl = $"https://yourstorageaccount.blob.core.windows.net/images-resized/{productImageName}_medium.jpg";
```

## Monitoring

- View function logs in the Azure Portal
- Monitor execution metrics and errors
- Set up Application Insights for detailed telemetry

## Function App URL

Your function app is available at:
https://resizeimagefunctionapplication-gnh8dmbndbe5hnbc.australiasoutheast-01.azurewebsites.net

## Supported Image Formats

- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- BMP (.bmp)
- TIFF (.tiff)
- WebP (.webp)