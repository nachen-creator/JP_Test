using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;
using PixelSquare;
using TMPro;

public class LLMManager : MonoBehaviour
{
    [SerializeField] private LLMAgent llmAgent;
    [SerializeField] private TextMeshProUGUI textMesh;
    
    private bool warmUpDone = false;
    private string currentResponse = "";

    void OnEnable()
    {
        TesseractOCRTextureDemo.NewTextDetected += HandleNewTextInput;
    }

    void OnDisable()
    {
        TesseractOCRTextureDemo.NewTextDetected -= HandleNewTextInput;
    }
    
    void Start()
    {
        _ = llmAgent.Warmup(WarmUpCallback);
    }

    void HandleNewTextInput(string message)
    {
        if (!warmUpDone) return;
        Debug.Log($"Hand-writing result: {message}");
        
        Task chatTask = llmAgent.Chat(message, 
            (response) =>
            {
                currentResponse = response;
            }, 
            HandleLLMResponseCompleted);
    }

    private void HandleLLMResponseCompleted()
    {
        textMesh.text = $"JP: {currentResponse}";
    }
    
    public void WarmUpCallback()
    {
        warmUpDone = true;
    }
}
