#!/bin/bash

# Azure Function App Deployment Script for Image Resize Function

# Configuration
FUNCTION_APP_NAME="resizeImageFunctionApplication"
RESOURCE_GROUP="RG-Hack"  # Update this with your resource group
SUBSCRIPTION_ID="1a7551e1-7de4-4d88-8715-40e11416ece3"  # Update this with your subscription ID

echo "🚀 Starting deployment to Azure Function App: $FUNCTION_APP_NAME"

# Build the project
echo "📦 Building the project..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

# Publish the project
echo "📤 Publishing the project..."
dotnet publish --configuration Release --output ./publish

if [ $? -ne 0 ]; then
    echo "❌ Publish failed!"
    exit 1
fi

# Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../deploy.zip .
cd ..

echo "🔧 Deploying to Azure Function App..."

# Deploy using Azure CLI
az functionapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $FUNCTION_APP_NAME \
    --src deploy.zip

if [ $? -eq 0 ]; then
    echo "✅ Deployment successful!"
    echo "🌐 Function App URL: https://$FUNCTION_APP_NAME.azurewebsites.net"
    echo "📊 You can monitor the function at: https://portal.azure.com"
else
    echo "❌ Deployment failed!"
    exit 1
fi

# Cleanup
rm deploy.zip
rm -rf publish

echo "🧹 Cleaned up temporary files"
echo "✨ Deployment completed!"