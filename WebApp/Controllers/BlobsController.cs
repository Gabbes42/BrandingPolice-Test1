using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WebApp.Controllers
{
    public class BlobsController : Controller
    {
        private const string blobContainerName = "uploadppts";
        private const string messageQueueName = "analizequeue";
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=brandingpolice;AccountKey=B5pQ+hDOjOpktjMX3IFQGzm4r6uf8IN6m21yt/GQiw4OAsb7x5YWNxtJQuFxctFelCBubKvrpADW1X/j//iKgA==;EndpointSuffix=core.windows.net";
        public IActionResult Index()
        {
            return View();
        }

        private CloudBlobContainer GetCloudBlobContainer(string containerName)
        {
            // Retrieve storage account from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the container client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            return container;
        }

        private CloudQueue GetCloudQueue(string queueName)
        {
            // Retrieve storage account from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference(queueName);

            return queue;
        }


        [HttpPost("UploadFiles")]
        public async Task<IActionResult> Post(List<IFormFile> files, string searchString)
        {
            var uploadSuccess = false;

            // Retrieve a reference to a container.
            CloudBlobContainer container = GetCloudBlobContainer(blobContainerName);

            // Create the container if it doesn't already exist
            await container.CreateIfNotExistsAsync();

            // Retrieve a reference to a queue.
            CloudQueue queue = GetCloudQueue(messageQueueName);

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            foreach (var formFile in files)
            {
                if (formFile.Length <= 0)
                {
                    continue;
                }

                // Create a blob for each uploaded file
                CloudBlockBlob file = container.GetBlockBlobReference("ppt_" + formFile.FileName);

                // Create a blob for each result
                CloudBlockBlob result = container.GetBlockBlobReference("results_" + formFile.FileName +".txt");

                // Fill the result file
                await result.UploadTextAsync("Working on ppt_\"" + formFile.FileName+ "\"");

                // Upload the file
                using (var stream = formFile.OpenReadStream())
                {
                    await file.UploadFromStreamAsync(stream);
                    uploadSuccess = true;
                }

                // Build the string for the queue message
                String storageName = blobContainerName;
                String filePathPresentation = file.Uri.ToString();
                String filePathResult = result.Uri.ToString();
                String stringDelimiter = "|||";
                String completeStringForSending = "";
                completeStringForSending = storageName
                                         + stringDelimiter + filePathPresentation
                                         + stringDelimiter + filePathResult
                                         + stringDelimiter + searchString;
                
                // Append new message to queue
                CloudQueueMessage message = new CloudQueueMessage(completeStringForSending);
                await queue.AddMessageAsync(message);

                // Display URI 
                ViewData["resultUri"] = result.Uri.ToString();
            }

            if (!uploadSuccess)
                return View("UploadError");

            return View("UploadSuccess");

        }
    }
}