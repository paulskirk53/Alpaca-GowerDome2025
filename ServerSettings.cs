using ASCOM.Alpaca;
using System.IO.Ports;

namespace GowerDome2025
{
    internal static class ServerSettings
    {
        //This is a shared profile that is used to store server settings.
        internal static ASCOM.Tools.XMLProfile Profile = new ASCOM.Tools.XMLProfile(Program.DriverID, "Server");

        internal static void Reset()
        {
            Profile.Clear();
        }

        internal static string Location
        {
            get
            {
                return Profile.GetValue("Location", "Unknown");
            }
            set
            {
                Profile.WriteValue("Location", value.ToString());
            }
        }
       // public static string ControlBoxComPort { get; set; } = string.Empty;  // todo pk added this to initialise and persist the comport
        //AI says it is possible save the setting like this in the driver.cs - ServerSettings.Save(); 


        internal static class DomeSettings
        {
            internal static ASCOM.Tools.XMLProfile Profile = new ASCOM.Tools.XMLProfile(Program.DriverID, "Server");

            // Existing settings
            public static int ParkAzimuth
            {
                get => int.Parse(Profile.GetValue("ParkAzimuth", "0"));
                set => Profile.WriteValue("ParkAzimuth", value.ToString());
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

                ControlBoxComPort = "";
                ShutterComPort    = "";

                foreach (string portName in ports)
                {
                    try
                    {
                        using (SerialPort testPort = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One))
                        {
                            messages.Add($"writing to port {portName}");
                            testPort.ReadTimeout = 500;
                            testPort.WriteTimeout = 500;
                            testPort.Open();

                            testPort.DiscardInBuffer();
                            testPort.DiscardOutBuffer();

                            testPort.Write("identify#");
                            Thread.Sleep(500);

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

 


        internal static bool AutoStartBrowser
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("AutoStartBrowser", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("AutoStartBrowser", value.ToString());
            }
        }

        internal static ushort ServerPort
        {
            get
            {
                if (ushort.TryParse(Profile.GetValue("ServerPort", Program.DefaultPort.ToString()), out ushort result))
                {
                    return result;
                }
                return Program.DefaultPort;
            }
            set
            {
                Profile.WriteValue("ServerPort", value.ToString());
            }
        }

        internal static bool AllowRemoteAccess
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("AllowRemoteAccess", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("AllowRemoteAccess", value.ToString());
            }
        }

        internal static bool AllowDiscovery
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("AllowDiscovery", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("AllowDiscovery", value.ToString());
            }
        }

        internal static bool LocalRespondOnlyToLocalHost
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("LocalRespondOnlyToLocalHost", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("LocalRespondOnlyToLocalHost", value.ToString());
            }
        }

        internal static bool PreventRemoteDisconnects
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("PreventRemoteDisconnects", false.ToString()), out bool result))
                {
                    return result;
                }
                return false;
            }
            set
            {
                Profile.WriteValue("PreventRemoteDisconnects", value.ToString());
            }
        }

        internal static bool RunSwagger
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("RunSwagger", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("RunSwagger", value.ToString());
            }
        }

        internal static bool AllowImageBytesDownload
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("CanImageBytesDownload", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("CanImageBytesDownload", value.ToString());
            }
        }

        internal static bool RunInStrictAlpacaMode
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("RunInStrictAlpacaMode", true.ToString()), out bool result))
                {
                    return result;
                }
                return true;
            }
            set
            {
                Profile.WriteValue("RunInStrictAlpacaMode", value.ToString());
            }
        }

        internal static bool UseAuth
        {
            get
            {
                if (bool.TryParse(Profile.GetValue("UseAuth", false.ToString()), out bool result))
                {
                    return result;
                }
                return false;
            }
            set
            {
                Profile.WriteValue("UseAuth", value.ToString());
            }
        }

        internal static string UserName
        {
            get
            {
                return Profile.GetValue("UserName", "User");
            }
            set
            {
                Profile.WriteValue("UserName", value.ToString());
            }
        }

        internal static string Password
        {
            get
            {
                return Profile.GetValue("Password");
            }
            set
            {
                Profile.WriteValue("Password", Hash.GetStoragePassword(value));
            }
        }

        internal static ASCOM.Common.Interfaces.LogLevel LoggingLevel
        {
            get
            {
                if (Enum.TryParse(Profile.GetValue("LoggingLevel", ASCOM.Common.Interfaces.LogLevel.Information.ToString()), out ASCOM.Common.Interfaces.LogLevel result))
                {
                    return result;
                }
                return ASCOM.Common.Interfaces.LogLevel.Information;
            }
            set
            {
                Program.Logger?.SetMinimumLoggingLevel(value);
                Profile.WriteValue("LoggingLevel", value.ToString());
            }
        }

        internal static string GetDeviceUniqueId(string DeviceType, int DeviceID)
        {
            string deviceKey = $"{DeviceType}-{DeviceID}";
            if (Profile.ContainsKey(deviceKey))
            {
                return Profile.GetValue(deviceKey);
            }
            else
            {
                var NewGuid = Guid.NewGuid();

                Profile.WriteValue(deviceKey, NewGuid.ToString());

                return NewGuid.ToString();
            }
        }

        internal static string GetDeviceUniqueId(ASCOM.Common.DeviceTypes DeviceType, int DeviceID)
        {
            string deviceKey = $"{DeviceType.ToString()}-{DeviceID}";
            if (Profile.ContainsKey(deviceKey))
            {
                return Profile.GetValue(deviceKey);
            }
            else
            {
                var NewGuid = Guid.NewGuid();

                Profile.WriteValue(deviceKey, NewGuid.ToString());

                return NewGuid.ToString();
            }
        }
    }
}