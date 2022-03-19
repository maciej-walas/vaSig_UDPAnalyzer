using System;
using System.Net.Sockets;
using System.Text;

namespace UDPAnalyzer.Services
{

    public class UdpServerService : IDisposable
    {
        private readonly UdpClient _udpClient;

        public UdpServerService()
        {
            _udpClient = new UdpClient();
        }

        public void Send(string hostname, int port, string data)
        {
            _udpClient.Connect(hostname, port);
            var datagramBytes = Encoding.ASCII.GetBytes(data);
            _udpClient.Send(datagramBytes, datagramBytes.Length);
        }

        public void Dispose()
        {
            _udpClient.Close();
        }
    }

}