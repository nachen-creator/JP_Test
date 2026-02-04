using UnityEngine;

public class HandwritingCanvas
{
    public Texture2D DrawingTexture { get; private set; }
    private Color32[] _blankCanvas;
    private int _width;
    private int _height;
    private Color32 _penColor = Color.black;
    private Color32 _bgColor = Color.white;

    public HandwritingCanvas(int width, int height)
    {
        _width = width;
        _height = height;
    
        // Most OCR plugins prefer RGB24 (3 bytes per pixel) 
        // If your OCR still fails, try TextureFormat.RGBA32
        DrawingTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
    
        // Ensure the texture is set to Point filter for sharper OCR recognition
        DrawingTexture.filterMode = FilterMode.Point;
        DrawingTexture.wrapMode = TextureWrapMode.Clamp;

        _blankCanvas = new Color32[width * height];
        for (int i = 0; i < _blankCanvas.Length; i++)
            _blankCanvas[i] = _bgColor;

        Clear();
    }

    public void Clear()
    {
        DrawingTexture.SetPixels32(_blankCanvas);
        DrawingTexture.Apply();
    }

    // Draws a line between two points to prevent "dotted" lines during fast movement
    public void DrawLine(Vector2 start, Vector2 end, int thickness)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCircle(x0, y0, thickness);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        DrawingTexture.Apply();
    }

    private void DrawCircle(int x, int y, int radius)
    {
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int px = Mathf.Clamp(x + i, 0, _width - 1);
                    int py = Mathf.Clamp(y + j, 0, _height - 1);
                    DrawingTexture.SetPixel(px, py, _penColor);
                }
            }
        }
    }
}