using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

// ------------------------------------------------------------------------------------ //
// This script receives audio data over UDP and plays it using an AudioSource component //
// ------------------------------------------------------------------------------------ //

public class ReceiveSound : MonoBehaviour
{
    private static AudioSource audioSource;
    private static ConcurrentQueue<float[]> AUDIO_QUEUE = new ConcurrentQueue<float[]>();
    private static int PORT = 55001;
    private static int SAMPLE_RATE = 16000;
    private static bool isRunning = true;
    private Thread AudioRetriever;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Opened UDP connection on port " + PORT);
        AudioRetriever = new Thread(new ThreadStart(ReceiveAudio));
        AudioRetriever.Start();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 4.0f;

    }


    static float[] getFloatArray(UdpClient udpServer, IPEndPoint groupEP)
    {

       
        var data = udpServer.Receive(ref groupEP);
        string stringData = Encoding.UTF8.GetString(data);
        string stringNumbers = Regex.Replace(stringData, "[ ]+", " ")
            .Trim('[', ']');

        float[] floatNumbers = Regex.Replace(stringNumbers, @"^\s|\s+$", "")
            .Split(' ')
            .Select(num => float.Parse(num, CultureInfo.InvariantCulture))
            .ToArray();
        return floatNumbers;
    }
    
    static void ReceiveAudio()
    {
        UdpClient udpServer = new UdpClient(PORT);
        udpServer.Client.ReceiveTimeout = 1000; // Set a timeout for receiving data
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, PORT);
       
        while (isRunning)
        {
            try
            {
                float[] samples = getFloatArray(udpServer, groupEP);
                AUDIO_QUEUE.Enqueue(samples);
            }
            catch (SocketException ex)
            {
                if(ex.SocketErrorCode != SocketError.TimedOut)
                {
                    Debug.LogError("SocketException: " + ex.Message);
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Receive error: " + ex.Message);
            }
        }
        // Close the UDP connection when done
        udpServer.Close();
    }

    void Update()
    {
        if (AUDIO_QUEUE.TryDequeue(out float[] samples))
        {
            
            audioSource.clip = AudioClip.Create("ReceivedAudio", samples.Length, 1, SAMPLE_RATE, false);
            audioSource.clip.SetData(samples, 0);
            audioSource.Play(); 
        }

    }

    private void OnApplicationQuit()
    {
        isRunning = false;

        if (AudioRetriever != null && AudioRetriever.IsAlive)
        {
            AudioRetriever.Join(); // Now it can exit cleanly
        }
    }

}