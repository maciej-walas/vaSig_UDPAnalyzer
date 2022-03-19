using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace UDPAnalyzer.Models
{
    public class ReceivedDatagram
    {
        private const int MaxCharsToTrim = 100;
        public DateTime Timestamp { get; set; }
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
        public string ReceivedString { get; set; }
        public string ReceivedStringTrimmed { get; set; }
        public byte[] ReceivedRaw { get; set; }
        public int Size { get; set; }

        public ReceivedDatagram(IPAddress ipAddress, int port, byte[] receivedDataRaw)
        {
            Timestamp = DateTime.Now;
            IpAddress = ipAddress;
            Port = port;
            ReceivedString = Encoding.ASCII.GetString(receivedDataRaw);
            ReceivedRaw = receivedDataRaw;
            Size = receivedDataRaw.Length;
            var trimmed = Regex.Replace(ReceivedString, @"\n", "")[..(ReceivedString.Length >= MaxCharsToTrim ? MaxCharsToTrim - 1 : ReceivedString.Length - 1)];
            ReceivedStringTrimmed = ReceivedString.Length <= MaxCharsToTrim ? trimmed : $"{trimmed}(...) \n(+{ReceivedString.Length - MaxCharsToTrim} additional characters)";
        }
    }
}