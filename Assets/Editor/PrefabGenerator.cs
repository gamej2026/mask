using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using TMPro;

public class PrefabGenerator
{
    [MenuItem("Tools/Generate Missing Prefabs")]
    public static void Generate()
    {
        // Ensure directories
        EnsureDirectory("Assets/Resources/Prefabs/Managers");
        EnsureDirectory("Assets/Resources/Prefabs/Environment");
        EnsureDirectory("Assets/Resources/Prefabs/Units");
        EnsureDirectory("Assets/Resources/Prefabs/UI");
        EnsureDirectory("Assets/Resources/Prefabs/Projectiles");

        // 1. Managers
        CreatePrefab("Prefabs/Managers/GameManager", CreateGameManager);
        CreatePrefab("Prefabs/Managers/UIManager", CreateUIManager);

        // 2. Environment
        CreatePrefab("Prefabs/Environment/MainCamera", CreateMainCamera);
        CreatePrefab("Prefabs/Environment/DirectionalLight", CreateDirectionalLight);
        CreatePrefab("Prefabs/Environment/Ground", CreateGround);
        CreatePrefab("Prefabs/Environment/Tree", CreateTree);

        // 3. Units
        CreatePrefab("Prefabs/Units/Player", CreatePlayer);
        CreatePrefab("Prefabs/Units/Monster", CreateMonster);

        // 4. UI
        CreatePrefab("Prefabs/UI/MainCanvas", CreateMainCanvas);
        CreatePrefab("Prefabs/UI/InventoryPanel", CreateInventoryPanel);
        CreatePrefab("Prefabs/UI/InventorySlot", CreateInventorySlot);
        CreatePrefab("Prefabs/UI/DetailPanel", CreateDetailPanel);
        CreatePrefab("Prefabs/UI/RewardPanel", CreateRewardPanel);
        CreatePrefab("Prefabs/UI/RewardCard", CreateRewardCard);
        CreatePrefab("Prefabs/UI/ReplacePanel", CreateReplacePanel);
        CreatePrefab("Prefabs/UI/HealthText", CreateHealthText);
        CreatePrefab("Prefabs/UI/PopupText", CreatePopupText);

        // 5. Projectiles
        CreatePrefab("Prefabs/Projectiles/Projectile", CreateProjectile);

        AssetDatabase.Refresh();
        Debug.Log("Prefab Generation Complete!");
    }

    // Helper Methods
    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    static void CreatePrefab(string path, System.Func<GameObject> creator)
    {
        string fullPath = "Assets/Resources/" + path + ".prefab";
        if (File.Exists(fullPath))
        {
            Debug.Log($"Prefab already exists: {fullPath}");
            return;
        }

        GameObject obj = creator();
        if (obj != null)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, fullPath);
            Object.DestroyImmediate(obj);
            Debug.Log($"Created Prefab: {fullPath}");
        }
    }

    static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/Resources/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null) mat.shader = urpShader;

            mat.color = color;
            if (urpShader != null) mat.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // Creators

    static GameObject CreateGameManager()
    {
        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        return go;
    }

    static GameObject CreateUIManager()
    {
        GameObject go = new GameObject("UIManager");
        go.AddComponent<UIManager>();
        return go;
    }

    static GameObject CreateMainCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.8f, 0.92f);
        go.AddComponent<AudioListener>();
        go.transform.position = new Vector3(0, 0, -10);
        return go;
    }

    static GameObject CreateDirectionalLight()
    {
        GameObject go = new GameObject("Directional Light");
        Light l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        go.transform.rotation = Quaternion.Euler(50, -30, 0);
        return go;
    }

    static GameObject CreateGround()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Ground";
        go.transform.position = new Vector3(0, -5.5f, 0);
        go.transform.localScale = new Vector3(1000, 10, 10);
        go.GetComponent<Renderer>().material = GetMaterial("GroundMat", new Color(0.2f, 0.6f, 0.2f));
        return go;
    }

    static GameObject CreateTree()
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Tree";
        trunk.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
        trunk.GetComponent<Renderer>().material = GetMaterial("TrunkMat", new Color(0.4f, 0.2f, 0.1f));

        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Leaves";
        leaves.transform.SetParent(trunk.transform);
        // Calculated local position and scale based on UIManager fallback logic
        leaves.transform.localPosition = new Vector3(0, 1.25f, 0);
        leaves.transform.localScale = new Vector3(5f, 1.25f, 5f);

        leaves.GetComponent<Renderer>().material = GetMaterial("LeavesMat", new Color(0.1f, 0.5f, 0.1f));

        return trunk;
    }

    static GameObject CreatePlayer()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Player";
        go.AddComponent<Unit>();
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        return go;
    }

    static GameObject CreateMonster()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Monster";
        go.AddComponent<Unit>();
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        return go;
    }

    static GameObject CreateMainCanvas()
    {
        GameObject go = new GameObject("MainCanvas");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreateInventoryPanel()
    {
        GameObject go = new GameObject("InventoryPanel");
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 120);
        go.AddComponent<HorizontalLayoutGroup>().spacing = 20;
        return go;
    }

    static GameObject CreateInventorySlot()
    {
        GameObject go = new GameObject("InventorySlot");
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
        go.AddComponent<InventorySlotHandler>();

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(go.transform, false);
        icon.AddComponent<Image>().color = Color.clear;
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);

        GameObject hl = new GameObject("Highlight");
        hl.transform.SetParent(go.transform, false);
        Image hlImg = hl.AddComponent<Image>();
        hlImg.color = Color.yellow;
        hlImg.raycastTarget = false;
        hl.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 110);
        hl.SetActive(false);

        CreateTMP(go.transform, "Hotkey", "Q", 20, new Vector2(-35, 35));
        CreateTMP(go.transform, "Cost", "0", 20, new Vector2(35, -35));

        return go;
    }

    static GameObject CreateDetailPanel()
    {
        GameObject go = new GameObject("DetailPanel");
        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.9f);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 300);

        CreateTMP(go.transform, "Name", "Mask Name", 36, new Vector2(0, 60));
        CreateTMP(go.transform, "Desc", "Stats...", 22, new Vector2(0, -50));

        return go;
    }

    static GameObject CreateRewardPanel()
    {
        GameObject go = new GameObject("RewardPanel");
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.9f);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920, 1080);

        CreateTMP(go.transform, "Title", "CHOOSE YOUR REWARD", 60, new Vector2(0, 400));

        GameObject container = new GameObject("Container");
        container.transform.SetParent(go.transform, false);
        RectTransform cRect = container.AddComponent<RectTransform>();
        cRect.sizeDelta = new Vector2(1400, 600);
        container.AddComponent<HorizontalLayoutGroup>().spacing = 50;

        return go;
    }

    static GameObject CreateRewardCard()
    {
        GameObject go = new GameObject("RewardCard");
        go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().preferredWidth = 400;
        go.GetComponent<LayoutElement>().preferredHeight = 550;

        CreateTMP(go.transform, "Title", "Title", 40, new Vector2(0, 200));
        CreateTMP(go.transform, "Desc", "Desc", 24, new Vector2(0, -50));

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(go.transform, false);
        icon.AddComponent<Image>();
        icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 150);

        return go;
    }

    static GameObject CreateReplacePanel()
    {
        GameObject go = new GameObject("ReplacePanel");
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.95f);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);

        CreateTMP(go.transform, "Title", "INVENTORY FULL!", 50, new Vector2(0, 300));
        CreateTMP(go.transform, "Sub", "Select a mask to discard...", 30, new Vector2(0, 200));

        GameObject container = new GameObject("Container");
        container.transform.SetParent(go.transform, false);
        container.AddComponent<RectTransform>().sizeDelta = new Vector2(1000, 300);
        container.AddComponent<HorizontalLayoutGroup>().spacing = 30;

        GameObject btn = new GameObject("CancelBtn");
        btn.transform.SetParent(go.transform, false);
        btn.AddComponent<Image>();
        btn.AddComponent<Button>();
        btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);
        btn.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        CreateTMP(btn.transform, "Label", "CANCEL", 24, Vector2.zero).color = Color.black;

        return go;
    }

    static GameObject CreateHealthText()
    {
        GameObject go = new GameObject("HealthText");
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 5;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "HP";
        return go;
    }

    static GameObject CreatePopupText()
    {
        GameObject go = new GameObject("PopupText");
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "Pop";
        return go;
    }

    static GameObject CreateProjectile()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.AddComponent<Projectile>();
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        go.GetComponent<Collider>().isTrigger = true;
        return go;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        go.GetComponent<RectTransform>().anchoredPosition = pos;
        return tmp;
    }
}
