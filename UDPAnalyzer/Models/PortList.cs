using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace UDPAnalyzer.Models
{
    public class PortList : INotifyPropertyChanged
    {
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged();
            }
        }
        public DateTime LastTime
        {
            get => _lastTime;
            set
            {
                _lastTime = value;
                OnPropertyChanged();
            }
        }
        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                OnPropertyChanged();
            }
        }

        private bool _checked;
        private int _count;
        private DateTime _lastTime;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
