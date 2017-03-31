using System;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Text;

namespace DemoBlu
{
    public class BleStatusManager
    {

        public BehaviorSubject<bool?> BleSensorStatus = new BehaviorSubject<bool?>(null);
        public BehaviorSubject<bool?> GpsSensorStatus = new BehaviorSubject<bool?>(null);

        public BehaviorSubject<bool?> Connecting = new BehaviorSubject<bool?>(null);
        public BehaviorSubject<IDevice> Connected = new BehaviorSubject<IDevice>(null);
        public BehaviorSubject<bool?> Scanning = new BehaviorSubject<bool?>(null);

        Subject<List<byte>> requestStatusSubject = new Subject<List<byte>>();

        public Subject<IDevice> Devices = new Subject<IDevice>();

        IBluetoothLE Ble;
        IAdapter Adapter;
        public IDevice ConnectedDevice;

        public ObservableCollection<IDevice> DeviceList { get; set; }

        IService Service;
        ICharacteristic CharacteristicNotify;
        ICharacteristic CharacteristicWrite;

        bool isParsing = false;
        List<byte> parsed = new List<byte>();

        #region Singleton init
        private static BleStatusManager instance;

        public static BleStatusManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BleStatusManager();
                }
                return instance;
            }
        }
        #endregion

        BleStatusManager()
        {
            Setup();
        }

        async void Setup()
        {
            #region initializations
            Ble = CrossBluetoothLE.Current;
            Adapter = CrossBluetoothLE.Current.Adapter;
            DeviceList = new ObservableCollection<IDevice>();
            var state = Ble.State;
            Debug.WriteLine(Ble.State);
            //Il blu funziona con servizi, che hanno un Uid e caratteristiche che vengono scritte e al loro cambiamento posso alzare un evento o posso solo leggerle
      
            #endregion

            #region trigger gps status check (Android)
            Device.OnPlatform(Android: async () =>
            {
                while (true)
                {
                    await CheckGpsForAndroid();
                    await Task.Delay(250);
                }
            });
            #endregion

            #region trigger ble status check
            Device.OnPlatform(
            iOS: () =>
            {
                if (Ble.IsOn) BleSensorStatus.OnNext(true);
                else BleSensorStatus.OnNext(false);
            },
            Android: async () =>
            {
                if (Ble.IsOn)
                    BleSensorStatus.OnNext(true);
                else BleSensorStatus.OnNext(false);
                await CheckGpsForAndroid();
            });
            #endregion

            #region ble status changing
            Ble.StateChanged += async (s, e) =>
            {
                Debug.WriteLine($"The bluetooth state changed to {e.NewState}");

                if (e.NewState == BluetoothState.Off)
                {
                    BleSensorStatus.OnNext(false);
                    Connecting.OnNext(false);

                    //Disconnect();
                }
                else if (e.NewState == BluetoothState.On)
                {
                    BleSensorStatus.OnNext(true);

                    var d = Adapter.ConnectedDevices;
                    Connecting.OnNext(true);
                    await Task.Delay(2000);
                    ConnectToKnownDevice();
                }
            };
            #endregion

            #region discovery
            // durante lo scan faccio questo
            Adapter.DeviceDiscovered += (s, a) =>
            {
                Debug.WriteLine(a.Device.Name + "device");
                DeviceList.Add(a.Device);
                Devices.OnNext(a.Device);

            };
            #endregion

            #region device connected
            Adapter.DeviceConnected +=  (s, e) =>
            {
                Debug.WriteLine("Device Connected ************************");

                Connecting.OnNext(false);

                ConnectedDevice = e.Device;
                Connected.OnNext(ConnectedDevice);

                DataPersistencyManager.SaveStringData("Guid", ConnectedDevice.Id.ToString());
                DataPersistencyManager.SaveStringData("NomeDevice", ConnectedDevice.Name);

                //await SubscribeNotification();
            };
            #endregion

            #region timeout, connection lost
            Adapter.ScanTimeoutElapsed += (sender, e) => Scanning.OnNext(false);

            Adapter.DeviceConnectionLost += (s, e) =>
            {
                Debug.WriteLine("Device Connection Lost");
                Connected.OnNext(null);
                ConnectedDevice = null;
            };
            #endregion

            requestStatusSubject.Throttle(TimeSpan.FromMilliseconds(250))
                                .Subscribe(val => RequestStatus());
        }

        #region notification subscription
        async Task SubscribeNotification()
        {
            //Il blu funziona con servizi, che hanno un Uid e caratteristiche che vengono scritte e al loro cambiamento posso alzare un evento o posso solo leggerle
            try
            {
                Service = await ConnectedDevice.GetServiceAsync(Guid.Parse(Constants.ServiceUuid));
                CharacteristicWrite = await Service.GetCharacteristicAsync(Guid.Parse(Constants.CharacteristicUuidWrite));
                CharacteristicNotify = await Service.GetCharacteristicAsync(Guid.Parse(Constants.CharacteristicUuidNotify));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            //Quando questa caratteristica cambia, che ho inizializzato prima faccio...
            CharacteristicNotify.ValueUpdated += (o, args) =>
            {
            };

            await CharacteristicNotify.StartUpdatesAsync(); //?
        }
        #endregion
        public async Task readStatus()
        {
            try
            {
                Service = await ConnectedDevice.GetServiceAsync(Guid.Parse(Constants.ServiceUuid));
                CharacteristicNotify = await Service.GetCharacteristicAsync(Guid.Parse(Constants.CharacteristicUuidNotify));
                await CharacteristicNotify.();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            //Quando questa caratteristica cambia, che ho inizializzato prima faccio...
            CharacteristicNotify.ValueUpdated +=  (o, args) =>
            {
                //byte[] readed = CharacteristicNotify.ReadAsync();
                MessagingCenter.Send<BleStatusManager, string>(this, "stato cambiato","stato cambiato");
            };
        }
         public async Task sendMessage(String input)
        {
            try
            {
                Service = await ConnectedDevice.GetServiceAsync(Guid.Parse(Constants.ServiceUuid));
                CharacteristicWrite = await Service.GetCharacteristicAsync(Guid.Parse(Constants.CharacteristicUuidWrite));
                CharacteristicWrite.WriteAsync(Encoding.UTF8.GetBytes(input));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        async Task CheckGpsForAndroid()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (status == PermissionStatus.Granted && DependencyService.Get<ILocationServiceManager>().IsGpsEnabled() == true)
            {
                GpsSensorStatus.OnNext(true);
            }
            else GpsSensorStatus.OnNext(false);
        }

        public async Task ScanForDevices()
        {
            Scanning.OnNext(true);
            try
            {
                await Adapter.StartScanningForDevicesAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public async Task StopScanning()
        {
            await Adapter.StopScanningForDevicesAsync();
            Scanning.OnNext(false);
        }

        public async Task ConnectToDevice(IDevice device)
        {
            try
            {
                Connecting.OnNext(true);
                await Adapter.ConnectToDeviceAsync(device);
            }
            catch (DeviceConnectionException e)
            {
                Debug.WriteLine(e.Message + "ConnectToDevice(IDevice device)");
                Connecting.OnNext(false);
                Connected.OnNext(null);
            }
        }

        public async Task ConnectToKnownDevice()
        {
            if (DataPersistencyManager.AppHasData(Constants.Guid) && Ble.State == BluetoothState.On)
            {

                Connecting.OnNext(true);

                try
                {
                    CancellationTokenSource _cancellationTokenSource;
                    _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    //await Adapter.ConnectToKnownDeviceAsync(Guid.Parse(DataPersistencyManager.GetStringData("Gui")), _cancellationTokenSource.Token);
                    _cancellationTokenSource.Dispose();
                }
                catch (DeviceConnectionException e)
                {
                    // ... could not connect to device
                    Debug.WriteLine(e.Message);
                    Connected.OnNext(null);
                    Connecting.OnNext(false);
                }
                catch (TaskCanceledException e)
                {
                    Debug.WriteLine("Timeout - ConnectToKnownDevice()" + e.Message);
                    Connected.OnNext(null);
                    Connecting.OnNext(false);
                }
            }
            else
            {
                Connected.OnNext(null);
                Connecting.OnNext(false);
            }
            Connecting.OnNext(false);
        }

        public async Task RequestStatus()
        {/*per leggere dati io scrivo su una caratteristic e lui mi risponde*/
         // reset status array

            /*SidereaStatus.Instance.ResponseStatus = new byte[] { };

            try
            {
                await CharacteristicWrite.WriteAsync(Constants.RequestStatusCommand);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }*/
        }

        /// <summary>
        /// Writes the status command.
        /// </summary>
        /// <returns>The status command.</returns>
        /// <param name="command">Cmd 0=spento 1=spento 2=acceso</param>
        /// <param name="scenario">Scenario 0..5</param>
        /// <param name="lightIntensity">Light intensity valore assoluto</param>
        public async void WriteStatus(int command, int scenario, int lightIntensity)
        {
            //try
            //{
            //    await CharacteristicWrite.WriteAsync(Constants.WriteStatusCommand(command, scenario, lightIntensity));
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex);
            //}
        }

        public async Task Disconnect()
        {
            await Adapter.DisconnectDeviceAsync(ConnectedDevice);
            ConnectedDevice = null;
        }
    }
}
