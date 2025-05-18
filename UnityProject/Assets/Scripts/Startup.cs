using UnityEngine;
using System.IO;

[System.Serializable]
public class ConfigData
{
    public bool useSimulator;
    public bool noAudio;
    public bool noSetup;
    public bool rotationDebug;
    public bool shoulderDebug;
    public bool armDebug;
    public float angleThreshold;
    public float moveThreshold;
    public float userArmLength;
    public int updateFrequency;
    public string serverIp;
}

// ------------------------------------------------------------------------ //
// This script is responsible for loading the configuration file at startup //
// ------------------------------------------------------------------------ //

public class Startup : MonoBehaviour
{
    public GameObject TCPSend;
    public GameObject SetupCanvas;
    public GameObject Manager;
    public GameObject Audio;
    public GameObject Simulator;

    private string path;


    void Start()
    {
        // Default directory for the config file (root folder)
        path = Path.GetDirectoryName(Application.dataPath);
        path = Path.Combine(path, "config.json");

        LoadConfig();
    }

    void LoadConfig()
    {
      
        if (!File.Exists(path))
        {
            Debug.LogWarning("Config file not found, creating default config...");

            // Generate config file
            ConfigData defaultConfig = new ConfigData
            {
                useSimulator    = false,
                noAudio         = false,
                noSetup         = true,
                rotationDebug   = false,
                shoulderDebug   = false,
                armDebug        = false,
                angleThreshold  = 0.1f,
                moveThreshold   = 0.1f,
                userArmLength   = 0.40f,
                updateFrequency = 10,
                serverIp        = "127.0.0.1"
            };

            // Create the default config file
            string defJson = JsonUtility.ToJson(defaultConfig, true);
            File.WriteAllText(path, defJson);
        }

        string json = File.ReadAllText(path);
        ConfigData configData = JsonUtility.FromJson<ConfigData>(json);

        // ------------------------------ //
        // Apply the loaded configuration //
        // ------------------------------ //

        // Enable or disable audio module
        if (configData.noAudio)
        {
            Audio.SetActive(false);
            Debug.Log("Audio disabled");
        }
        else
        {
            Audio.SetActive(true);
            Debug.Log("Audio enabled");
        }

        // Enable or disable simulator
        if (configData.useSimulator)
        {
            Simulator.SetActive(true);
            Debug.Log("Simulator enabled");
        }
        else
        {
            Simulator.SetActive(false);
            Debug.Log("Simulator disabled");
        }

        // Enable or disable setup mode
        if (configData.noSetup)
        {
            SetupCanvas.SetActive(false);
            Manager.SetActive(false);
            TCPSend.SetActive(true);
            Debug.Log("Setup mode disabled");
        }
        else
        {
            SetupCanvas.SetActive(true);
            Manager.SetActive(true);
            TCPSend.SetActive(false);
            Debug.Log("Setup mode enabled");
        }

        // Apply the loaded configuration to the TCP sender
        TcpSender TCPSendSettings = TCPSend.GetComponent<TcpSender>();
        if (TCPSendSettings != null)
        {
            TCPSendSettings.noSetup         = configData.noSetup;
            TCPSendSettings.shoulderDebug   = configData.shoulderDebug;
            TCPSendSettings.armDebug        = configData.armDebug;
            TCPSendSettings.rotationDebug   = configData.rotationDebug;
            TCPSendSettings.serverIp        = configData.serverIp;
            TCPSendSettings.updateFrequency = configData.updateFrequency;
            TCPSendSettings.angleThreshold  = configData.angleThreshold;
            TCPSendSettings.moveThreshold   = configData.moveThreshold;
            TCPSendSettings.userArmLength   = configData.userArmLength;

            Debug.Log("Configuration loaded successfully");
        }
    }
}