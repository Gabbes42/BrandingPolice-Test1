using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WebJob
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("analizequeue")] string message, ILogger logger)
        {
            try
            {
                logger.LogInformation(message);

                JobMessage receivedJobMessage = JobMessage.JobStringToJobMessage(message);
                string storageName = receivedJobMessage.StorageName;
                string powerPointFileUrl = receivedJobMessage.FilePathPresentation;
                string resultTextFileUrl = receivedJobMessage.FilePathResult;
                string searchString = receivedJobMessage.SearchString;

                // Ausgabe der Elemente des empfangenen JobMessage-Objekts
                logger.LogInformation("Received / Readed Elements: \n" +
                                        "Storage-Name: " + receivedJobMessage.StorageName + "\n" +
                                        "PowerPoint-Filename: " + receivedJobMessage.FilePathPresentation + "\n" +
                                        "ResultText-Filename: " + receivedJobMessage.FilePathResult + "\n" +
                                        "Search-String: " + receivedJobMessage.SearchString + "\n");

                // Get parameters from App.config
                var config = new JobHostConfiguration();
                if (config.IsDevelopment)
                {
                    config.UseDevelopmentSettings();
                }

                // Get connectionString for storage and get access to storageAccount
                string storageConnectionString = config.StorageConnectionString;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference(storageName);

                // Retrieve reference to the blobs for the PowerPoint- and result-text-file.
                //CloudBlob powerPointBlob = container.GetBlobReference(powerPointFileName);
                //CloudBlob resultTextBlob = container.GetBlobReference(resultTextFileName);
                CloudBlockBlob powerPointBlockBlob = new CloudBlockBlob(new Uri(powerPointFileUrl), storageAccount.Credentials);

                string resultName = "results_" + powerPointBlockBlob.Name.Substring(4) + ".txt";
                CloudBlockBlob resultTextblockBlob = container.GetBlockBlobReference(resultName);

                if (!powerPointBlockBlob.Exists())
                    logger.LogWarning("The Blob " + powerPointBlockBlob.Name + " don't exists. Aborded.");

                // Download the Powerpoint
                //MemoryStream cachedPowerPoint = new MemoryStream();
                //powerPointBlockBlob.DownloadToStreamAsync(cachedPowerPoint);

                using (var memoryStream = new MemoryStream())
                {
                    powerPointBlockBlob.DownloadToStream(memoryStream);

                    // Count pages of the PowerPoint-file
                    int numberOfSlides = CountSlides(memoryStream);
                    logger.LogInformation("Found " + numberOfSlides + " slides in " + powerPointBlockBlob.Name);

                    string result = "Search-String: " + searchString + Environment.NewLine + "Number of Slides: " + numberOfSlides;
                    int foundCounter;
                    for (int i = 0; i < numberOfSlides; i++)
                    {
                        GetSlideIdAndText(out foundCounter, searchString, memoryStream, i);
                        if (foundCounter > 0)
                        {
                            System.Console.WriteLine("Slide #{0}: {1} times found", i + 1, foundCounter);
                            string appendText = Environment.NewLine + "Slide #" + (i + 1) + ": " + foundCounter + " times found";
                            result = result + appendText;
                        }
                    }

                    
                    resultTextblockBlob.UploadText(result);
                    logger.LogInformation("Finished with:" + Environment.NewLine + result);
                    //powerPointBlockBlob.DeleteIfExists();
                }

            }
            catch (Exception ex)
            {
                logger.LogError("" + ex);
            }

        }

        // Count the slides in the presentation.
        public static int CountSlides(Stream presentationFile)
        {
            // Open the presentation as read-only.
            using (PresentationDocument presentationDocument = PresentationDocument.Open(presentationFile, false))
            {
                // Check for a null document object.
                if (presentationDocument == null)
                {
                    throw new ArgumentNullException("presentationDocument");
                }

                int slidesCount = 0;

                // Get the presentation part of document.
                PresentationPart presentationPart = presentationDocument.PresentationPart;

                // Verify that the presentation part and presentation exist.
                if (presentationPart != null && presentationPart.Presentation != null)
                {
                    // Get the Presentation object from the presentation part.
                    Presentation presentation = presentationPart.Presentation;

                    // Get the slide count from the SlideParts.
                    var slideIds = presentation.SlideIdList.ChildElements;
                    slidesCount = slideIds.Count;
                    
                    // Return the slide count to the previous method.
                    return slidesCount;
                }
            }

            return -1;
        }

        public static void GetSlideIdAndText(out int foundCounter, string searchString, Stream docName, int index)
        {
            using (PresentationDocument ppt = PresentationDocument.Open(docName, false))
            {
                //Reset Counter for founded String
                int stringCount = 0;

                // Get the relationship ID of the first slide.
                PresentationPart part = ppt.PresentationPart;
                OpenXmlElementList slideIds = part.Presentation.SlideIdList.ChildElements;

                string relId = (slideIds[index] as SlideId).RelationshipId;

                // Get the slide part from the relationship ID.
                SlidePart slide = (SlidePart)part.GetPartById(relId);

                // Build a StringBuilder object.
                StringBuilder paragraphText = new StringBuilder();

                // Get the inner text of the slide:
                IEnumerable<A.Text> texts = slide.Slide.Descendants<A.Text>();
                foreach (A.Text text in texts)
                {
                    if (text.Text.Contains(searchString) && searchString != "")
                    {
                        stringCount = stringCount + 1;
                    }

                }
                foundCounter = stringCount;
            }
        }
    }
}
