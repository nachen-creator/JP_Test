using UnityEngine;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System;
using System.Collections;
using System.Linq;

public class TTSManager : MonoBehaviour
{
    [Header("Setup")]
    public string pythonExeName = "tts_server.exe";
    public string pythonExeFolder = "tts_server"; // Folder inside StreamingAssets
    public bool showConsole = true;
    public int connectionTimeoutSeconds = 60; // Increased from 10 to 60

    private Process pythonProcess;
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;

    void Start()
    {
        // 1. Kill any stuck instances from previous runs (The "Zombie" Fix)
        KillExistingProcesses();

        // 2. Launch the new server
        LaunchPythonServer();

        // 3. Start connecting
        StartCoroutine(ConnectWithRetry());
    }

    void KillExistingProcesses()
    {
        // This finds any process named 'tts_server' and kills it.
        // Critical for Unity Editor iteration where OnApplicationQuit might not have run perfectly.
        var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(pythonExeName));
        foreach (var p in processes)
        {
            try 
            { 
                UnityEngine.Debug.LogWarning($"Killing zombie process: {p.ProcessName} (ID: {p.Id})");
                p.Kill(); 
                p.WaitForExit(); // Ensure it's dead before we start a new one
            }
            catch (Exception e) { UnityEngine.Debug.LogError($"Could not kill process: {e.Message}"); }
        }
    }

    void LaunchPythonServer()
    {
        string exePath = Path.Combine(Application.streamingAssetsPath, pythonExeFolder, pythonExeName);

        if (!File.Exists(exePath))
        {
            UnityEngine.Debug.LogError($"<color=red>MISSING EXE:</color> Could not find file at {exePath}");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = exePath;
        startInfo.WorkingDirectory = Path.GetDirectoryName(exePath); // Set working dir to exe folder
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = !showConsole; 
        
        // IMPORTANT: If UseShellExecute is false, we can't easily keep the window open on crash 
        // unless the python script has an input() at the end.

        try {
            pythonProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log($"Python Server Launched. PID: {pythonProcess.Id}");
        }
        catch (Exception e) {
            UnityEngine.Debug.LogError($"Failed to launch Python: {e.Message}");
        }
    }

    IEnumerator ConnectWithRetry()
    {
        int attempts = 0;
        
        // Wait up to 'connectionTimeoutSeconds'
        while (!isConnected && attempts < connectionTimeoutSeconds)
        {
            // Check if process died (Crash detection)
            if (pythonProcess != null && pythonProcess.HasExited)
            {
                UnityEngine.Debug.LogError($"<color=red>PYTHON CRASHED:</color> The process exited unexpectedly with code {pythonProcess.ExitCode}.");
                UnityEngine.Debug.LogError("Check the black console window for the Python error trace.");
                yield break; // Stop trying
            }

            yield return new WaitForSeconds(1.0f);
            
            try
            {
                // Attempt connection
                client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", 5000, null, null);
                
                // Wait small amount for TCP handshake
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));

                if (success)
                {
                    client.EndConnect(result);
                    stream = client.GetStream();
                    isConnected = true;
                    UnityEngine.Debug.Log("<color=green>Connected to TTS Engine!</color>");
                    
                    // Initial handshake message
                    Speak("Unity connection established.");
                }
                else
                {
                   throw new SocketException(); // Force catch block
                }
            }
            catch (Exception)
            {
                attempts++;
                if (attempts % 5 == 0) // Log every 5 seconds so we don't spam
                    UnityEngine.Debug.Log($"Waiting for Python engine... ({attempts}/{connectionTimeoutSeconds}s)");
                
                if (client != null) client.Close();
            }
        }

        if (!isConnected)
        {
             UnityEngine.Debug.LogError("Timed out waiting for Python Server.");
        }
    }

    public void Speak(string text)
    {
        if (!isConnected || stream == null) 
        {
            UnityEngine.Debug.LogWarning("Cannot speak - not connected.");
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
            UnityEngine.Debug.LogError("Error sending text: " + e.Message);
            isConnected = false;
        }
    }

    void OnApplicationQuit()
    {
        if (client != null) client.Close();
        
        // Force kill the process
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
        }
    }
}