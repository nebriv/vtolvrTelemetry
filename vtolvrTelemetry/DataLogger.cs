using System;
using System.Text;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using System.Threading;
using System.IO;

using UnityEngine.SceneManagement;
using Harmony;
using System.Reflection;
using System.Collections;
using Valve.Newtonsoft;

using System.Net.Sockets;
using UnityEngine.Events;
using Valve.Newtonsoft.Json;
using System.Linq.Expressions;

public class FlightInfoLogger
{

    public string StallDetector { get; set; }
    public string MissileDetected { get; set; }
    public string TailHook { get; set; }
    public string Health { get; set; }
    public string Flaps { get; set; }
    public string Brakes { get; set; }
    public string GearState { get; set; }
    public string EjectionState { get; set; }
    public Dictionary<string, string> Lights { get; set; }
    public string Location { get; set; }
    public string RadarState { get; set; }
    public string RadarCrossSection { get; set; }
    public string BatteryLevel { get; set; }
    public List<Dictionary<string, string>> Engines { get; set; }
    public string FuelLevel { get; set; }
    public string FuelBurnRate { get; set; }
    public string FuelDensity { get; set; }
    public Int32 unixTimestamp { get; set; }
    public string Heading { get; set; }
    public string Pitch { get; set; }
    public string AoA { get; set; }
    public string Roll { get; set; }
    public string XAccel { get; set; }
    public string YAccel { get; set; }
    public string ZAccel { get; set; }
    public string Airspeed { get; set; }
    public string PlayerGs { get; set; }
    public string VerticalSpeed { get; set; }
    public string AltitudeASL { get; set; }
    public string Drag { get; set; }
    public string Mass { get; set; }
    public string VehicleName { get; set; }
    public List<Dictionary<string, string>> RWRContacts { get; set; }

    public string CSVHeaders()
    {
        return "Timestamp,VehicleName,Mass,Drag,AltitudeASL,Airspeed,Roll,Pitch,Heading,AoA,XAccel,YAccel,ZAccel,PlayerGs,FuelDensity,FuelBurnRate,FuelLevel,RadarCrossSection,BatteryLevel,Health,Stall";
    }
    public string ToCSV()
    {
        return $"{this.unixTimestamp},{this.VehicleName},{this.Mass},{this.Drag},{this.AltitudeASL},{this.Airspeed},{this.Roll},{this.Pitch},{this.Heading},{this.AoA},{this.XAccel},{this.YAccel},{this.ZAccel},{this.PlayerGs},{this.FuelDensity},{this.FuelBurnRate},{this.FuelLevel},{this.RadarCrossSection},{this.BatteryLevel},{this.Health},{this.StallDetector}";

    }
}

public class vtolvrTelemetry : VTOLMOD
{

    private static string projectName = "vtolvrTelemetry";
    private static string projectAuthor = "nebriv";
    private static string projectVersion = "v1.0";

    private static bool runlogger;
    private static int iterator;
    private static int secondCount;
    private static Settings settings;
    private static UnityAction<string> stringChanged;
    private static UnityAction<int> intChanged;
    private static UnityAction<bool> csvChanged;
    private static string receiverIp = "127.0.0.1";
    private static int receiverPort = 4123;
    private static bool csvEnabled = true;

    private static bool udpEnabled = false;
    private static UnityAction<bool> udpChanged;

    private static UnityAction<bool> jsonChanged;
    private static bool jsonEnabled = true;

    private static UdpClient udpClient;
    private static VTOLAPI vtolmod_api;

    private string DataLogFolder;

    private string csv_path;
    private string json_path;

    private static bool printOutput = false;

    public void WriteLog(string line)
    {
        Debug.Log($"{projectName} - {line}");
    }

    public void WriteErrorLog(string line)
    {
        Debug.LogError($"{projectName} - {line}");
    }

    public override void ModLoaded()
    {
        base.ModLoaded();
        stringChanged += IpChanged;
        settings = new Settings(this);
        settings.CreateCustomLabel("The IP of the system receiving the telemetry stream.");
        settings.CreateCustomLabel("Default = 127.0.0.1");
        settings.CreateStringSetting("Current IP", stringChanged, receiverIp);

        intChanged += PortChanged;
        settings.CreateCustomLabel("The receiverPort to send the telemetry stream to.");
        settings.CreateCustomLabel("Default = 4123");
        settings.CreateIntSetting("Current Port", intChanged, receiverPort);

        udpChanged += udpEnabledChanged;
        settings.CreateCustomLabel("Output telemetry to UDP.");
        settings.CreateCustomLabel("Default = False");
        settings.CreateBoolSetting("Enable UDP Output", udpChanged, udpEnabled);

        csvChanged += csvEnabledChanged;
        settings.CreateCustomLabel("Saves the telemetry log to CSV file in VTOL VR Directory. If this is enabled features such as engine telemetry will not be saved.");
        settings.CreateCustomLabel("Default = False");
        settings.CreateBoolSetting("Enable CSV Output", csvChanged, csvEnabled);

        jsonChanged += jsonEnabledChanged;
        settings.CreateCustomLabel("Saves the telemetry log to JSON file in VTOL VR Directory. If this is enabled features such as engine telemetry will not be saved.");
        settings.CreateCustomLabel("Default = False");
        settings.CreateBoolSetting("Enable JSON Output", jsonChanged, csvEnabled);

        VTOLAPI.CreateSettingsMenu(settings);

        Debug.Log($"{projectName} - Telemetry Mod {projectVersion} by {projectAuthor} loaded!");

    }

    public void IpChanged(string amount)
    {
        receiverIp = amount;
        udpClient = new UdpClient();
        udpClient.Connect(receiverIp, receiverPort);
    }

    public void PortChanged(int amount)
    {
        receiverPort = amount;
        udpClient = new UdpClient();
        udpClient.Connect(receiverIp, receiverPort);
    }

    public void csvEnabledChanged(bool newval)
    {
        csvEnabled = newval;

    }
    public void udpEnabledChanged(bool newval)
    {
        udpEnabled = newval;

    }

    public void jsonEnabledChanged(bool newval)
    {
        jsonEnabled = newval;

    }

    private void Start()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("vtolvrTelemetry.logger.logger");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        udpClient = new UdpClient();
        udpClient.Connect(receiverIp, receiverPort);

        this.WriteLog("Running Startup and Waiting for map load");
        vtolmod_api = VTOLAPI.instance;

        StartCoroutine(WaitForMap());

        System.IO.Directory.CreateDirectory("TelemtryDataLogs");
        System.IO.Directory.CreateDirectory("TelemtryDataLogs\\" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmm"));

        DataLogFolder = "TelemtryDataLogs\\" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmm") + "\\";

        csv_path = @DataLogFolder + "datalog.csv";
        json_path = @DataLogFolder + "datalog.json";

    }

    IEnumerator WaitForMap()
    {
        while (SceneManager.GetActiveScene().buildIndex != 7 || SceneManager.GetActiveScene().buildIndex == 12)
        {
            //Pausing this method till the loader scene is unloaded
            yield return null;
        }

        this.WriteLog("Done waiting map load");
        yield return new WaitForSeconds(5);
        runlogger = true;
    }



    public string cleanString(string input)
    {
        string clean = input.Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("*", "").Replace("\"", "").Replace("?", "").Replace(":", "").Replace("|", "");
        return clean;
    }

    private void FixedUpdate()
    {

        if (iterator < 46)
        {
            iterator++;
        }
        else
        {
            iterator = 0;
            secondCount++;

            if (runlogger)
            {
                if (SceneManager.GetActiveScene().buildIndex != 7 && SceneManager.GetActiveScene().buildIndex != 12)
                {
                    
                    ResetLogger();
                }
            }
        }

        if (runlogger)
        {

            GetData();
        }

    }
    public void ResetLogger()
    {
        runlogger = false;
        udpClient.Close();

        this.WriteLog("Scene end detected. Stopping telemetry");
        
        Start();
    }

    public void GetData()
    {
        bool tookDamage = false;
        FlightInfoLogger f_info = new FlightInfoLogger();
        try
        {
            
            if (printOutput)
            {
                this.WriteLog("Collecting Data...");
            }
            Actor playeractor = FlightSceneManager.instance.playerActor;

            f_info.Heading = Math.Round(playeractor.flightInfo.heading, 2).ToString();
            f_info.Pitch = Math.Round(playeractor.flightInfo.pitch, 2).ToString();
            f_info.Roll = Math.Round(playeractor.flightInfo.roll, 2).ToString();
            f_info.XAccel = Math.Round(playeractor.flightInfo.acceleration.x, 2).ToString();
            f_info.YAccel = Math.Round(playeractor.flightInfo.acceleration.y, 2).ToString();
            f_info.ZAccel = Math.Round(playeractor.flightInfo.acceleration.z, 2).ToString();

            f_info.AoA = Math.Round(playeractor.flightInfo.aoa, 2).ToString();

            Health health = Traverse.Create(playeractor).Field("h").GetValue() as Health;
            f_info.Health = health.currentHealth.ToString();

            f_info.Airspeed = Math.Round(playeractor.flightInfo.airspeed, 2).ToString();
            f_info.PlayerGs = Math.Round(playeractor.flightInfo.playerGs, 2).ToString();
            f_info.VerticalSpeed = Math.Round(playeractor.flightInfo.verticalSpeed, 2).ToString();
            f_info.AltitudeASL = Math.Round(playeractor.flightInfo.altitudeASL, 2).ToString();

            f_info.Drag = Math.Round(playeractor.flightInfo.rb.drag, 2).ToString();

            f_info.Mass = Math.Round(playeractor.flightInfo.rb.mass, 2).ToString();

            GameObject currentVehicle = VTOLAPI.instance.GetPlayersVehicleGameObject();

            f_info.VehicleName = currentVehicle.name;
            f_info.StallDetector = GetStall(currentVehicle);
            f_info.FuelDensity = getFuelDensity(currentVehicle);
            f_info.FuelBurnRate = getFuelBurnRate(currentVehicle);
            f_info.FuelLevel = getFuelLevel(currentVehicle);

            f_info.BatteryLevel = GetBattery(currentVehicle);
            f_info.Engines = GetEngineStats(currentVehicle);

            f_info.RadarCrossSection = getRadarCrossSection(currentVehicle);
            f_info.MissileDetected = getMissileDetected(currentVehicle);
            
            // INOP
            //f_info.Lights = getVehicleLights(currentVehicle);
            
            
            f_info.Flaps = getFlaps(currentVehicle);
            f_info.unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            f_info.Brakes = getBrakes(currentVehicle);
            f_info.EjectionState = getEjectionState(currentVehicle);

            f_info.RadarState = getRadarState(currentVehicle);

            f_info.RWRContacts = getRWRContacts(currentVehicle);

            if (printOutput)
            {
                this.WriteLog(f_info.ToCSV());
            }

        } catch (Exception ex)
        {
            this.WriteErrorLog($"{projectName} - Error getting telemetry data " + ex.ToString());
        }


        if (csvEnabled == true)
        {
            if (printOutput)
            {
                this.WriteLog("Saving CSV");
            }

            if (!File.Exists(csv_path))
            {
                using (StreamWriter sw = File.AppendText(csv_path))
                {
                    sw.WriteLine(f_info.CSVHeaders());
                }
            }

            using (StreamWriter sw = File.AppendText(csv_path))
            {
                sw.WriteLine(f_info.ToCSV());
            }
        }

        if (jsonEnabled == true)
        {
            if (printOutput)
            {
                this.WriteLog("Saving JSON...");
            }

            using (StreamWriter sw = File.AppendText(json_path))
            {
                sw.WriteLine(JsonConvert.SerializeObject(f_info)+"\n");
            }
        }

        if (udpEnabled == true)
        {
            if (printOutput)
            {
                this.WriteLog("Sending UDP Packet...");
            }
            try
            {
                SendUdp(JsonConvert.SerializeObject(f_info));
            } catch (Exception ex)
            {
                Debug.LogError($"{projectName} - Error sending UDP " + ex.ToString());
            }
        }


    }


    private void saveCSV(string msg)
    {

    }

    private List<Dictionary<string, string>> GetEngineStats(GameObject vehicle)
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

    private string getBrakes(GameObject vehicle)
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
    private string getRadarState(GameObject vehicle)
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
    private string getEjectionState(GameObject vehicle)
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

    private List<Dictionary<string, string>> getRWRContacts(GameObject vehicle)
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
            this.WriteErrorLog("Error getting RWR Contacts: " + ex);
        }
        return contacts;

    }

    private string getMissileDetected(GameObject vehicle)
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

    private Dictionary<string, string> getVehicleLights(GameObject vehicle)
    {
        // THIS DOES NOT WORK YET
        try
        {
            ExteriorLightsController lightcontroller = vehicle.GetComponentInChildren<ExteriorLightsController>();
            Dictionary<string, string> lights = new Dictionary<string, string>();

            lights.Add("Landing Lights", lightcontroller.landingLights.ToString());
            lights.Add("Navigation Lights", lightcontroller.navLights.ToString());
            lights.Add("Strobe Lights", lightcontroller.strobeLights.ToString());
            return lights;
        }
        catch (Exception ex)
        {
            Dictionary<string, string> lights = new Dictionary<string, string>();
            this.WriteErrorLog("Error getting lights " + ex.ToString());
            return lights;
        }
    }

    private string getFlaps(GameObject vehicle)
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

    private string GetStall(GameObject vehicle)
    {

        try
        {
            HUDStallWarning warning = vehicle.GetComponentInChildren<HUDStallWarning>();

            String stalling = Traverse.Create(warning).Field("stalling").GetValue() as String;

            return stalling;
        }
        catch (Exception ex)
        {
            this.WriteErrorLog("unable to get stall status: " + ex.ToString());
            return "false";
        }

    }


    private string GetBattery(GameObject vehicle)
    {


        try
        {
            Battery batteryCharge = vehicle.GetComponentInChildren<Battery>();
            string battery = batteryCharge.currentCharge.ToString();
            return battery;
        }
        catch (Exception ex)
        {
            this.WriteErrorLog("unable to get battery status: " + ex.ToString());
            return "false";
        }

    }

    private string getFuelLevel(GameObject vehicle)
    {
        try
        {
            FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
            return tank.totalFuel.ToString();
        }
        catch (Exception ex)
        {
            return "Unavailable";
        }
    }

    private string getFuelBurnRate(GameObject vehicle)
    {

        try
        {
            FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
            return tank.fuelDrain.ToString();
        }
        catch (Exception ex)
        {
            return "Unavailable";
        }

    }

    private string getFuelDensity(GameObject vehicle)
    {
        try
        {
            FuelTank tank = vehicle.GetComponentInChildren<FuelTank>();
            return tank.fuelDensity.ToString();
        }
        catch (Exception ex)
        {
            return "Unavailable";
        }

    }

    private bool GetGunFiring(GameObject vehicle)
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
            this.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
            return false;
        }

    }

    private bool GetBombFiring(GameObject vehicle)
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
            this.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
            return false;
        }
    }

    private bool GetMissileFiring(GameObject vehicle)
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
            this.WriteErrorLog("Unable to get weapon manager status: " + ex.ToString());
            return false;
        }
    }

    private string getRadarCrossSection(GameObject vehicle)
    {
        try
        {
            RadarCrossSection rcs = vehicle.GetComponentInChildren<RadarCrossSection>();
            return rcs.GetAverageCrossSection().ToString();
        }
        catch (Exception ex)
        {
            return "Unavailable";
        }
    }


    private bool GetOnGround(GameObject vehicle)
    {
        return true;
    }

    public void SendUdp(string text)
    {

        this.WriteLog($"{projectName} - Sending UDP Packet: {text} to {receiverIp}");

        byte[] sendBuffer = Encoding.ASCII.GetBytes(text);
        
        udpClient.Send(sendBuffer, sendBuffer.Length);

    }

}