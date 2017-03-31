using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DemoBlu
{
    public class DataPersistencyManager
    {
        public static void SaveStringData(string key, string value)
        {
            Application.Current.Properties[key] = value;
        }

        public static string GetStringData(string key)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                return (string)Application.Current.Properties[key];
            }
            else
                return null;
        }

        public static bool AppHasData(string key)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                return true;
            }
            else
                return false;
        }

        public static void RemoveStringData(string key)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                Application.Current.Properties.Remove(key);
            }
        }
    }
}
