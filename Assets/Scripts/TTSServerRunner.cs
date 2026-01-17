using UnityEngine;
using System.Diagnostics;
using System.IO;

public class TTSServerRunner : MonoBehaviour
{
    [SerializeField] private string pyFileName = "tts_server.py";
    [SerializeField] private string pyFileFolder = "GhostJP";
    [SerializeField] private string venvName = "venv_tts"; // The name of your venv folder inside GhostJP

    // Keep a reference to the process so we can kill it when the game stops
    private Process _serverProcess;

    void Start()
    {
        RunPythonScript(pyFileName, "");
    }

    public void RunPythonScript(string scriptName, string args)
    {
        // 0. Windows Only Check
        if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
        {
            UnityEngine.Debug.LogError("TTS SERVER ERROR: This script is explicitly configured for Windows only.");
            return;
        }

        // 1. Setup paths
        string folderPath = Path.Combine(Application.streamingAssetsPath, pyFileFolder);
        string scriptPath = Path.Combine(folderPath, scriptName);
        string venvPath = Path.Combine(folderPath, venvName);

        // 2. Find the Python Executable (Windows Only)
        // Standard Windows venv structure is always: venv_folder/Scripts/python.exe
        string pythonExePath = Path.Combine(venvPath, "Scripts", "python.exe");

        // 3. Validation
        if (!File.Exists(pythonExePath))
        {
            UnityEngine.Debug.LogError($"[TTS Server] Python executable not found at: {pythonExePath}\n" +
                $"Make sure your virtual environment '{venvName}' is inside '{folderPath}' and contains 'Scripts/python.exe'.");
            return;
        }
        
        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError($"[TTS Server] Python script not found at: {scriptPath}");
            return;
        }

        // 4. Configure the Process
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = pythonExePath;
        startInfo.Arguments = $"\"{scriptPath}\" {args}";
        
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true; 
        startInfo.RedirectStandardError = true;  
        startInfo.CreateNoWindow = true;

        // 5. Run the Process
        _serverProcess = new Process();
        _serverProcess.StartInfo = startInfo;

        // ASYNC OUTPUT READING
        _serverProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log($"[Py]: {e.Data}");
        };
        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError($"[Py Err]: {e.Data}");
        };

        try
        {
            _serverProcess.Start();
            
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log($"TTS Server started using: {pythonExePath}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to launch Python process: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill();
            _serverProcess.Dispose();
            UnityEngine.Debug.Log("TTS Server killed.");
        }
    }
}