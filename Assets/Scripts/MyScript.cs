using LLMUnity;
using UnityEngine;

public class MyScript : MonoBehaviour
{
    public LLMAgent llmAgent;
    private void Start()
    {
        Debug.Log("Hello from start");
        Game();
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q))
        {
            llmAgent.CancelRequests();
        }

    }
    void HandleReply(string replySoFar)
    {
        // do something with the reply from the model as it is being produced
        Debug.Log(replySoFar);

    }

    void Game()
    {
        // handle the response as it is being produced

        _ = llmAgent.Chat("Hello, introduce yourself", HandleReply);

  }

    async void GameAsync()
    {
        // or handle the entire response in one go

        string reply = await llmAgent.Chat("Hello bot!");
        Debug.Log(reply);
  }
}