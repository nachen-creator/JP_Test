using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections;
using LLMUnitySamples;

public class TTSClient : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    public bool connectOnStart = true;
    public int retryAttempts = 5;

    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;

    void Start()
    {
        if (connectOnStart)
        {
            StartCoroutine(ConnectToServer());
        }
    }

    void OnEnable()
    {
        ChatBot.ResponseCompletedCallback += HandleResponseCompleted;
    }

    void OnDisable()
    {
        ChatBot.ResponseCompletedCallback -= HandleResponseCompleted;
    }

    public IEnumerator ConnectToServer()
    {
        int currentAttempt = 0;

        while (!isConnected && currentAttempt < retryAttempts)
        {
            bool success = false;

            // 1. Attempt Connection (No yield returns here)
            try
            {
                client = new TcpClient();
                var result = client.BeginConnect(serverIP, serverPort, null, null);
                
                // Wait 1 second for handshake
                bool connected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                if (connected)
                {
                    client.EndConnect(result);
                    stream = client.GetStream();
                    isConnected = true;
                    success = true;
                }
                else
                {
                    client.Close();
                }
            }
            catch (Exception)
            {
                // Just catch the error, don't yield here
                success = false;
            }

            // 2. Handle Outcome (Yield return happens here, outside try/catch)
            if (success)
            {
                UnityEngine.Debug.Log($"<color=green>Connected to Python TTS at {serverIP}:{serverPort}</color>");
                Speak("Unity Connected.");
            }
            else
            {
                currentAttempt++;
                UnityEngine.Debug.LogWarning($"Connection attempt {currentAttempt}/{retryAttempts} failed. Is Python running? Retrying in 2s...");
                
                // Cleanup before waiting
                if (client != null) client.Close();
                
                // Wait before retrying
                yield return new WaitForSeconds(2.0f);
            }
        }

        if (!isConnected)
        {
            UnityEngine.Debug.LogError("<color=red>Failed to connect.</color> Make sure 'tts_server.py' is running in a separate terminal.");
        }
    }

    public void Speak(string text)
    {
        if (!isConnected || stream == null) 
        {
            UnityEngine.Debug.LogWarning("Cannot speak: Not connected to server.");
            return;
        }

        try
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] lengthBytes = BitConverter.GetBytes(textBytes.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);

            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(textBytes, 0, textBytes.Length);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error sending text: {e.Message}");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        isConnected = false;
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    [ContextMenu("Test")]
    public void TestSpeech()
    {
        Speak("Hi, my name is Joseph Priestley.");
    }

    [ContextMenu("Connect")]
    public void Connect()
    {
        StartCoroutine(ConnectToServer());
    }

    private void HandleResponseCompleted(string response)
    {
        Speak(response);
    }
}