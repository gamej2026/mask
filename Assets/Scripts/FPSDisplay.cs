using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    private TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;
    private float updateInterval = 0.5f;
    private float timeSinceLastUpdate = 0.0f;

    void Awake()
    {
        // Create FPS text UI element
        GameObject fpsObject = new GameObject("FPS Text");
        fpsObject.transform.SetParent(transform, false);
        
        fpsText = fpsObject.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize = 24;
        fpsText.color = Color.green;
        fpsText.alignment = TextAlignmentOptions.TopRight;
        
        RectTransform rectTransform = fpsText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-10, -10);
        rectTransform.sizeDelta = new Vector2(150, 50);
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            float fps = 1.0f / deltaTime;
            fpsText.text = string.Format("FPS: {0:0}", fps);
            
            // Change color based on FPS
            if (fps >= 50)
                fpsText.color = Color.green;
            else if (fps >= 30)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.red;
            
            timeSinceLastUpdate = 0.0f;
        }
    }
}
