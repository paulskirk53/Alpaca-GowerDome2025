using System.IO.Ports;

namespace GowerDome2025
{
    
    //this is a file/ class I created for settings releated to Dome hardware.

        internal static class DomeSettings
        {
       
        internal static ASCOM.Tools.XMLProfile Profile = new ASCOM.Tools.XMLProfile(Program.DriverID, "Server");

            // Existing settings
            public static int ParkAzimuth
            {
                get => int.Parse(Profile.GetValue("ParkAzimuth", "0"));
                set => Profile.WriteValue("ParkAzimuth", value.ToString());
            }
        public static int HomeAzimuth
        {
            get => int.Parse(Profile.GetValue("HomeAzimuth", "0"));
            set => Profile.WriteValue("HomeAzimuth", value.ToString());
        }

        public static bool SlavingEnabled
            {
                get => bool.Parse(Profile.GetValue("SlavingEnabled", "false"));
                set => Profile.WriteValue("SlavingEnabled", value.ToString());
            }

            public static int ShutterOpenTime
            {
                get => int.Parse(Profile.GetValue("ShutterOpenTime", "30"));
                set => Profile.WriteValue("ShutterOpenTime", value.ToString());
            }

            // New persisted COM port settings
            public static string? ControlBoxComPort
            {
                get => Profile.GetValue("ControlBoxComPort", null);
                private set => Profile.WriteValue("ControlBoxComPort", value ?? "");
            }

            public static string? ShutterComPort
            {
                get => Profile.GetValue("ShutterComPort", null);
                private set => Profile.WriteValue("ShutterComPort", value ?? "");
            }

            /// <summary>
            /// Scan COM ports, identify hardware, update persisted settings, and return status messages.
            /// </summary>
            public static List<string> IdentifyMCUPorts()
            {
                var messages = new List<string>();
                string[] ports = SerialPort.GetPortNames()
                                           .Where(p => p != "COM1")
                                           .ToArray();

                ControlBoxComPort = ""; // using this initialiser rather than null because of get / set in definition. Null does not work
                ShutterComPort = "";

                foreach (string portName in ports)
                {
                    try
                    {
                        using (SerialPort testPort = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One))
                        {
                            //messages.Add($"writing to port {portName}");
                            testPort.ReadTimeout = 500;
                            testPort.WriteTimeout = 500;
                            testPort.Open();
                            Thread.Sleep(500);             // required setup time for mcu with bootloader
                            testPort.DiscardInBuffer();
                            testPort.DiscardOutBuffer();

                            testPort.Write("identify#");

                                            
                        string response = testPort.ReadTo("#").Trim().ToLower();

                            if (response == "controlbox")
                            {
                                ControlBoxComPort = portName;
                                messages.Add($"Control Box found on {portName}");
                            }
                            if (response == "shutter")
                            {
                                ShutterComPort = portName;
                                messages.Add($"Shutter Radio found on {portName}");
                            }

                            if (!string.IsNullOrEmpty(ControlBoxComPort) && !string.IsNullOrEmpty(ShutterComPort))
                            {
                                // messages.Add($"Break on port {portName}");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //messages.Add($"Error on port {portName}: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(ControlBoxComPort))
                { messages.Add("Control Box not found"); }
                if (string.IsNullOrEmpty(ShutterComPort))
                { messages.Add("Shutter Radio not found"); }

                return messages;
            }
        }


    
}
