using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading.Tasks;

public class AzureSpeechToText : MonoBehaviour
{
    private string subscriptionKey = "1A0o5uKyKJb0bEtl10R8G8UeaaXCbZ78h4U8B2yjnPxAtGWdXMD7JQQJ99BCACi5YpzXJ3w3AAAYACOGdGyt";
    private string serviceRegion = "northeurope";

    private SpeechRecognizer recognizer;
    private bool isListening = false;

    void Start()
    {
        // Create a config object with your subscription key and region
        var config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);

        // Use the default microphone
        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();

        // Create a speech recognizer
        recognizer = new SpeechRecognizer(config, audioConfig);

        // Subscribe to events
        recognizer.Recognizing += (s, e) =>
        {
            Debug.Log($"Recognizing: {e.Result.Text}");
        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.Log($"Recognized: {e.Result.Text}");
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Debug.LogWarning("No speech could be recognized.");
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            Debug.LogError($"Recognition canceled: {e.Reason}");
            if (e.Reason == CancellationReason.Error)
            {
                Debug.LogError($"Error details: {e.ErrorDetails}");
            }
        };

        recognizer.SessionStopped += (s, e) =>
        {
            Debug.Log("Session stopped.");
            isListening = false;
        };

        StartListening();
    }

    async void StartListening()
    {
        if (!isListening)
        {
            isListening = true;
            Debug.Log("Starting to listen...");
            await recognizer.StartContinuousRecognitionAsync();
        }
    }

    async void OnApplicationQuit()
    {
        if (isListening)
        {
            await recognizer.StopContinuousRecognitionAsync();
        }

        recognizer.Dispose();
    }
}
