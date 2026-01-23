using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

/// <summary>
/// A sample script to demonstrate real-time speech recognition using OpenAI Whisper in Unity.
/// </summary>
public class WhisperRealtimeDemo : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Reference to the WhisperManager in the scene.")]
    public WhisperManager whisper;
    
    [Tooltip("Reference to the MicrophoneRecord component.")]
    public MicrophoneRecord microphone;
    
    [Header("UI")]
    [Tooltip("Text component to display the transcribed text.")]
    public Text outputText;
    
    [Tooltip("Optional: Text component to display status.")]
    public Text statusText;

    private WhisperStream _stream;

    private async void Start()
    {
        // Validate references
        if (whisper == null || microphone == null)
        {
            Debug.LogError("WhisperManager or MicrophoneRecord not assigned!");
            return;
        }

        try
        {
            if (statusText) statusText.text = "Initializing...";

            // 1. Create a stream instance from the WhisperManager, passing the microphone.
            // This waits for the model to load if it hasn't already.
            _stream = await whisper.CreateStream(microphone);

            if (_stream == null)
            {
                Debug.LogError("Failed to create Whisper stream.");
                return;
            }

            // 2. Subscribe to the stream events to get real-time text updates
            _stream.OnResultUpdated += OnResultUpdated;
            _stream.OnSegmentFinished += OnSegmentFinished;
            
            // 3. Start the microphone recording
            microphone.StartRecord();
            
            // 4. Start the streaming process
            _stream.StartStream();
            
            if (statusText) statusText.text = "Listening...";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting Whisper stream: {e.Message}");
            if (statusText) statusText.text = "Error starting stream";
        }
    }

    /// <summary>
    /// Called when the partial result of the current transcription is updated.
    /// This gives you the full text including the part currently being spoken.
    /// </summary>
    private void OnResultUpdated(string result)
    {
        if (outputText != null)
        {
            outputText.text = result;
        }
    }

    /// <summary>
    /// Called when a segment (sentence/phrase) is finalized.
    /// </summary>
    private void OnSegmentFinished(WhisperResult segment)
    {
        // Useful for debugging or handling completed sentences separately
        Debug.Log($"Segment finished: {segment.Result}");
    }
}