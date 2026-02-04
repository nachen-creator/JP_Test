using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandwritingUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Settings")]
    public RawImage displayImage;
    public Button clearButton;
    public int textureWidth = 512;
    public int textureHeight = 512;
    public int penThickness = 3;

    private HandwritingCanvas _canvasLogic;
    private Vector2 _lastPos;

    void Start()
    {
        // 1. Initialize logic
        _canvasLogic = new HandwritingCanvas(textureWidth, textureHeight);
        
        // 2. Link texture to UI
        displayImage.texture = _canvasLogic.DrawingTexture;

        // 3. Setup Clear Button
        clearButton.onClick.AddListener(OnClearClicked);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _lastPos = GetTextureCoords(eventData);
        _canvasLogic.DrawLine(_lastPos, _lastPos, penThickness);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPos = GetTextureCoords(eventData);
        _canvasLogic.DrawLine(_lastPos, currentPos, penThickness);
        _lastPos = currentPos;
    }

    private Vector2 GetTextureCoords(PointerEventData eventData)
    {
        // Convert screen click position to local position on the RawImage
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            displayImage.rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPos);

        // Map localPos to 0-1 range based on Rect
        Rect r = displayImage.rectTransform.rect;
        float x = (localPos.x - r.x) / r.width;
        float y = (localPos.y - r.y) / r.height;

        return new Vector2(x * textureWidth, y * textureHeight);
    }

    private void OnClearClicked()
    {
        _canvasLogic.Clear();
    }
}