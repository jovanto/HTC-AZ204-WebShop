using System.IO;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
 
namespace ResizeFunction
{
    public class ImageResizeFunction
    {
        private readonly ILogger<ImageResizeFunction> _logger;
 
        public ImageResizeFunction(ILogger<ImageResizeFunction> logger)
        {
            _logger = logger;
        }
 
        [Function("ResizeImage")]
        public async Task Run(
            // Connection is set to an empty string because the AzureWebJobsStorage connection string is used by default
            [BlobTrigger("images/{name}", Connection = "STORAGE_ACCOUNT_CONNECTION")] Stream stream,
            string name)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
 
            await ResizeImageAsync(memoryStream, name);
 
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Size: {memoryStream.Length} Bytes");
        }
 
        private static async Task ResizeImageAsync(Stream stream, string name)
        {
            stream.Position = 0;
 
            var format = Image.DetectFormat(stream);
 
            if (format == null)
            {
                throw new InvalidOperationException("Unsupported image format.");
            }
 
            stream.Position = 0;
            using var image = await Image.LoadAsync(stream);
 
            image.Mutate(x => x.Resize(
                new ResizeOptions()
                {
                    Mode = ResizeMode.Pad,
                    Size = new Size(100, 100)
                }
            ));
 
            var newFileName = $"{Path.GetFileNameWithoutExtension(name)}_thumb{Path.GetExtension(name)}";
 
            var blobService = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var containerClient = blobService.GetBlobContainerClient("contosoimagesresized");
            var blobClient = containerClient.GetBlobClient(newFileName);
 
            IImageEncoder encoder = name.ToLower() switch
            {
                var ext when ext.EndsWith(".jpg") || ext.EndsWith(".jpeg") => new JpegEncoder(),
                var ext when ext.EndsWith(".bmp") => new BmpEncoder(),
                var ext when ext.EndsWith(".gif") => new GifEncoder(),
                _ => new PngEncoder(),
            };
 
            using var newStream = new MemoryStream();
            await image.SaveAsync(newStream, encoder);
            newStream.Position = 0;
 
            await blobClient.UploadAsync(newStream, true);
        }
 
    }
}