using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;

namespace DemoBlu
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanPage : ContentPage
    {
        ObservableCollection<IDevice> DeviceList { get; set; }
        bool scanningEnabled;

        Command _GpsCommand;
        public ICommand GpsCommand
        {
            get
            {
                if (_GpsCommand == null) _GpsCommand = new Command(HandleGpsCommand);
                return _GpsCommand;
            }
        }

        Command _BleCommand;
        public ICommand BleCommand
        {
            get
            {
                if (_BleCommand == null) _BleCommand = new Command(HandleBleCommand);
                return _BleCommand;
            }
        }

        public ScanPage()
        {
            InitializeComponent();

            BindingContext = this;

            gpsBanner.Command = GpsCommand;
            bleBanner.Command = BleCommand;

           

            DeviceList = new ObservableCollection<IDevice>();
            deviceList.ItemsSource = DeviceList;

   

            var refreshTapGestureRecognizer = new TapGestureRecognizer();
            scaButton.Clicked+= (s, e) =>
            {
                if (scanningEnabled)
                {
                    Handle_Clicked();
                }
            };

            BleStatusManager.Instance.Devices.Subscribe(obj =>
            {
                if (obj != null)
                {
                    DeviceList.Add(obj);
                }
            });

            BleStatusManager.Instance.BleSensorStatus.Subscribe(obj => Device.BeginInvokeOnMainThread(() => bleBanner.IsVisible = !(bool)obj));

            Device.OnPlatform(Android: () => BleStatusManager.Instance.GpsSensorStatus.Subscribe(obj => Device.BeginInvokeOnMainThread(() => gpsBanner.IsVisible = !(bool)obj)));

            BleStatusManager.Instance.Connecting.Subscribe(obj =>
            {
                if (obj != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        spinner.IsRunning = spinner.IsVisible = (bool)obj;
                        scaButton.IsVisible = !(bool)obj;
                        scanningEnabled = !(bool)obj;
                    });
                }
            });
            BleStatusManager.Instance.Scanning.Subscribe(obj =>
            {
                if (obj != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        spinner.IsRunning = spinner.IsVisible = (bool)obj;
                        scaButton.IsVisible = !(bool)obj;
                        scanningEnabled = !(bool)obj;
                    });
                }
            });

            BleStatusManager.Instance.Connected.Subscribe(device =>
            {
                if (device == null)
                    Device.BeginInvokeOnMainThread(() => { statusLabel.Text = "Non connesso"; input.IsVisible = false; sendInput.IsVisible = false; status.IsVisible = false; });
                else
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        statusLabel.Text = device.Name; input.IsVisible = true;
                        sendInput.IsVisible = true;
                        status.IsVisible = true;
                        await BleStatusManager.Instance.readStatus();
                        MessagingCenter.Subscribe<BleStatusManager, string>(this, "stato cambiato", (sender, arg) =>
                        {
                            status.Text = arg;
                        });
                    });
                }
            });
        }

        public void sendMessage()
        {
            BleStatusManager.Instance.sendMessage(input.Text);
        }

        private void HandleGpsCommand(object parameter)
        {
            try
            {
                Device.OnPlatform(
                Android: async () =>
                {

                    // Andorid need GPS in order to get BLE

                    // verify to have permission for Localization
                    var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                    if (status != PermissionStatus.Granted)
                    {
                        var permissionGPS = await DisplayAlert("Abilita i permessi per la localizzazione", "Per il corretto funzionamento del Bluetooth LE Android necessita dei permessi di localizzazione", "Continua", "Annulla");
                        // if user continue i promt the request for permission
                        if (permissionGPS)
                        {
                            try
                            {
                                var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Permission.Location });
                                status = results[Permission.Location];
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                        }
                    }

                    // verifiy if gps is enabled
                    if (DependencyService.Get<ILocationServiceManager>().IsGpsEnabled() == false)
                    {
                        // if not i ask user to enable it
                        var enableGPS = await DisplayAlert("Il GPS è disabilitato", "Abilita il gps per il corretto funzionamento dell'app", "Ok", "Annulla");
                        // if want to enable go to settings
                        if (enableGPS) DependencyService.Get<ILocationServiceManager>().GoToGpsSettings();
                    }


                }
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex);
            }
        }
        void HandleBleCommand(object parameter)
        {
            Debug.WriteLine("BleCommand");
            Device.OnPlatform(
                iOS: async () => await DisplayAlert("Il bluetooth è disabilitato", "Abilita il bluetooth in Impostazioni -> Bluetooth", "Ok"),
                Android: async () => {
                    // i ask user to enable it
                    var enableBLE = await DisplayAlert("Il bluetooth è disabilitato", "Abilita il bluetooth per il corretto funzionamento dell'app", "Ok", "Annulla");
                    // if want to enable go to settings
                    if (enableBLE) DependencyService.Get<IBleManager>().GoToBleSettings();
                }
            );

        }
        async void CheckGpsForAndroid()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (status == PermissionStatus.Granted && DependencyService.Get<ILocationServiceManager>().IsGpsEnabled() == true)
                gpsBanner.IsVisible = false;
            else
                gpsBanner.IsVisible = true;
        }

        async Task Handle_Clicked()
        {
            DeviceList.Clear();
            await BleStatusManager.Instance.ScanForDevices();
        }

        async Task GetStatus()
        {
            await BleStatusManager.Instance.RequestStatus();
        }

        async void Handle_ItemTapped(object sender, Xamarin.Forms.ItemTappedEventArgs e)
        {
            deviceList.SelectedItem = null; //dico alla listView che nulla è attivato

            await BleStatusManager.Instance.StopScanning(); // ferma la scansione dei dispositivi bluetooth 

            //CONTROLLO, se c'è vado sul dialog e chiedo conferma di connettersi alla lampada
            IDevice item = (IDevice)e.Item;
            await BleStatusManager.Instance.ConnectToDevice(item);

        }



    }
}
