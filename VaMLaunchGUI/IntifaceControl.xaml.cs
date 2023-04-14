﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Buttplug;
using VAMLaunch;

namespace VaMLaunchGUI
{

    public class CheckedListItem
    {
        public CheckedListItem(ButtplugClientDevice dev, bool isChecked = false)
        {
            Device = dev;
            IsChecked = isChecked;
        }

        public string Name => Device.Name;

        public uint Id => Device.Index;

        public ButtplugClientDevice Device { get; set; }

        public bool IsChecked { get; set; }
    }

    /// <summary>
    /// Interaction logic for IntifaceControl.xaml
    /// </summary>
    public partial class IntifaceControl : UserControl
    {
        public ObservableCollection<CheckedListItem> DevicesList { get; set; } = new ObservableCollection<CheckedListItem>();

        private ButtplugClient _client;
        private List<ButtplugClientDevice> _devices = new List<ButtplugClientDevice>();
        private Task _connectTask;
        private bool _quitting;

        public EventHandler ConnectedHandler;
        public EventHandler DisconnectedHandler;
        public EventHandler<string> LogMessageHandler;
        //        public bool IsConnected => _client.Connected;

        public IntifaceControl()
        {
            InitializeComponent();
            DeviceListBox.ItemsSource = DevicesList;
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
            _autoConnect.IsChecked = VAMLaunchProperties.Default.ConnectOnStartup;
            _remoteAddress.Text = VAMLaunchProperties.Default.WebsocketAddress;
            _remoteAddress.TextChanged += (object o, TextChangedEventArgs e) =>
            {
                VAMLaunchProperties.Default.WebsocketAddress = _remoteAddress.Text;
                VAMLaunchProperties.Default.Save();
            };
            _radioEmbedded.IsChecked = VAMLaunchProperties.Default.UseEmbedded;
            _radioRemote.IsChecked = VAMLaunchProperties.Default.UseRemote;
            OnRadioChange(null, null);
            if (_autoConnect.IsChecked == true)
            {
                var addressArg = _radioEmbedded.IsChecked == false ? _remoteAddress.Text : null;
                OnConnectClick(null, null);
            }
            _connectStatus.Text = "Not connected";
            _scanningButton.IsEnabled = false;
        }

        public void OnAutoConnectChange(object o, EventArgs e)
        {
            VAMLaunchProperties.Default.ConnectOnStartup = _autoConnect.IsChecked == true;
            VAMLaunchProperties.Default.Save();
        }

        public void OnRadioChange(object o, EventArgs e)
        {
            if (_radioEmbedded == null || _radioRemote == null)
            {
                return;
            }
            VAMLaunchProperties.Default.UseEmbedded = _radioEmbedded.IsChecked == true;
            VAMLaunchProperties.Default.UseRemote = _radioRemote.IsChecked == true;
            VAMLaunchProperties.Default.Save();
            EmbeddedConnectionOptions.Visibility = _radioEmbedded.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            RemoteConnectionOptions.Visibility = _radioRemote.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public void OnConnectClick(object o, EventArgs e)
        {
            _connectButton.IsEnabled = false;
            _radioEmbedded.IsEnabled = false;
            _radioRemote.IsEnabled = false;
            var address = _radioEmbedded.IsChecked == false ? _remoteAddress.Text : null;
            _connectTask = new Task(async () => await ConnectTask(address));
            _connectTask.Start();
        }

        public async Task ConnectTask(string aAddress)
        {
            var client = new ButtplugClient("VaMSync");
            client.DeviceAdded += OnDeviceAdded;
            client.DeviceRemoved += OnDeviceRemoved;
            client.ServerDisconnect += OnDisconnect;
            client.ScanningFinished += OnScanningFinished;
            try
            {
                if (aAddress == null)
                {
                    await client.ConnectAsync(new ButtplugEmbeddedConnectorOptions());
                }
                else
                {
                    var connector = new ButtplugWebsocketConnectorOptions(new Uri(aAddress));
                    await client.ConnectAsync(connector);
                }

                _client = client;

                Dispatcher.Invoke(() =>
                {
                    ConnectedHandler?.Invoke(this, new EventArgs());
                    _connectStatus.Text = $"Connected{(aAddress == null ? ", restart VaMSync to disconnect." : " to Remote Buttplug Server")}";
                    OnScanningClick(null, null);
                    _scanningButton.IsEnabled = true;
                    _connectButton.Visibility = Visibility.Collapsed;
                    if (aAddress != null)
                    {
                        _disconnectButton.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (ButtplugConnectorException ex)
            {
                Debug.WriteLine("Connection failed.");
                // If the exception was thrown after connect, make sure we disconnect.
                if (_client != null && _client.Connected)
                {
                    await _client.DisconnectAsync();
                    _client = null;
                }
                Dispatcher.Invoke(() =>
                {
                    _connectStatus.Text = $"Connection failed, please try again.";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Did something else fail? {ex})");
                // If the exception was thrown after connect, make sure we disconnect.
                if (_client != null && _client.Connected)
                {
                    await _client.DisconnectAsync();
                    _client = null;
                }
                Dispatcher.Invoke(() =>
                {
                    _connectStatus.Text = $"Connection failed, please try again.";
                });
            }
            finally
            {
                if (_client == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _connectButton.IsEnabled = true;
                        _radioEmbedded.IsEnabled = true;
                        _radioRemote.IsEnabled = true;
                    });
                }
            }
        }

        public void OnScanningFinished(object o, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() => _scanningButton.Content = "Start Scanning"));
        }

        public void OnDisconnectClick(object o, EventArgs e)
        {
            _disconnectButton.IsEnabled = false;
            new Task(async () => await Disconnect()).Start();
        }

        public async Task Disconnect()
        {
            await _client.DisconnectAsync();
            Dispatcher.Invoke(() => {
                OnDisconnect(null, null);
            });
        }

        public Task StartScanning()
        {
            return _client.StartScanningAsync();
        }

        public Task StopScanning()
        {
            return _client.StopScanningAsync();
        }

        public void OnScanningClick(object aObj, EventArgs aArgs)
        {
            _scanningButton.IsEnabled = false;
            // Dear god this is so jank. How is IsScanning not exposed on the Buttplug Client?
            if (_scanningButton.Content.ToString().Contains("Stop"))
            {
                try
                {
                    _scanningButton.Content = "Start Scanning";
                    Task.Run(async () => await StopScanning());
                }
                catch (ButtplugException e)
                {
                    // This will happen if scanning has already stopped. For now, ignore it.
                }
            }
            else
            {
                _scanningButton.Content = "Stop Scanning";
                Task.Run(async () => await StartScanning());
            }
            _scanningButton.IsEnabled = true;
        }

        public void OnDisconnect(object aObj, EventArgs aArgs)
        {
            Dispatcher.Invoke(() =>
            {
                _connectButton.IsEnabled = true;
                _connectButton.Visibility = Visibility.Visible;
                _disconnectButton.Visibility = Visibility.Collapsed;
                _connectStatus.Text = "Disconnected";
                DevicesList.Clear();
                _client.Dispose();
                _client = null;
            });
        }

        public void OnDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    DevicesList.Add(new CheckedListItem(aArgs.Device));
                }
                catch (Exception ex)
                {
                    // Ignore already added devices
                }
            });
        }

        public void OnDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var dev in DevicesList)
                {
                    if (dev.Id != aArgs.Device.Index)
                    {
                        continue;
                    }
                    DevicesList.Remove(dev);
                    return;
                }
            });
        }

        public async Task Vibrate(int device, int motor, double aSpeed)
        {
            var devices = DevicesList;
            //Console.WriteLine("---------");
            //Console.WriteLine($"Vibrate {device} {motor} {aSpeed}");
            if (device != Command.DEVICE_ALL) {
                var singleDevice = DevicesList.FirstOrDefault(d => d.Device.Index == device - 1);
                if(singleDevice != null)
                {
                    //Console.WriteLine($"found device {device}");
                    devices = new ObservableCollection<CheckedListItem> { singleDevice };
                }
                else
                {
                    //Console.WriteLine($"no found device {device}");
                    devices = new ObservableCollection<CheckedListItem>();
                }
            }

            foreach (var deviceItem in devices)
            {
                if(!deviceItem.IsChecked)
                {
                    continue;
                }

                if(deviceItem.Device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.VibrateCmd))
                {
                    if(motor == Command.MOTOR_ALL)
                    {
                        await deviceItem.Device.SendVibrateCmd(aSpeed);
                    }
                    else
                    {
                        await deviceItem.Device.SendVibrateCmd(new Dictionary<uint, double>()
                        {
                            { (uint)motor-1, aSpeed }
                        });
                    }

                }
            }
        }

        public async Task Linear(int device, int motor, uint aDuration, double aPosition)
        {
            var devices = DevicesList;
            //Console.WriteLine("---------");
            //Console.WriteLine($"Linear {device} {motor} {aDuration} {aPosition}");
            if (device != Command.DEVICE_ALL) {
                var singleDevice = DevicesList.FirstOrDefault(d => d.Device.Index == device - 1);
                if(singleDevice != null)
                {
                    //Console.WriteLine($"found device {device}");
                    devices = new ObservableCollection<CheckedListItem> { singleDevice };
                }
                else
                {
                    //Console.WriteLine($"no found device {device}");
                    devices = new ObservableCollection<CheckedListItem>();
                }
            }

            foreach (var deviceItem in devices)
            {
                if(!deviceItem.IsChecked)
                {
                    continue;
                }

                if(deviceItem.Device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.LinearCmd) && aDuration > 0)
                {
                    if(motor == Command.MOTOR_ALL)
                    {
                        await deviceItem.Device.SendLinearCmd(aDuration, aPosition);
                    }
                    else
                    {
                        await deviceItem.Device.SendLinearCmd(new Dictionary<uint, (uint, double)>()
                        {
                            { (uint)motor-1, (aDuration, aPosition) }
                        });
                    }

                }
            }
        }

        public async Task StopVibration()
        {
            await Vibrate(Command.DEVICE_ALL, Command.MOTOR_ALL, 0);
        }

        private void DisposeClient()
        {
            if (_client == null)
            {
                return;
            }
            _client.Dispose();
            _client = null;
        }
    }
}