using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBlu
{
    public static class Constants
    {
        public static string Guid = "Guid";
        public static string Name = "Name";

        public static string ServiceUuid = "f000c0e0-0451-4000-b000-000000000000";
        public static string CharacteristicUuidNotify = "f000c0e1-0451-4000-b000-000000000000";
        public static string CharacteristicUuidWrite = "f000c0e1-0451-4000-b000-000000000000";


        // keys used in the dictionary for saving persistently data 
        public static string HomeValue = "HomeValue";
        public static string SunValue = "SunValue";
        public static string SunCloudValue = "SunCloudValue";
        public static string CloudValue = "CloudValue";
        public static string TeethValue = "TeethValue";
        public static string LastSavedStateCmd = "LastSavedStateCmd";
        public static string LastSavedStateScenario = "LastSavedStateScenario";
        public static string LastSavedStateLightIntensity = "LastSavedStateLightIntensity";


        public static byte[] RequestStatusCommand = new byte[] { 0xFF, 0x01, 0x55 };

        /// <summary>
        /// Writes the status command.
        /// </summary>
        /// <returns>The status command.</returns>
        /// <param name="cmd">Cmd 0=spento 1=spento 2=acceso</param>
        /// <param name="scenario">Scenario 0..5</param>
        /// <param name="lightIntensity">Light intensity valore assoluto</param>
        public static byte[] WriteStatusCommand(int cmd, int scenario, int lightIntensity)
        {
            byte c;
            switch (cmd)
            {
                case 0:
                    c = 0x00;
                    break;
                case 1:
                    c = 0x01;
                    break;
                case 2:
                    c = 0x02;
                    break;
                default:
                    c = 0x00;
                    break;
            }

            byte s;
            switch (scenario)
            {
                case 0:
                    s = 0x00;
                    break;
                case 1:
                    s = 0x01;
                    break;
                case 2:
                    s = 0x02;
                    break;
                case 3:
                    s = 0x03;
                    break;
                case 4:
                    s = 0x04;
                    break;
                case 5:
                    s = 0x05;
                    break;
                default:
                    s = 0x00;
                    break;
            }

            var bytes = new byte[] { 0xFF, 0x11, c, s, Convert.ToByte(lightIntensity), 0x55 };
            return bytes;
        }

    }
}
