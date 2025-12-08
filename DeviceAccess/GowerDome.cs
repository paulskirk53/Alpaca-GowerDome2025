using ASCOM;
using ASCOM.Common.Alpaca;
using ASCOM.Common.DeviceInterfaces;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

using System.Timers;


namespace GowerDome2025.DeviceAccess
{

    public class GowerDome : IDomeV3
    {
        // Run IdentifyMCUPorts once at startup so DomeSettings has valid ports
        List<string> ports = DomeSettings.IdentifyMCUPorts();
        private SerialPort control_Box  = new SerialPort("COM31", 19200);   //dummy prts
        private SerialPort? domeShutter = new SerialPort("COM32", 19200);    


        private double lastAzimuth = 27;
        private bool lastSlewing = false;
        private bool _connecting = false;


        public bool connectedState = false;

        public bool Connected     
        {
            get { return connectedState; }
            set
            {


                if (value)    // true = Connect to hardware requested
                {
                    Connecting = true;
                    try
                    {
                        Connect();               //hardware setup
                        connectedState = true;   // flag set here only
                        Connecting = false;

                        Task.Run(() => StartPolling());
                      
                    }
                    catch (ASCOM.InvalidOperationException ex)
                    {
                        throw new ASCOM.DriverException("Hardware not available: " + ex.Message, 100);

                    }
                   

                }
                else    // Disconnect requested
                {

                    try
                    {
                        connectedState = false;
                        Disconnect();
                    }
                    catch (ASCOM.InvalidOperationException ex)
                    {
                        throw new ASCOM.DriverException("Hardware not Disconnected: " + ex.Message, 100);

                    }

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

                double CurrentAzimuth = lastAzimuth;
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
        private System.Timers.Timer pollTimer;


        public void StartPolling()
        {
            pollTimer = new System.Timers.Timer(1500);// interval in ms
            pollTimer.Elapsed += (sender, e) =>
            {
                try
                {

                    if (control_Box == null || !control_Box.IsOpen)
                        return; // skip until connected
                    //control_Box.DiscardInBuffer();
                    control_Box.Write("AZ#");
                    Thread.Sleep(400);
                    string response = control_Box.ReadTo("#");
                    if (double.TryParse(response, out double az))
                        lastAzimuth = az;
                    //
                }
                catch
                { /* ignore errors */

                }
            };
            pollTimer.AutoReset = true;
            pollTimer.Start();
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
        //private Timer shutterPollTimer;
        private ShutterState lastShutterState = ShutterState.Error;

        

        public ShutterState ShutterStatus 
        {
            get
            {
                try
                {
                  
                    if (!domeShutter.IsOpen)
                    {
                        domeShutter.Open();
                       
                    }
                    domeShutter.ReadTimeout = 5000;
                  
                    domeShutter.Write("SS#");
                    
                    
                    string response = domeShutter.ReadTo("#").Trim().ToLower(); 

                    switch (response)
                    {
                        case "open": lastShutterState = ShutterState.Open; break;
                        case "opening": lastShutterState = ShutterState.Opening; break;
                        case "closed": lastShutterState = ShutterState.Closed; break;
                        case "closing": lastShutterState = ShutterState.Closing; break;
                        default: lastShutterState = ShutterState.Error; break;
                    }
                }
                catch
                {
                    // if anything goes wrong, mark as error but don’t block
                    lastShutterState = ShutterState.Error;
                }
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
                    //control_Box.DiscardOutBuffer();
                 //   control_Box.DiscardInBuffer(); // this cured the receive problem from Arduino             
                    control_Box.ReadTimeout = 4000;
                    control_Box.Write("SL#");                 //  accommodates the SL process in the control box mcu

                    


                    string SL_response = control_Box.ReadTo("#").Trim();     // # mark is not included in the string
                    
                    if (SL_response == "Moving")                                      // set this condition properly.
                    {
                        lastSlewing = true;
                        return lastSlewing;

                    }
                    else
                    {
                        lastSlewing = false;
                        return lastSlewing;
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
                domeShutter.Write("ES#");      // halt shutter and close it safely if open or part open
            }
            catch
            {// do nothing in event of failure todo this needs better catch

            }
            //throw new NotImplementedException();
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.NotImplementedException();
        }

        public void CloseShutter()
        {
            const string command = "CS#";
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    if (domeShutter == null)
                    {
                        throw new ASCOM.InvalidOperationException("Control box not initialized.");
                    }
                    if (!domeShutter.IsOpen)
                    {
                        domeShutter.Open();
                        Thread.Sleep(1500);

                    }

                    domeShutter.WriteTimeout = 3000;
                    domeShutter.Write(command);
                    break;   // if we reach here the try succedded so leave the loop
                }
                catch (Exception ex)
                {
                    attempt++;
                }



                if (attempt >= maxRetries)
                {
                    // escalate after retries exhausted

                }

                System.Threading.Thread.Sleep(200); // brief pause before retry
            }
            lastShutterState = ShutterState.Closing;   
            
        }

        public void CommandBlind(string command, bool raw = false)
        {
            throw new ASCOM.NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false)
        {
            throw new ASCOM.NotImplementedException();
        }

        public string CommandString(string command, bool raw = false)
        {
            throw new ASCOM.NotImplementedException();
        }

        public void Connect()
        {
            // as identify ports is called in setup, all we have to do here is ensure control_box and PKshutter are not null
            // there is no need to go through identifyconports() again
            try
            {
               
                SetupPorts();

                if (control_Box == null || domeShutter == null)
                {
                    throw new ASCOM.InvalidOperationException("One or both MCUs could not be identified.");
                }

              
            }
            catch (Exception ex)
            {
                // Wrap in a DriverException so Alpaca clients see a structured error
                throw new ASCOM.DriverException("Hardware connection failed: " + ex.Message, 1024);
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
                control_Box.ReadTimeout = 4000;
                // optional: configure timeouts, event handlers, etc.
            }

            // Shutter
            if (!string.IsNullOrEmpty(DomeSettings.ShutterComPort))  // we know what port is in use from Identifycomports()
            {
                domeShutter = new SerialPort(DomeSettings.ShutterComPort, 19200, Parity.None, 8, StopBits.One);
                domeShutter.Open();
                domeShutter.ReadTimeout = 4000;
            }
        }

       

        public void Disconnect()
        {
            // disconnect the hardware use try catch in case the objects might be null
            try
            {
                
                if (pollTimer != null)
                {
                    pollTimer.Stop();
                    Thread.Sleep(1500);
                }
                if (control_Box != null)
                {
                    if (control_Box.IsOpen)
                    { control_Box.Close(); }
                }  


                if (domeShutter != null)
                {
                    if (domeShutter.IsOpen)
                    { domeShutter.Close(); }  // closes the port safely
                }

                
                Dispose();

            }
            catch (Exception ex)
            {
                throw new ASCOM.DriverException("Hardware disconnection failed: " + ex.Message, 1024);
            }

            // throw new NotImplementedException();
        }

        public void Dispose()
        {
            pollTimer.Dispose();   
            pollTimer = null;      // clear the reference

            domeShutter.Dispose();     // releases unmanaged resources
            domeShutter = null;        // optional: clear reference
            

            control_Box.Dispose();     // releases unmanaged resources
            control_Box = null;        // optional: clear reference

            string x = "";    //todo delete - just used to set a breakpoint

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
                    if (domeShutter == null)
                    {
                        throw new ASCOM.InvalidOperationException("Control box not initialized.");
                    }
                    if (!domeShutter.IsOpen)
                    {
                        domeShutter.Open();
                        Thread.Sleep(1500);

                    }

                    domeShutter.WriteTimeout = 3000;
                    domeShutter.Write(command);
                    break;   // if we reach here the try succedded so leave the loop
                }
                catch (Exception ex)
                {
                    attempt++;
                }
                


                if (attempt >= maxRetries)
                {
                    // escalate after retries exhausted
                    
                }

                System.Threading.Thread.Sleep(200); // brief pause before retry
            }
            lastShutterState = ShutterState.Opening;
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
            throw new ASCOM.NotImplementedException();
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


            throw new ASCOM.NotImplementedException();
        }

    }

}