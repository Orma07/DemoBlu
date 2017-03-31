using Android.Bluetooth;
using Android.Content;
using Android.Provider;
using DemoBlu.Droid;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(DemoBlu.Droid.Interfaces.BleManager_Android))]
namespace DemoBlu.Droid.Interfaces
{
    public class BleManager_Android : IBleManager
    {
        public bool IsBleEnabled()
        {
            if (BluetoothAdapter.DefaultAdapter.IsEnabled) return true;
            return false;
        }

        public void GoToBleSettings()
        {
            Intent bleSettingIntent = new Intent(Settings.ActionBluetoothSettings);
            Forms.Context.StartActivity(bleSettingIntent);
        }
    }
}
