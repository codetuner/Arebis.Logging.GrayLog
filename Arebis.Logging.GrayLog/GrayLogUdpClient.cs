using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;

namespace Arebis.Logging.GrayLog
{
    /// <summary>
    /// A GrayLog client using the UDP protocol.
    /// </summary>
    public class GrayLogUdpClient : GrayLogClient
    {
        /// <summary>
        /// Creates a new GrayLogUdpClient using the "GrayLogFacility", "GrayLogHost" and optionally "GrayLogUdpPort" AppSettings.
        /// </summary>
        public GrayLogUdpClient()
            : this(ConfigurationManager.AppSettings["GrayLogFacility"], ConfigurationManager.AppSettings["GrayLogHost"], Int32.Parse(ConfigurationManager.AppSettings["GrayLogUdpPort"] ?? "12201"))
        { }

        /// <summary>
        /// Creates a new GrayLogUdpClient.
        /// </summary>
        /// <param name="facility">Facility to set on all sent messages.</param>
        /// <param name="host">GrayLog host name.</param>
        /// <param name="port">GrayLog UDP port.</param>
        public GrayLogUdpClient(string facility, string host, int port = 12201)
            : this(facility, new UdpClient(host, port), true)
        { }

        /// <summary>
        /// Creates a new GrayLogUdpClient.
        /// </summary>
        /// <param name="facility">Facility to set on all sent messages.</param>
        /// <param name="udpClient">UdpClient to use to sent messages to GrayLog.</param>
        public GrayLogUdpClient(string facility, UdpClient udpClient)
            : this(facility, udpClient, false)
        { }

        protected GrayLogUdpClient(string facility, UdpClient udpClient, bool udpClientIsOwned)
            : base(facility)
        {
            this.UdpClient = udpClient;
            this.UdpClientIsOwned = udpClientIsOwned;
            this.NextChunckedMessageId = new Random().Next(0, Int32.MaxValue);
            this.MaxPacketSize = Int32.Parse(ConfigurationManager.AppSettings["GrayLogUdpMaxPacketSize"] ?? "512");
        }

        private bool UdpClientIsOwned { get; set; }

        protected UdpClient UdpClient { get; private set; }

        protected long NextChunckedMessageId { get; private set; }

        /// <summary>
        /// Maximum UDP packet payload byte size used.
        /// Can also be overriden by the "GrayLogUdpMaxPacketSize" AppSetting.
        /// </summary>
        public int MaxPacketSize { get; set; }

        protected override void InternallySendMessage(byte[] messageBody)
        {
            // Get message id:
            NextChunckedMessageId++;

            var messageBodyCompressed = this.Compress(messageBody, CompressionLevel.Optimal);

            using (var objectStream = new MemoryStream(messageBodyCompressed))
            {
                // Calculate needed chunk count:
                var chunkCount = PartsNeeded(messageBodyCompressed.Length, MaxPacketSize - 12);
                if (chunkCount > 128) 
                {
                    Debug.WriteLine(String.Format("Error: Maximum number of GrayLog GELF UDP chuncks exceeded; {0} chuncks while maximum is {1}", chunkCount, 128));
                    throw new GrayLoggingException("Maximum number of GrayLog GELF UDP chuncks exceeded.");
                }

                // For each chunk:
                for (byte chunkNumber = 0; chunkNumber < chunkCount; chunkNumber++)
                {
                    // Create packet buffer:
                    var chunkBuffer = new byte[MaxPacketSize];

                    // Write GELF header (http://docs.graylog.org/en/latest/pages/gelf.html):
                    chunkBuffer[0x00] = (byte)0x1e;
                    chunkBuffer[0x01] = (byte)0x0f;
                    BitConverter.GetBytes(NextChunckedMessageId).CopyTo(chunkBuffer, 0x02);
                    chunkBuffer[0x0a] = chunkNumber;
                    chunkBuffer[0x0b] = (byte)chunkCount;

                    // Write body:
                    var chunkSize = 12 + objectStream.Read(chunkBuffer, 12, MaxPacketSize - 12);

                    // Send UDP packet:
                    Debug.WriteLine(String.Format("GrayLogUdpClient - Writing chunk {0} ({1}/{2}) {3:#,##0} bytes : ", NextChunckedMessageId, chunkNumber + 1, chunkCount, chunkSize) + String.Format("[{0:X2}-{1:X2}, {2:X2}-{3:X2}-{4:X2}-{5:X2}-{6:X2}-{7:X2}-{8:X2}-{9:X2}, {10:X2}, {11:X2}, [{12:X2},{13:X2},{14:X2},{15:X2},...]]", chunkBuffer[0], chunkBuffer[1], chunkBuffer[2], chunkBuffer[3], chunkBuffer[4], chunkBuffer[5], chunkBuffer[6], chunkBuffer[7], chunkBuffer[8], chunkBuffer[9], chunkBuffer[10], chunkBuffer[11], chunkBuffer[12], chunkBuffer[13], chunkBuffer[14], chunkBuffer[15]));
                    this.UdpClient.Send(chunkBuffer, chunkSize);
                }
            }
        }

        /// <summary>
        /// Disposes the client.
        /// </summary>
        public override void Dispose()
        {
            ((IDisposable)this.UdpClient).Dispose();
        }

        private static int PartsNeeded(int totalSize, int partSize)
        {
            return (totalSize + partSize - 1) / partSize;
        }
    }
}
