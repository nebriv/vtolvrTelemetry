using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.Newtonsoft.Json;
using System.IO;
using System.Text;

namespace vtolvrTelemetry
{
    public class DataGetters
    {

        public vtolvrTelemetry dataLogger { get; set; }

        public DataGetters(vtolvrTelemetry dataLogger)
        {
            this.dataLogger = dataLogger;
        }

        public void GetData()
        {

            LogData f_info = new LogData();

            try
            {

                if (dataLogger.printOutput)
                {
                    Support.WriteLog("Collecting Data...");
                }
                Actor playeractor = FlightSceneManager.instance.playerActor;
                GameObject currentVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();

                f_info.unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                f_info.Physics.XAccel = Math.Round(playeractor.flightInfo.acceleration.x, 2).ToString();
                f_info.Physics.YAccel = Math.Round(playeractor.flightInfo.acceleration.y, 2).ToString();
                f_info.Physics.ZAccel = Math.Round(playeractor.flightInfo.acceleration.z, 2).ToString();
                f_info.Physics.PlayerGs = Math.Round(playeractor.flightInfo.playerGs, 2).ToString();

                f_info.Vehicle.VehicleName = currentVehicle.name;

                f_info.Vehicle.Heading = Math.Round(playeractor.flightInfo.heading, 2).ToString();
                f_info.Vehicle.Pitch = Math.Round(playeractor.flightInfo.pitch, 2).ToString();
                f_info.Vehicle.Roll = Math.Round(playeractor.flightInfo.roll, 2).ToString();
                f_info.Vehicle.AoA = Math.Round(playeractor.flightInfo.aoa, 2).ToString();
                f_info.Vehicle.Airspeed = Math.Round(playeractor.flightInfo.airspeed, 2).ToString();
                f_info.Vehicle.VerticalSpeed = Math.Round(playeractor.flightInfo.verticalSpeed, 2).ToString();
                f_info.Vehicle.AltitudeASL = Math.Round(playeractor.flightInfo.altitudeASL, 2).ToString();

                Health health = Traverse.Create(playeractor).Field("h").GetValue() as Health;
                f_info.Vehicle.Health = health.currentHealth.ToString();

                f_info.Vehicle.Drag = Math.Round(playeractor.flightInfo.rb.drag, 2).ToString();
                f_info.Vehicle.Mass = Math.Round(playeractor.flightInfo.rb.mass, 2).ToString();
                f_info.Vehicle.IsLanded = playeractor.flightInfo.isLanded.ToString();


                f_info.Vehicle.Fuel.FuelDensity = DataGetters.getFuelDensity(currentVehicle);
                f_info.Vehicle.Fuel.FuelBurnRate = DataGetters.getFuelBurnRate(currentVehicle);
                f_info.Vehicle.Fuel.FuelLevel = DataGetters.getFuelLevel(currentVehicle);

                f_info.Vehicle.BatteryLevel = DataGetters.GetBattery(currentVehicle);
                f_info.Vehicle.Engines = DataGetters.GetEngineStats(currentVehicle);

                f_info.Vehicle.RadarCrossSection = DataGetters.getRadarCrossSection(currentVehicle);
                f_info.Vehicle.TailHook = DataGetters.GetHook(currentVehicle);

                f_info.Vehicle.Lights = DataGetters.getVehicleLights(currentVehicle);

                f_info.Vehicle.Flaps = DataGetters.getFlaps(currentVehicle);
                
                f_info.Vehicle.Brakes = DataGetters.getBrakes(currentVehicle);
                f_info.Vehicle.EjectionState = DataGetters.getEjectionState(currentVehicle);

                f_info.Vehicle.Avionics.RadarState = DataGetters.getRadarState(currentVehicle);
                f_info.Vehicle.Avionics.RWRContacts = DataGetters.getRWRContacts(currentVehicle);
                f_info.Vehicle.Avionics.MissileDetected = DataGetters.getMissileDetected(currentVehicle);
                f_info.Vehicle.Avionics.StallDetector = DataGetters.GetStall(currentVehicle);

                f_info.Vehicle.Avionics.masterArm = DataGetters.getMasterArm(currentVehicle);

                // Dumps all components in the vehicle. Will freeze game, but useful to see what we get.
                //GetAllVehicleComponents(currentVehicle);


                if (dataLogger.printOutput)
                {
                    Support.WriteLog(f_info.ToCSV());
                }

            }
            catch (Exception ex)
            {
                Support.WriteErrorLog($"{Globals.projectName} - Error getting telemetry data " + ex.ToString());
            }


            if (dataLogger.csvEnabled == true)
            {
                if (dataLogger.printOutput)
                {
                    Support.WriteLog("Saving CSV");
                }

                if (!File.Exists(dataLogger.csv_path))
                {
                    using (StreamWriter sw = File.AppendText(dataLogger.csv_path))
                    {
                        sw.WriteLine(f_info.CSVHeaders());
                    }
                }

                using (StreamWriter sw = File.AppendText(dataLogger.csv_path))
                {
                    sw.WriteLine(f_info.ToCSV());
                }
            }

            if (dataLogger.jsonEnabled == true)
            {
                if (dataLogger.printOutput)
                {
                    Support.WriteLog("Saving JSON...");
                }

                using (StreamWriter sw = File.AppendText(dataLogger.json_path))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(f_info) + "\n");
                }
            }

            if (dataLogger.udpEnabled == true)
            {
                if (dataLogger.printOutput)
                {
                    Support.WriteLog("Sending UDP Packet...");
                }
                try
                {
                    SendUdp(JsonConvert.SerializeObject(f_info));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Globals.projectName} - Error sending UDP " + ex.ToString());
                }
            }


        }

        public static List<Dictionary<string, string>> GetEngineStats(GameObject vehicle)
        {
            List<Dictionary<string, string>> engines = new List<Dictionary<string, string>>();

            int i = 1;

            foreach (ModuleEngine engine in vehicle.GetComponentsInChildren<ModuleEngine>())
            {
                Dictionary<string, string> engineDict = new Dictionary<string, string>();
                engineDict.Add("Engine Number", i.ToString());
                engineDict.Add("Enabled", engine.engineEnabled.ToString());
                engineDict.Add("Failed", engine.failed.ToString());
                engineDict.Add("Starting", engine.startingUp.ToString());
                engineDict.Add("Started", engine.startedUp.ToString());
                engineDict.Add("RPM", engine.displayedRPM.ToString());
                engineDict.Add("Afterburner", engine.afterburner.ToString());
                engineDict.Add("FinalThrust", engine.finalThrust.ToString());
                engineDict.Add("FinalThrottle", engine.finalThrottle.ToString());
                engineDict.Add("MaxThrust", engine.maxThrust.ToString());

                engines.Add(engineDict);
                i++;
            }

            return engines;
        }

        public static string getBrakes(GameObject vehicle)
        {
            try
            {
                AeroController aero = vehicle.GetComponentInChildren<AeroController>();
                return aero.brake.ToString();
            }
            catch (Exception ex)
            {
                return "Unavailable";
            }
        }
        public static string getRadarState(GameObject vehicle)
        {
            try
            {
                Radar radar = vehicle.GetComponentInChildren<Radar>();
                return radar.radarEnabled.ToString();
            }
            catch (Exception ex)
            {
                return "Unavailable";
            }

        }
        public static string getEjectionState(GameObject vehicle)
        {
            string ejectionState;
            try
            {
                EjectionSeat ejection = vehicle.GetComponentInChildren<EjectionSeat>();
                ejectionState = ejection.ejected.ToString();
            }
            catch (Exception ex)
            {
                ejectionState = "Unavailable";
            }

            return ejectionState;
        }

        public static List<Dictionary<string, string>> getRWRContacts(GameObject vehicle)
        {
            List<Dictionary<string, string>> contacts = new List<Dictionary<string, string>>();

            try
            {
                ModuleRWR rwr = vehicle.GetComponentInChildren<ModuleRWR>();

                foreach (ModuleRWR.RWRContact contact in rwr.contacts)
                {
                    Dictionary<string, string> contactDict = new Dictionary<string, string>();
                    contactDict.Add("active", contact.active.ToString());
                    contactDict.Add("locked", contact.locked.ToString());
                    Actor radar_actor = contact.radarActor;
                    contactDict.Add("friendFoe", radar_actor.team.ToString());
                    contactDict.Add("name", radar_actor.name.ToString());
                    contactDict.Add("radarSymbol", contact.radarSymbol.ToString());
                    contactDict.Add("signalStrength", contact.signalStrength.ToString());

                    contacts.Add(contactDict);
                }

            }
            catch (NullReferenceException ex)
            {
                //I don't think this really matters here. It seems to work and I'm too lazy to debug it. 
                //I think ModuleRWR only updates at a certain rate and does not exist otherwise.
            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Error getting RWR Contacts: " + ex);
            }
            return contacts;

        }

        public static string getMissileDetected(GameObject vehicle)
        {
            try
            {
                MissileDetector md = vehicle.GetComponentInChildren<MissileDetector>();
                return md.missileDetected.ToString();
            }
            catch (Exception ex)
            {
                return "Unavailable";
            }
        }

        public static string getMasterArm(GameObject vehicle)
        {
            bool masterArmState = false;
            try
            {


                foreach (VRLever lever in vehicle.GetComponentsInChildren<VRLever>())
                {

                    if (lever.gameObject.name == "masterArmSwitchInteractable")
                    {
                        if (lever.currentState == 1)
                        {
                            masterArmState = true;
                            break;
                        }
                        else
                        {
                            masterArmState = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Error getting master arm state: " + ex.ToString());
            }
            return masterArmState.ToString();
        }

        public static Dictionary<string, string> getVehicleLights(GameObject vehicle)
        {

            Dictionary<string, string> lights = new Dictionary<string, string>();

            //this is BAD
            //Light landingLights = vehicle.transform.Find("LandingLight").GetComponent<Light>();


            try
            {
                bool landinglight = false;
                bool navlight = false;
                bool strobelight = false;

                foreach (Light light in vehicle.GetComponentsInChildren<Light>())
                {

                    if (light.gameObject.name == "LandingLight")
                    {
                        landinglight = true;
                    }
                    if (light.gameObject.name.ToString().Contains("StrobeLight"))
                    {
                        strobelight = true;
                    }
                    Support.WriteLog(light.ToString());
                }


                foreach (SpriteRenderer spriteish in vehicle.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (spriteish.ToString().Contains("Nav"))
                    {
                        navlight = true;
                    }

                }

                lights.Add("LandingLights", landinglight.ToString());
                lights.Add("NavLights", navlight.ToString());
                lights.Add("StrobeLights", strobelight.ToString());

                return lights;

            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Error getting lights " + ex.ToString());
                return lights;
            }
        }

        public static string getFlaps(GameObject vehicle)
        {
            try
            {
                AeroController aero = vehicle.GetComponentInChildren<AeroController>();
                return aero.flaps.ToString();
            }
            catch (Exception ex)
            {
                return "Unavailable";
            }
        }

        public static string GetStall(GameObject vehicle)
        {

            try
            {

                HUDStallWarning warning = vehicle.GetComponentInChildren<HUDStallWarning>();

                //Nullable boolean allows it to get "stalling" if it doesn't exist? and sets it as false? I think.
                Boolean? stalling = Traverse.Create(warning).Field("stalling").GetValue() as Boolean?;

                return stalling.ToString();
            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("unable to get stall status: " + ex.ToString());
                return "False";
            }

        }

        public static string GetHook(GameObject vehicle)
        {

            try
            {

                Tailhook hook = vehicle.GetComponentInChildren<Tailhook>();

                //Nullable boolean allows it to get "stalling" if it doesn't exist? and sets it as false? I think.
                Boolean? deployed = Traverse.Create(hook).Field("deployed").GetValue() as Boolean?;

                return deployed.ToString();
            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("unable to get stall status: " + ex.ToString());
                return "False";
            }

        }

        public static string GetBattery(GameObject vehicle)
        {


            try
            {
                Battery batteryCharge = vehicle.GetComponentInChildren<Battery>();
                string battery = batteryCharge.currentCharge.ToString();
                return battery;
            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("unable to get battery status: " + ex.ToString());
                return "False";
            }

        }

        public static string getFuelLevel(GameObject vehicle)
        {
            try
            {
                FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
                return tank.totalFuel.ToString();
            }
            catch (Exception ex)
            {
                return "False";
            }
        }

        public static string getFuelBurnRate(GameObject vehicle)
        {

            try
            {
                FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
                return tank.fuelDrain.ToString();
            }
            catch (Exception ex)
            {
                return "False";
            }

        }

        public static string getFuelDensity(GameObject vehicle)
        {
            try
            {
                FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
                return tank.fuelDensity.ToString();
            }
            catch (Exception ex)
            {
                return "False";
            }

        }

        public static bool GetGunFiring(GameObject vehicle)
        {

            try
            {
                WeaponManager weaponManager = vehicle.GetComponentInChildren<WeaponManager>();

                if (weaponManager.availableWeaponTypes.gun)
                {
                    return weaponManager.isFiring;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
                return false;
            }

        }

        public static bool GetBombFiring(GameObject vehicle)
        {

            try
            {
                WeaponManager weaponManager = vehicle.GetComponentInChildren<WeaponManager>();

                if (weaponManager.availableWeaponTypes.bomb)
                {
                    return weaponManager.isFiring;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
                return false;
            }
        }

        public static bool GetMissileFiring(GameObject vehicle)
        {

            try
            {
                WeaponManager weaponManager = vehicle.GetComponentInChildren<WeaponManager>();

                if (weaponManager.availableWeaponTypes.aam || weaponManager.availableWeaponTypes.agm || weaponManager.availableWeaponTypes.antirad || weaponManager.availableWeaponTypes.antiShip)
                {
                    return weaponManager.isFiring;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Support.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
                return false;
            }
        }

        public static string getRadarCrossSection(GameObject vehicle)
        {
            try
            {
                RadarCrossSection rcs = vehicle.GetComponentInChildren<RadarCrossSection>();
                return rcs.GetAverageCrossSection().ToString();
            }
            catch (Exception ex)
            {
                return "False";
            }
        }


        public static bool GetLanded(GameObject vehicle)
        {
            return true;
        }


        public void SendUdp(string text)
        {

            Support.WriteLog($"{Globals.projectName} - Sending UDP Packet: {text} to {dataLogger.receiverIp}");

            byte[] sendBuffer = Encoding.ASCII.GetBytes(text);

            dataLogger.udpClient.Send(sendBuffer, sendBuffer.Length);

        }
    }
}
