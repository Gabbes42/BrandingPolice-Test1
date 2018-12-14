using System;

namespace WebJob
{
    [Serializable]
    public class JobMessage : BaseMessage
    {
        public string StorageName { get; set; }

        public string FilePathPresentation { get; set; }

        public string FilePathResult { get; set; }

        public string SearchString { get; set; }

        public static JobMessage JobStringToJobMessage(string cloudQueueMessage)
        {

            // Description delimiter is |||
            string descriptionDelimiter = "|||";

            try
            {
                string[] projectDescriptionSplitParts;
                projectDescriptionSplitParts = cloudQueueMessage.Split(new string[] { descriptionDelimiter }, StringSplitOptions.None);

                //Console.WriteLine("+++ORIGINAL: " + projectDescription);
                //Console.WriteLine("###PART1: " + projectDescriptionSplitParts[0]);
                //Console.WriteLine("###PART2: " + projectDescriptionSplitParts[1]);

                JobMessage jobMessage = new JobMessage()
                {
                    StorageName = projectDescriptionSplitParts[0],
                    FilePathPresentation = projectDescriptionSplitParts[1],
                    FilePathResult = projectDescriptionSplitParts[2],
                    SearchString = projectDescriptionSplitParts[3]
                };

                return jobMessage;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
