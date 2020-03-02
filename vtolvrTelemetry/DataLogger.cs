using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;
using System.Reflection;
using System.Collections;
using System.Net.Sockets;
using UnityEngine.Events;

public class vtolvrTelemetry : VTOLMOD
{

    private static string projectName = "vtolvrTelemetry";
    private static string projectAuthor = "nebriv";
    private static string projectVersion = "v1.0";

    private static bool runlogger;
    private static int iterator;
    private static Settings settings;
    private static UnityAction<string> stringChanged;
    private static UnityAction<int> intChanged;
    private static string receiverIp = "127.0.0.1";
    private static int receiverPort = 4123;
    private static UdpClient udpClient;
    private static VTOLAPI vtolmod_api;

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
        VTOLAPI.CreateSettingsMenu(settings);

        Debug.Log($"{projectName} - Telemetry Mod {projectVersion} by {projectAuthor} loaded!");

    }

    public void IpChanged(string amount)
    {
        receiverIp = amount;
    }

    public void PortChanged(int amount)
    {
        receiverPort = amount;
    }

    private void Start()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("vtolvrTelemetry.logger.logger");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        udpClient = new UdpClient();
        udpClient.Connect(receiverIp, receiverPort);

        Debug.Log($"{projectName} - Running Startup and Waiting for map load");
        vtolmod_api = VTOLAPI.instance;

        StartCoroutine(WaitForMap());

    }

    IEnumerator WaitForMap()
    {
        while (SceneManager.GetActiveScene().buildIndex != 7 || SceneManager.GetActiveScene().buildIndex == 12)
        {
            //Pausing this method till the loader scene is unloaded
            yield return null;
        }

        Debug.Log($"{projectName} - Done waiting map load");
        yield return new WaitForSeconds(5);
        runlogger = true;
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

        Debug.Log($"{projectName} - Scene end detected. Stopping telemetry");
        
        Start();
    }

    public void GetData()
    {
        string msg = "";
        bool tookDamage = false;
        try
        {
            Actor playeractor = FlightSceneManager.instance.playerActor;

            string heading = Math.Round(playeractor.flightInfo.heading, 2).ToString();
            string pitch = Math.Round(playeractor.flightInfo.pitch, 2).ToString();
            string roll = Math.Round(playeractor.flightInfo.roll, 2).ToString();
            string xAccel = Math.Round(playeractor.flightInfo.acceleration.x, 2).ToString();
            string yAccel = Math.Round(playeractor.flightInfo.acceleration.y, 2).ToString();
            string zAccel = Math.Round(playeractor.flightInfo.acceleration.z, 2).ToString();

            //Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            //Health health = Traverse.Create(playeractor).Field("h").GetValue() as Health;
            

            //if (health.currentHealth != lastHealth)
            //{
            //    tookDamage = true;
            //    lastHealth = health.currentHealth;
            //}

            //string stalling = getStall(currentVehicle);

            //bool gun_firing = gunFiring(currentVehicle);

            // FORMAT
            //S:heading:pitch:roll:x_accel:y_accel:z_accel:E
            msg = $"S:{heading}:{pitch}:{roll}:{xAccel}:{yAccel}:{zAccel}:E";
            

        } catch (Exception ex)
        {
            Debug.LogError($"{projectName} - Error getting telemetry data " + ex.ToString());
        }

        try
        {
            SendUdp(msg);
        } catch (Exception ex)
        {
            Debug.LogError($"{projectName} - Error sending UDP " + ex.ToString());
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
            Debug.LogError("unable to get stall status: " + ex.ToString());
            return "false";
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
            Debug.LogError("Unable to get weapon manager status: " + ex.ToString());
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
            Debug.LogError("Unable to get weapon manager status: " + ex.ToString());
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
            Debug.LogError("Unable to get weapon manager status: " + ex.ToString());
            return false;
        }
    }

    private bool GetOnGround(GameObject vehicle)
    {
        return true;
    }

    public void SendUdp(string text)
    {

        Debug.Log($"{projectName} - Sending UDP Packet: {text} to {receiverIp}");

        byte[] sendBuffer = Encoding.ASCII.GetBytes(text);
        
        udpClient.Send(sendBuffer, sendBuffer.Length);

    }

}