using ASCOM.Common.Alpaca;
using ASCOM.Common.DeviceInterfaces;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GowerDome2025.DeviceAccess
{

    public class GowerDome : IDomeV3
    {

        private SerialPort control_Box = new SerialPort("COM3", 9600);   // see comment below
        private SerialPort pkShutter = new SerialPort("COM3", 9600);    // this is to satisfy the silly C# compiler. COM3 is never used. The port will be reassigned in identifycomports()


        private double lastAzimuth = 27;
        private bool lastSlewing = false;
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
                        Task.Run(() => StartPolling());
                        Task.Run(() => StartShutterPolling());
                        // connectedState = true;   // these are set in connect()
                        // Connecting = false;
                    }
                    catch
                    {
                        connectedState = false;
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


        public List<StateValue> DeviceState => new List<StateValue>();
        // was  public List<StateValue> DeviceState => throw new NotImplementedException();


        public double Altitude
        {
            get
            {
                // If your dome doesn’t move vertically, just return 0.0
                return 0.0;
            }
        }





        // public double Altitude = 0.0;  // => throw new NotImplementedException();

        public bool AtHome //=> throw new NotImplementedException();
        {
            get
            {

                double CurrentAzimuth = Azimuth;
                //pk todo remove hardcoding of Home position below - done

                if (Math.Abs((int)CurrentAzimuth - DomeSettings.HomeAzimuth) <= 5)   // care - assumes a fixed sensor position of 270.0 degrees
                {
                    return true;
                }
                else
                {
                    return false;
                }

                //  throw new ASCOM.PropertyNotImplementedException("AtHome", false);
            }
        }

        public bool AtPark   //=> throw new NotImplementedException();
        {
            get
            {

                // check if the current dome azimuth is = to ParkAzimuth
                double CurrentAzimuth = Azimuth;     // note Azimuth is a dome property - see code for it.


                if (Math.Abs((int)CurrentAzimuth - DomeSettings.ParkAzimuth) <= 5)
                    return true;
                else
                    return false;

            }
        }

        public double Azimuth
        {

            get
            {
                return lastAzimuth;
            }
            
        }

        // Background polling task
        private Timer pollTimer;

        public void StartPolling()
        {
            pollTimer = new Timer(_ =>    // this is a timer which runs in the background to poll the azimuth value. it runs in a separate thread to avoid blocking azimuth calls
            {
                try
                {

                    if (control_Box == null || !control_Box.IsOpen)
                        return; // skip until connected
                    //control_Box.DiscardInBuffer();
                    control_Box.Write("AZ#");
                    string response = control_Box.ReadTo("#");
                    if (double.TryParse(response, out double az))
                        lastAzimuth = az;
                   //
                }
                catch 
                { /* ignore errors */
                  
                }
            }, null, 0, 1500); // poll every 1.5 second
        }

       

        public bool CanFindHome => true; // throw new NotImplementedException();

        public bool CanPark => true; //throw new NotImplementedException();

        public bool CanSetAltitude => false;  //throw new NotImplementedException();

        public bool CanSetAzimuth => true; //throw new NotImplementedException();

        public bool CanSetPark => true; //throw new NotImplementedException();

        public bool CanSetShutter => true; // throw new NotImplementedException();

        public bool CanSlave => false; //throw new NotImplementedException();

        public bool CanSyncAzimuth => true;


        // Background polling task for shutter state
        private Timer shutterPollTimer;
        private ShutterState lastShutterState = ShutterState.Error;

        public void StartShutterPolling()
        {
            shutterPollTimer = new Timer(_ =>
            {
                try
                {
                    if (pkShutter == null || !pkShutter.IsOpen)
                        return; // skip until connected

                    pkShutter.ReadTimeout = 1000;
                    pkShutter.Write("SS#");               // request shutter state
                    string response = pkShutter.ReadTo("#").Replace("#", "");

                    switch (response)
                    {
                        case "open":
                            lastShutterState = ShutterState.Open;
                            break;
                        case "opening":
                            lastShutterState = ShutterState.Opening;
                            break;
                        case "closed":
                            lastShutterState = ShutterState.Closed;
                            break;
                        case "closing":
                            lastShutterState = ShutterState.Closing;
                            break;
                        default:
                            lastShutterState = ShutterState.Error;
                            break;
                    }
                }
                catch
                {
                    // if anything goes wrong, mark as error but don’t block
                    lastShutterState = ShutterState.Error;
                }
            }, null, 0, 2000); // poll every 2 seconds
        }

        public ShutterState ShutterStatus 
        {
            get
            {
                return lastShutterState;

            }

        }

        public bool Slaved //{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


        {
            get => false;
            set { /* ignore */ }
        }


        public bool Slewing //  get the slewing status of the dome
        {
            get
            {         //todo 22/11/25 - need to remodel to add a control_box.receievetimout=4 and add the receiveterminated code into the try block
                try
                {
                    control_Box.DiscardOutBuffer();
                    control_Box.DiscardInBuffer(); // this cured the receive problem from Arduino             
                    control_Box.ReadTimeout = 1000;
                    control_Box.Write("SL#");                 //  accommodates the SL process in the control box mcu

                    


                    string SL_response = control_Box.ReadTo("#").Trim();     // ReceiveTerminated("#");            // read what's sent back
                    SL_response = SL_response.Replace("#", "");                       // remove the # mark
                    if (SL_response == "Moving")                                      // set this condition properly.
                    {
                        lastSlewing = true;
                        return true;

                    }
                    else
                    {
                        lastSlewing = false;
                        return false;
                    }
                }


                catch (TimeoutException)
                {
                    return lastSlewing; // return last known value
                }
                catch (OperationCanceledException)
                {
                    return lastSlewing; // handle cancellation gracefully
                }
            }

        }





        public bool Connecting
        {
            get { return _connecting; }
            set { _connecting = value; }
        }

        public string Description => "Alpaca driver for Gower Dome hardware"; //

        public string DriverInfo => "Gower Dome Alpaca driver first go at alpaca driver";  //throw new NotImplementedException(); todo

        public string DriverVersion => "1";  // throw new NotImplementedException(); todo

        public short InterfaceVersion => 3; // throw new NotImplementedException();

        public string Name => "Gower Dome 2025"; //throw new NotImplementedException();

        public IList<string> SupportedActions => new List<string>();   // template had public IList<string> SupportedActions => throw new NotImplementedException();

        public void AbortSlew()
        {
            try
            {
                control_Box.DiscardOutBuffer();
                control_Box.Write("ES#");    // halt dome slewing
                pkShutter.Write("ES#");      // halt shutter and close it safely if open or part open
            }
            catch
            {// do nothing in event of failure todo this needs better catch

            }
            //throw new NotImplementedException();
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new NotImplementedException();
        }

        public void CloseShutter()
        {
            try
            {
                control_Box.DiscardOutBuffer();
                control_Box.Write("CS#");    // halt dome slewing

            }
            catch
            {// do nothing in event of failure todo this needs better catch

            }

            // throw new NotImplementedException();
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

                SetupPorts();

                if (control_Box == null || pkShutter == null)
                {
                    throw new InvalidOperationException("One or both MCUs could not be identified.");
                }

                connectedState = true;
                _connecting = false;
             //  StartPolling();

                // LogMessage("Connect", "MCUs successfully connected.");
            }
            catch
            {
                connectedState = false;
                _connecting = false;
                //LogMessage("Connect", $"Connection failed: {ex.Message}");
                //  throw new AlpacaException(1279, "Failed to connect to MCUs: " + ex.Message);
            }





        }

        // Assuming ServerSettings.ControlBoxComPort and ServerSettings.ShutterComPort
        // are populated by IdentifyMCUPorts()

        private void SetupPorts()
        {
            // Control Box
            if (!string.IsNullOrEmpty(DomeSettings.ControlBoxComPort))  // we know what port is in use from Identifycomports()
            {
                control_Box = new SerialPort(DomeSettings.ControlBoxComPort, 19200, Parity.None, 8, StopBits.One);
                control_Box.Open();
                Thread.Sleep(500);
                // optional: configure timeouts, event handlers, etc.
            }

            // Shutter
            if (!string.IsNullOrEmpty(DomeSettings.ShutterComPort))  // we know what port is in use from Identifycomports()
            {
                pkShutter = new SerialPort(DomeSettings.ShutterComPort, 19200, Parity.None, 8, StopBits.One);
                pkShutter.Open();
                Thread.Sleep(500);
            }
        }

        private void IdentifyMCUPorts()  // this method not used anywhere todo remove
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
                Dispose();
            }
            catch
            {
                connectedState = false;
            }

            // throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (pkShutter != null)
            {
                if (pkShutter.IsOpen)
                    pkShutter.Close();   // closes the port safely

                pkShutter.Dispose();     // releases unmanaged resources
                pkShutter = null;        // optional: clear reference
            }

            if (control_Box != null)
            {
                if (control_Box.IsOpen)
                    control_Box.Close();   // closes the port safely

                control_Box.Dispose();     // releases unmanaged resources
                control_Box = null;        // optional: clear reference
            }


            // throw new NotImplementedException();
        }

        public void FindHome()
        {
            // we need to implement this for the remote Observatory because if a power or MCU reset happens, we lose position
            //findhome 'scans' for the fixed azimuth by slewing the dome and checking if the findhome sensor is activated
            //if so, the MCU azimuth is set to that correct azimuth value associated with the sensor.


            try
            {
                control_Box.DiscardOutBuffer();

                control_Box.Write("FH#");
            }
            catch
            {
                //
                control_Box.DiscardOutBuffer();

                control_Box.Write("FH#");
                // log

            }

        }

        public void OpenShutter()
        {
            const string command = "OS#";
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    if (control_Box == null)
                    {
                        throw new InvalidOperationException("Control box not initialized.");
                    }
                    if (!control_Box.IsOpen)
                    {
                        control_Box.Open();

                    }

                    control_Box.WriteTimeout = 3000;
                    control_Box.Write(command);
                }
                catch (Exception ex)
                {
                    
                }
                attempt++;


                if (attempt >= maxRetries)
                {
                    // escalate after retries exhausted
                    
                }

                System.Threading.Thread.Sleep(200); // brief pause before retry
            }
        }


        public void Park()
        {
            if (!AtPark)                     //if we're already there, do nothing
            {
                SlewToAzimuth(DomeSettings.ParkAzimuth);
            }
            // throw new NotImplementedException();
        }

        public void SetPark()
        {

            // ParkAzimuth is in domsettings as a property, which writes to the xml profile so use that
            DomeSettings.ParkAzimuth = (int)Azimuth; //this writes the current azimuth to the xml profile yes
            //ServerSettings.Profile.WriteValue("ParkAzimuth", Azimuth.ToString());                                        //   save the park azimuth in xml profile store
        }

        public void SlewToAltitude(double Altitude)
        {
            throw new NotImplementedException();
        }

        public void SlewToAzimuth(double Azimuth)
        {

            try
            {
                control_Box.DiscardOutBuffer();

                control_Box.Write("SA" + Azimuth.ToString("0.##") + "#");
            }
            catch
            {

                control_Box.DiscardOutBuffer();

                control_Box.Write("SA" + Azimuth.ToString("0.##") + "#");
                // log
                //tl.LogMessage("Slew to azimuth - attempt to send CL and SA for angle > 180", ex.ToString());
            }



            //throw new NotImplementedException();
        }

        public void SyncToAzimuth(double Azimuth)
        {

            try
            {
                control_Box.DiscardOutBuffer();
                control_Box.Write("STA" + Azimuth.ToString("0.##") + "#");
            }
            catch
            {
                control_Box.DiscardOutBuffer();
                control_Box.Write("STA" + Azimuth.ToString("0.##") + "#");
            }


            throw new NotImplementedException();
        }

    }

}