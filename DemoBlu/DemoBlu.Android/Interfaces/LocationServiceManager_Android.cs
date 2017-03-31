using Android.Content;
using Android.Locations;
using Android.Provider;
using DemoBlu;
using Xamarin.Forms;


[assembly: Xamarin.Forms.Dependency(typeof(DemoBlu.Droid.Interfaces.LocationServiceManager_Android))]

namespace DemoBlu.Droid.Interfaces
{
    public class LocationServiceManager_Android : ILocationServiceManager
    {
        public void GoToGpsSettings()
        {
            Intent gpsSettingIntent = new Intent(Settings.ActionLocationSourceSettings);
            Forms.Context.StartActivity(gpsSettingIntent);
        }

        public bool IsGpsEnabled()
        {
            LocationManager locationManager = (LocationManager)Forms.Context.GetSystemService(Context.LocationService);
            if (locationManager.IsProviderEnabled(LocationManager.GpsProvider) == false) return false;
            return true;
        }
    }
}