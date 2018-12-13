using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace WebJob
{
    [Serializable]
    public abstract class BaseMessage
    {
        // Umwandlung des Objekts für die Queue (Objekt->Binary->Byte[])
        public byte[] ToBinary()
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            byte[] output = null;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Position = 0;
                bf.Serialize(ms, this);
                output = ms.GetBuffer();
            }
            return output;
        }

        // Message aus der Queue auslesen und Objekt wiederherstellen (Byte[]->Binary->Objekt)
        public static JobMessage FromMessage<JobMessage>(CloudQueueMessage m)
        {
            byte[] buffer = m.AsBytes;
            JobMessage returnValue = default(JobMessage);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                returnValue = (JobMessage)bf.Deserialize(ms);
            }
            return returnValue;
        }
    }
}