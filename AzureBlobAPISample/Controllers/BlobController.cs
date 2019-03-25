using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace AzureBlobAPISample.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        public BlobController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        //Read by Id
        [HttpGet]
        public async Task<IActionResult> Download(String fileName)
        {
            try
            {
                var result = await DownloadAssetAsync(fileName);
                return File(result.ToArray(), GetContentType(fileName));

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }


        }


        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var result = await UploadAssetAsync(file);

            return Ok(result);
        }


        #region Upload

        private async Task<IActionResult> UploadAssetAsync([FromForm]IFormFile asset)
        {
            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(_configuration.GetConnectionString("StorageAccount"), out storageAccount))
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("fileupload");
                await container.CreateIfNotExistsAsync();

                var blob = container.GetBlockBlobReference(asset.FileName);
                await blob.UploadFromStreamAsync(asset.OpenReadStream());
                return Ok(blob.Uri);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region Download

        private async Task<MemoryStream> DownloadAssetAsync(string documentId)
        {
            MemoryStream fileStream = new MemoryStream();

            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(_configuration.GetConnectionString("StorageAccount"), out storageAccount))
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("fileupload");
                await container.CreateIfNotExistsAsync();

                var blob = container.GetBlockBlobReference(documentId);
                await blob.DownloadToStreamAsync(fileStream);

            }
            return fileStream;

            //return StatusCode(StatusCodes.Status500InternalServerError);
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }

        #endregion


    }
}

