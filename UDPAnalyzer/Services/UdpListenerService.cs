using System;
using System.Net;
using System.Net.Sockets;
using UDPAnalyzer.Models;

namespace UDPAnalyzer.Services
{

    public class UdpListenerService : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _remoteIpEndPoint;
        private bool _disposed;

        public delegate void DataReceived(object sender, ReceivedDataArgs args);
        public event DataReceived DataReceivedEvent;

        public UdpListenerService(IPEndPoint remoteIpEndPoint)
        {
            _remoteIpEndPoint = remoteIpEndPoint;
            _udpClient = new UdpClient(_remoteIpEndPoint.Port);

        }

        public void StartListen()
        {
            while (!_disposed)
            {
                try
                {
                    var data = _udpClient.Receive(ref _remoteIpEndPoint);
                    RaiseEvent(new ReceivedDataArgs(_remoteIpEndPoint.Address, _remoteIpEndPoint.Port, data));
                }
                catch (SocketException)
                {

                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            try
            {
                _udpClient.Close();
            }
            catch (ObjectDisposedException)
            {

            }
            catch (NullReferenceException)
            {

            }
            finally
            {
                _udpClient = null;
                _remoteIpEndPoint = null;
            }
        }

        private void RaiseEvent(ReceivedDataArgs args)
        {
            if (DataReceivedEvent != null)
            {
                DataReceivedEvent(this, args);
            }
        }
    }

    public class ReceivedDataArgs : EventArgs
    {
        public ReceivedDatagram ReceivedDatagram { get; set; }

        public ReceivedDataArgs(IPAddress ipAddress, int port, byte[] receivedDataBytes)
        {
            ReceivedDatagram = new ReceivedDatagram(ipAddress, port, receivedDataBytes);
        }
    }

}