using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UDPAnalyzer.Helpers;
using UDPAnalyzer.Models;
using UDPAnalyzer.Services;

namespace UDPAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private uint _random;
        private bool _filterApplied;

        private UdpListenerService _listenerService;
        private readonly UdpServerService _udpServerService;
        private static Thread _listenerServiceThread;

        private readonly ObservableCollection<ReceivedDatagram> _receivedData = new();
        private readonly ObservableCollection<PortList> _portList = new();
        private readonly ICollectionView _receivedDataFiltered;

        public MainWindow()
        {
            InitializeComponent();

            _random = RandomGenerator.GetRandomNumber();
            _udpServerService = new UdpServerService();

            var timer = new DispatcherTimer(DispatcherPriority.SystemIdle)
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += TimerOnTick;
            timer.Start();

            var receivedDataSourceList = new CollectionViewSource() { Source = _receivedData };
            _receivedDataFiltered = receivedDataSourceList.View;
            ReceivedDataGrid.ItemsSource = _receivedDataFiltered;
            PortList.ItemsSource = _portList;
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            _random = RandomGenerator.GetRandomNumber();
            RandomNumberLabel.Content = _random;
        }

        private void ListenerServiceOnDataReceived(object sender, ReceivedDataArgs args)
        {
            var dataToSend = $"ACK! {_random}";
            _udpServerService.Send(args.ReceivedDatagram.IpAddress.ToString(),args.ReceivedDatagram.Port, dataToSend);

            _random = RandomGenerator.GetRandomNumber();

            Dispatcher.Invoke(() => {
                _receivedData.Add(args.ReceivedDatagram);
                if (_portList.Any(x =>
                        Equals(x.IpAddress, args.ReceivedDatagram.IpAddress) && x.Port == args.ReceivedDatagram.Port))
                {
                    var listItem = _portList.FirstOrDefault(x =>
                        Equals(x.IpAddress, args.ReceivedDatagram.IpAddress) && x.Port == args.ReceivedDatagram.Port);

                    if (listItem != null)
                    {
                        listItem.Count += 1;
                        listItem.LastTime = args.ReceivedDatagram.Timestamp;
                    }
                }
                else
                {
                    var addNewItemsToList = AddNewToFilter.IsChecked != null && (bool)AddNewToFilter.IsChecked;
                    _portList.Add(new PortList()
                    {
                        Checked = addNewItemsToList,
                        Count = 1,
                        Port = args.ReceivedDatagram.Port,
                        IpAddress = args.ReceivedDatagram.IpAddress,
                        LastTime = args.ReceivedDatagram.Timestamp
                    });
                }
                RandomNumberLabel.Content = _random;
                Filter(_filterApplied);
            });
        }

        private async void StartListening_Click(object sender, RoutedEventArgs e)
        {
            var ipAddressString = IpAddressTextBox.Text;
            var portString = PortTextBox.Text;
            var ipAddress = IPAddress.Any;
            int port;

            try
            {
                if (ipAddressString.Length != 0) ipAddress = IPAddress.Parse(ipAddressString);
                var parsed = int.TryParse(portString, out port);
                if (!parsed || port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
                {
                    await this.ShowMessageAsync("Wrong port number", $"The port number must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
                    return;
                }
            }
            catch (ArgumentNullException)
            {
                await this.ShowMessageAsync("Error", "You have not entered an IP address");
                return;
            }
            catch (FormatException)
            {
                await this.ShowMessageAsync("Error", "The IP address or port entered is in the wrong format");
                return;
            }

            try
            {
                var endpoint = new IPEndPoint(ipAddress, port);

                _listenerService = new UdpListenerService(endpoint);
                _listenerServiceThread = new Thread(_listenerService.StartListen);
                _listenerServiceThread.Start();
                _listenerService.DataReceivedEvent += ListenerServiceOnDataReceived;
                IpAddressTextBox.Text = ipAddress.ToString();
                ListenState.Content = $"Listening on {(Equals(ipAddress, IPAddress.Any) ? "all IPs" : ipAddress.ToString())} on port: {port}";
                EnableUserControls(false);
            }
            catch (SocketException ex)
            {
                await this.ShowMessageAsync("Error", $"Error while trying to open connection: {ex.Message}");
            }
        }

        private void StopListening_Click(object sender, RoutedEventArgs e)
        {
            _listenerService.DataReceivedEvent -= ListenerServiceOnDataReceived;
            _listenerService.Dispose();

            ListenState.Content = "Stopped";
            EnableUserControls(true);
        }

        private async void ClearReceivedData_Click(object sender, RoutedEventArgs e)
        {
            var settings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Clear",
                NegativeButtonText = "Cancel"
            };

            var result = await this.ShowMessageAsync("Are you sure?",
                "Clicking \"Clear\" will delete all received data",
                MessageDialogStyle.AffirmativeAndNegative,
                settings);

            if (result != MessageDialogResult.Affirmative) return;
            _receivedData.Clear();
            _portList.Clear();
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            Filter(true);
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            Filter(false);
            foreach (var item in _portList)
            {
                item.Checked = true;
            }
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _listenerService?.Dispose();
            }
            catch (NullReferenceException)
            {

            }
        }

        private void ReceivedDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var receivedDataItem = (ReceivedDatagram)ReceivedDataGrid.CurrentItem;
            if (receivedDataItem == null) return;
            FlyoutIP.Content = receivedDataItem.IpAddress.ToString();
            FlyoutPort.Content = receivedDataItem.Port;
            FlyoutTimestamp.Content = receivedDataItem.Timestamp.ToString("O");
            FlyoutDataSize.Content = receivedDataItem.Size + " bytes";
            FlyoutRawData.Document.Blocks.Clear();
            FlyoutRawData.Document.Blocks.Add(new Paragraph(new Run(Convert.ToHexString(receivedDataItem.ReceivedRaw))));
            FlyoutStringData.Document.Blocks.Clear();
            FlyoutStringData.Document.Blocks.Add(new Paragraph(new Run(receivedDataItem.ReceivedString)));
            ReceivedDataFlyout.IsOpen = true;
        }

        private void AddNewToFilter_Checked(object sender, RoutedEventArgs e)
        {
            Filter(true);
        }

        private void AddNewToFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            Filter(true);
        }

        private void Filter(bool isEnabled)
        {
            var receivedDataFilter = new Predicate<object>(item => _portList.Any(x => x.IpAddress.Equals(((ReceivedDatagram)item).IpAddress) && x.Port.Equals(((ReceivedDatagram)item).Port) && x.Checked));
            if (_receivedDataFiltered == null) return;
            _receivedDataFiltered.Filter = isEnabled ? receivedDataFilter : null;
            _filterApplied = isEnabled;
        }

        private void EnableUserControls(bool enabled)
        {
            PortTextBox.IsEnabled = enabled;
            IpAddressTextBox.IsEnabled = enabled;
            StartListening.IsEnabled = enabled;
            StopListening.IsEnabled = !enabled;
            ProgressBar.IsIndeterminate = !enabled;
            ProgressBar.Visibility = enabled ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
