using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    private Canvas mainCanvas;
    private GameObject rewardPanel;
    private TextMeshProUGUI rewardTitle;
    private TextMeshProUGUI rewardDesc;
    private Button button1; // Equip / Confirm
    private Button button2; // Discard / Cancel
    private TextMeshProUGUI button1Text;
    private TextMeshProUGUI button2Text;

    private int selectedOption = -1; // -1: waiting, 0: btn1, 1: btn2

    void Awake()
    {
        SetupUI();
    }

    void SetupUI()
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("MainCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Reward Panel
        rewardPanel = CreatePanel(mainCanvas.transform, "RewardPanel", new Color(0, 0, 0, 0.8f));
        rewardPanel.SetActive(false);

        // Title
        rewardTitle = CreateText(rewardPanel.transform, "Title", "REWARD!", 50, new Vector2(0, 150));

        // Description
        rewardDesc = CreateText(rewardPanel.transform, "Desc", "You found...", 30, new Vector2(0, 50));

        // Buttons
        button1 = CreateButton(rewardPanel.transform, "Btn1", "Equip", new Vector2(-100, -100));
        button1Text = button1.GetComponentInChildren<TextMeshProUGUI>();
        button1.onClick.AddListener(() => OnButtonClick(0));

        button2 = CreateButton(rewardPanel.transform, "Btn2", "Discard", new Vector2(100, -100));
        button2Text = button2.GetComponentInChildren<TextMeshProUGUI>();
        button2.onClick.AddListener(() => OnButtonClick(1));
    }

    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Vector2 anchoredPos)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = size;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform rect = txtObj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(600, 100);

        return txt;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(160, 50);

        // Text
        TextMeshProUGUI txt = CreateText(btnObj.transform, "Label", label, 24, Vector2.zero);
        txt.color = Color.black;
        txt.rectTransform.sizeDelta = new Vector2(160, 50);

        return btn;
    }

    void OnButtonClick(int index)
    {
        selectedOption = index;
    }

    public async UniTask<int> ShowRewardPopup(RewardType type, MaskData mask = null)
    {
        rewardPanel.SetActive(true);
        selectedOption = -1;

        if (type == RewardType.NewMask && mask != null)
        {
            rewardTitle.text = "NEW MASK FOUND!";
            rewardDesc.text = $"<b>{mask.name}</b>\n{mask.description}\n\nATK: {mask.atkBonus} | SPD: {mask.moveSpeedBonus}";

            button1.gameObject.SetActive(true);
            button1Text.text = "EQUIP";

            button2.gameObject.SetActive(true);
            button2Text.text = "SALVAGE (+Stats)";
        }
        else if (type == RewardType.UpgradeMask)
        {
            rewardTitle.text = "MASK UPGRADE!";
            rewardDesc.text = "Your current mask has been improved!";

            button1.gameObject.SetActive(true);
            button1Text.text = "GREAT!";

            button2.gameObject.SetActive(false);
        }
        else // Stat Boost
        {
            rewardTitle.text = "STAT BOOST!";
            rewardDesc.text = "You feel stronger permanently.";

            button1.gameObject.SetActive(true);
            button1Text.text = "OK";

            button2.gameObject.SetActive(false);
        }

        // Wait for input
        await UniTask.WaitUntil(() => selectedOption != -1);

        rewardPanel.SetActive(false);
        return selectedOption;
    }
}
