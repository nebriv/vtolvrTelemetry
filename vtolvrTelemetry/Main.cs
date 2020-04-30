using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;
using System.Reflection;
using System.Collections;
using System.Net.Sockets;
using UnityEngine.Events;



namespace vtolvrTelemetry
{

    static class Globals
    {

        public static string projectName = "vtolvrTelemetry";
        public static string projectAuthor = "nebriv";
        public static string projectVersion = "v1.0";

    }

    public class vtolvrTelemetry : VTOLMOD
    {

        public bool runlogger;
        public int iterator;
        public int secondCount;
        public Settings settings;
        public UnityAction<string> stringChanged;
        public UnityAction<int> intChanged;
        public UnityAction<bool> csvChanged;
        public string receiverIp = "127.0.0.1";
        public int receiverPort = 4123;
        public bool csvEnabled = true;

        public bool udpEnabled = false;
        public UnityAction<bool> udpChanged;

        public UnityAction<bool> jsonChanged;
        public bool jsonEnabled = true;

        public UdpClient udpClient;
        public VTOLAPI vtolmod_api;

        public string DataLogFolder;

        public string csv_path;
        public string json_path;

        public bool printOutput = false;

        public DataGetters dataGetter;

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

            Debug.Log($"{Globals.projectName} - Telemetry Mod {Globals.projectVersion} by {Globals.projectAuthor} loaded!");
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

        public void Start()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("vtolvrTelemetry.logger.logger");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            udpClient = new UdpClient();
            udpClient.Connect(receiverIp, receiverPort);

            Support.WriteLog("Running Startup and Waiting for map load");
            vtolmod_api = VTOLAPI.instance;

            StartCoroutine(WaitForMap());

            System.IO.Directory.CreateDirectory("TelemtryDataLogs");
            System.IO.Directory.CreateDirectory("TelemtryDataLogs\\" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmm"));

            DataLogFolder = "TelemtryDataLogs\\" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmm") + "\\";

            csv_path = @DataLogFolder + "datalog.csv";
            json_path = @DataLogFolder + "datalog.json";

            dataGetter = new DataGetters(this);

        }

        IEnumerator WaitForMap()
        {
            while (SceneManager.GetActiveScene().buildIndex != 7 || SceneManager.GetActiveScene().buildIndex == 12)
            {
                //Pausing this method till the loader scene is unloaded
                yield return null;
            }

            Support.WriteLog("Done waiting map load");
            yield return new WaitForSeconds(5);
            runlogger = true;
        }


        public string cleanString(string input)
        {
            string clean = input.Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("*", "").Replace("\"", "").Replace("?", "").Replace(":", "").Replace("|", "");
            return clean;
        }

        public void FixedUpdate()
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
                try
                {
                    dataGetter.GetData();
                }
                catch (Exception ex)
                {
                    Support.WriteErrorLog("Error getting data." + ex.ToString());
                }
                
            }

        }
        public void ResetLogger()
        {
            runlogger = false;
            udpClient.Close();

            Support.WriteLog("Scene end detected. Stopping telemetry");

            Start();
        }



        public void GetAllChildrenComponents(GameObject g_object)
        {
            Component[] components = g_object.GetComponentsInChildren<Component>(true);
            foreach (Component comp in components)
            {
                Support.WriteLog(comp.ToString());
            }
            Debug.Log("");

        }

    }
}