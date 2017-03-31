using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace DemoBlu
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new ScanPage());
            BleStatusManager.Instance.ConnectToKnownDevice();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            BleStatusManager.Instance.Disconnect();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            BleStatusManager.Instance.ConnectToKnownDevice();
        }
    }
}
