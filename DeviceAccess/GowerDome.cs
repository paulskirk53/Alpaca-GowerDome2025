using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Common.DeviceInterfaces;
using System.IO.Ports;


namespace GowerDome2025.DeviceAccess
{

    public class GowerDome : IDomeV3
    {
        internal static string? control_BoxComPort;   // note this will contain e.g. COM8
        internal static string? ShutterComPort;       // note this will contain e.g. COM12


        private SerialPort? control_Box;   // serial port objects
        private SerialPort? pkShutter;
        
        private bool _connecting = false;


        public bool connectedState = false;

        public bool Connected      // fetting true connects the hardware.
        {
            get { return connectedState; }
            set
            {


                if (value)    // Connect to hardware requested
                {
                    Connecting = true;
                    try
                    {
                        Connect();          
                       // connectedState = true;   // these are set in connect()
                       // Connecting = false;
                    }
                    catch 
                    {
                        connectedState= false;
                        Connecting = false;
                    }

                    // todo persist data in profile > park azimuth 



                    // Send sync command to microcontroller
                    // the correct method with system.io.ports is shown below with "ES" as example
                    //   control_Box.Write("ES#");


                    //control_Box.Transmit("STA" + parkAzimuthStr + "#");    // this should be the current value initialised in the setup dialog

                }
                else    // Disconnect requested
                {
                    Disconnect();     // todo as this is a void routine in alpaca template it will have to go in try - catch, since if disconnect fails
                                      // connectedstate will be set false and the device will still be connected
                    connectedState = false;
                }
            }
        }

        

        





    public List<StateValue> DeviceState => throw new NotImplementedException();

        public double Altitude => throw new NotImplementedException();

        public bool AtHome => throw new NotImplementedException();

        public bool AtPark => throw new NotImplementedException();

        public double Azimuth => throw new NotImplementedException();

        public bool CanFindHome => true; // throw new NotImplementedException();

        public bool CanPark => throw new NotImplementedException();

        public bool CanSetAltitude => throw new NotImplementedException();

        public bool CanSetAzimuth => throw new NotImplementedException();

        public bool CanSetPark => throw new NotImplementedException();

        public bool CanSetShutter => throw new NotImplementedException();

        public bool CanSlave => throw new NotImplementedException();

        public bool CanSyncAzimuth => true;

        public ShutterState ShutterStatus => throw new NotImplementedException();

        public bool Slaved { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Slewing => throw new NotImplementedException();

        public bool Connecting
        {
            get { return _connecting; }
            set { _connecting = value; }
        }

        public string Description => "Gower Dome"; //

        public string DriverInfo => throw new NotImplementedException();

        public string DriverVersion => throw new NotImplementedException();

        public short InterfaceVersion => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public IList<string> SupportedActions => throw new NotImplementedException();

        public void AbortSlew()
        {
            throw new NotImplementedException();
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new NotImplementedException();
        }

        public void CloseShutter()
        {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false)
        {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false)
        {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            // as identify ports is called in setup, all we have to do here is ensure control_box and PKshutter are not null
            // there is no need to go through identifyconports() again
            try
            {
                _connecting = true;
                //IdentifyMCUPorts(); // Scan and assign control_Box and pkShutter

                if (control_Box == null || pkShutter == null)
                {
                    throw new InvalidOperationException("One or both MCUs could not be identified.");
                }

                connectedState = true;
                _connecting = false;
               // LogMessage("Connect", "MCUs successfully connected.");
            }
            catch (Exception ex)
            {
                connectedState = false;
                _connecting = false;
                //LogMessage("Connect", $"Connection failed: {ex.Message}");
              //  throw new AlpacaException(1279, "Failed to connect to MCUs: " + ex.Message);
            }





        }


        private void IdentifyMCUPorts()
        {
            string[] ports = SerialPort.GetPortNames();

            foreach (string portName in ports)
            {
                try
                {
                    using (SerialPort testPort = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One))
                    {
                        testPort.ReadTimeout = 500;
                        testPort.WriteTimeout = 500;
                        testPort.Open();

                        testPort.DiscardInBuffer();
                        testPort.DiscardOutBuffer();

                        testPort.WriteLine("controlbox");
                        Thread.Sleep(500);
                        string response = testPort.ReadLine().Trim().ToLower();
                        if (response == "controlbox")
                        {
                            control_Box = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One);
                            control_Box.Open();
                            continue;
                        }

                        testPort.DiscardInBuffer();
                        testPort.DiscardOutBuffer();
                        testPort.WriteLine("shutter");
                        Thread.Sleep(200);
                        response = testPort.ReadLine().Trim().ToLower();
                        if (response == "shutter")
                        {
                            pkShutter = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One);
                            pkShutter.Open();
                        }
                    }
                }
                catch { /* Ignore errors during scanning */ }
            }
        }



       

        public void Disconnect()
        {
            // disconnect the hardware use try catch in case the objects might be null
            try
            {
                control_Box.Close();

                pkShutter.Close();
                connectedState = false;
            }
            catch
            {
                connectedState = false;
            }

            // throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void FindHome()
        {
            throw new NotImplementedException();
        }

        public void OpenShutter()
        {
            throw new NotImplementedException();
        }

        public void Park()
        {
            throw new NotImplementedException();
        }

        public void SetPark()
        {
            throw new NotImplementedException();
        }

        public void SlewToAltitude(double Altitude)
        {
            throw new NotImplementedException();
        }

        public void SlewToAzimuth(double Azimuth)
        {
            throw new NotImplementedException();
        }

        public void SyncToAzimuth(double Azimuth)
        {
            throw new NotImplementedException();
        }
    }
}
