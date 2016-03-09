using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Arebis.Logging.GrayLog
{
    /// <summary>
    /// Base for GrayLog client implementations.
    /// </summary>
    public abstract class GrayLogClient : IDisposable
    {
        /// <summary>
        /// Constructs a new GrayLogClient.
        /// </summary>
        /// <param name="facility">The facility to set on all sent messages.</param>
        protected GrayLogClient(string facility)
        {
            this.Facility = facility;
        }

        /// <summary>
        /// The facility to set on all sent messages.
        /// </summary>
        public string Facility { get; protected set; }

        /// <summary>
        /// Sents a message to GrayLog.
        /// </summary>
        /// <param name="shortMessage">Short message text (required).</param>
        /// <param name="fullMessage">Full message text.</param>
        /// <param name="data">Additional details object. Can be a plain object, a string, an enumerable or a dictionary.</param>
        public void Send(string shortMessage, string fullMessage = null, object data = null)
        {
            // Construct log record:
            var logRecord = new Dictionary<string, object>();
            logRecord["version"] = "1.1";
            logRecord["host"] = Environment.MachineName;
            logRecord["_facility"] = this.Facility;
            logRecord["short_message"] = shortMessage;
            if (!String.IsNullOrWhiteSpace(fullMessage)) logRecord["full_message"] = fullMessage;
            logRecord["timestamp"] = EpochOf(DateTime.UtcNow);
            if (data is string) logRecord["_data"] = data;
            else if (data is System.Collections.IEnumerable) logRecord["_values"] = data;
            else if (data is System.Collections.IDictionary) MergeDictionary(logRecord, (System.Collections.IDictionary)data, "_");
            else if (data != null) MergeObject(logRecord, data, "_");

            // Serialize object:
            string logRecordString = JsonConvert.SerializeObject(logRecord);
            var logRecordBytes = Encoding.UTF8.GetBytes(logRecordString);

            // Dispatch message:
            this.InternallySendMessage(logRecordBytes);
        }

        /// <summary>
        /// Convenience method to send an exception message to GrayLog.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public void Send(Exception ex)
        {
            // Collect exception data:
            var data = new System.Collections.Hashtable();
            for (var iex = ex; iex != null; iex = iex.InnerException)
            {
                foreach (var key in iex.Data.Keys)
                {
                    data.Add(key, iex.Data[key]);
                }
            }

            // Send exception:
            this.Send(ex.Message, ex.ToString(), data);
        }

        /// <summary>
        /// Protocol specific implementation of sending a message.
        /// </summary>
        /// <param name="messageBody">The UTF8 encoded JSON message.</param>
        protected abstract void InternallySendMessage(byte[] messageBody);

        /// <summary>
        /// Disposes the client.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Helper method to apply GZIP compression.
        /// </summary>
        protected byte[] Compress(byte[] raw, CompressionLevel compressionLevel)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, compressionLevel, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        private void MergeDictionary(Dictionary<string, object> target, System.Collections.IDictionary source, string prefix)
        {
            foreach (var key in source.Keys)
            {
                target[prefix + key] = source[key];
            }
        }

        private static void MergeObject(IDictionary<string, object> target, dynamic source, string prefix = "")
        {
            foreach (PropertyInfo property in source.GetType().GetProperties())
            {
                target[prefix + property.Name] = property.GetValue(source);
            }
        }

        private static long EpochOf(DateTime dt)
        {
            TimeSpan t = dt.ToUniversalTime() - new DateTime(1970, 1, 1);
            return (long)t.TotalSeconds;
        }
    }
}
