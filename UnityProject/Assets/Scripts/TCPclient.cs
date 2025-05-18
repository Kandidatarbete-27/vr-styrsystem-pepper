// Imports necessary for the script
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using UnityEngine;
using System.Threading;
using System;
using UnityEngine.UI;

// ---------------------------------------------------------------------------------------------------- //
// This script handles receiving video data over TCP and displaying it in a Unity UI RawImage component //
// ---------------------------------------------------------------------------------------------------- //

public class TcpVideoReceiver : MonoBehaviour
{   // Creates a thread to stop stop unity from freezing waiting to execute commands, and sets up TCP
    Thread thread;
    TcpListener server;
    TcpClient client;

    // Public objects that can be altered through Unity's inspector
    public int connectionPort = 55000;
    public RawImage rawImage;

    // Setting up two useful variables, one that indicates that the program is running, and the other stores the most recent image
    private bool running;
    private byte[] latestImageData;

    // Creates a queue for main thread actions, Unity does not allow modifying UI/gameobjects from background threads
    private ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    public RenderTexture renderTexture; // Public reference to the RenderTexture

    private Texture2D cachedTexture;
    private RenderTexture tempRenderTexture;
    private int lastImageWidth = 0;
    private int lastImageHeight = 0;


    // Start is called first
    void Start()
    {
        //Prints what network port Unity is listening on
        Debug.Log("TcpReceiver Start");
        Debug.Log("Listening on port " + connectionPort);

        // Starts receiving on a seperate thread
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start();
    }

    void GetData()
    {
        // Creates the TCP server 
        server = new TcpListener(IPAddress.Any, connectionPort);
        server.Start();

        // Create a client to get the data stream
        client = server.AcceptTcpClient();
        client.NoDelay = true;

        // Starts receiving and converting data to pictures
        running = true;
        while (running)
        {
            CreateImage();
        }
        // Stops the server
        server.Stop();
    }

    void CreateImage()
    {
        try
        {
            // Reads data that has been sent and creates a lengthbuffer
            NetworkStream nwStream = client.GetStream();
            byte[] lengthBuffer = new byte[4];

            // Checks that the length of the data is correct 
            int bytesRead = nwStream.Read(lengthBuffer, 0, 4);
            if (bytesRead != 4)
            {
                Debug.LogError("Failed to read frame length");
                return;
            }

            // Convert from big-endian to little-endian (needed for BitConverter)
            Array.Reverse(lengthBuffer); // Reverse byte order
            int frameLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] buffer = new byte[frameLength];

            // Reads the frame data
            int bytesReadTotal = 0;
            while (bytesReadTotal < frameLength)
            {
                int read = nwStream.Read(buffer, bytesReadTotal, frameLength - bytesReadTotal);
                if (read == 0)
                {
                    Debug.LogError("Connection closed before receiving full frame");
                    return;
                }
                bytesReadTotal += read;
            }



            // Saves the image and calls UpdateCanvas for main thread
            latestImageData = buffer;
            mainThreadQueue.Enqueue(UpdateCanvas);
        }

        // If something goes wrong, this stops the process
        catch (Exception e)
        {
            Debug.LogError("Connection error" + e.Message);
            StopReceiving();
        }

    }

    void StopReceiving()
    {
        // Stops trying to create images
        if (running)
        {
            running = false;
            Debug.Log("Stopping TCP receiver...");
        }

        // Closes the client connection
        if (client != null)
        {
            client.Close();
            client.Dispose();
            client = null;
        }

        // Stops the server
        if (server != null)
        {
            server.Stop();
            server = null;
        }
        // Kill the thread if needed
        if (thread != null && thread.IsAlive)
        {
            thread.Abort();
            thread = null;
        }
    }


    void UpdateCanvas()
    {
        if (latestImageData == null || renderTexture == null)
        {
            Debug.LogError("No image data or RenderTexture to update");
            return;
        }

        // Get image dimensions by loading into texture (only once)
        if (cachedTexture == null)
            cachedTexture = new Texture2D(2, 2);

        if (!cachedTexture.LoadImage(latestImageData))
        {
            Debug.LogError("Failed to load image data into texture");
            return;
        }

        int width = cachedTexture.width;
        int height = cachedTexture.height;

        // Check if size changed, and recreate only if necessary
        if (width != lastImageWidth || height != lastImageHeight)
        {
            if (tempRenderTexture != null)
                Destroy(tempRenderTexture);

            tempRenderTexture = new RenderTexture(width, height, 24);
            lastImageWidth = width;
            lastImageHeight = height;
        }

        // Set temp render target and blit to both
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = tempRenderTexture;
        Graphics.Blit(cachedTexture, tempRenderTexture);

        Graphics.Blit(tempRenderTexture, renderTexture);
        RenderTexture.active = currentRT;
    }



    // Update checks the queue and if not empty, calls a command sent from CreateImage() 
    void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            action.Invoke();
        }
    }
    //When the application is quit through Unity interface, teh StopReceiving() function is called
    private void OnApplicationQuit()
    {
        StopReceiving();
    }
}