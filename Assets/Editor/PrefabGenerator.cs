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
        EnsureDirectory("Assets/Resources/Materials");

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
        CreatePrefab("Prefabs/UI/CombatUI", CreateCombatUI);
        CreatePrefab("Prefabs/UI/UnitHUD", CreateUnitHUD);
        CreatePrefab("Prefabs/UI/InventorySlot", CreateInventorySlot);
        CreatePrefab("Prefabs/UI/DetailPanel", CreateDetailPanel);
        CreatePrefab("Prefabs/UI/RewardPanel", CreateRewardPanel);
        CreatePrefab("Prefabs/UI/RewardCard", CreateRewardCard);
        CreatePrefab("Prefabs/UI/ReplacePanel", CreateReplacePanel);
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

    static GameObject CreateCombatUI()
    {
        GameObject go = new GameObject("CombatUI");
        CombatUI ui = go.AddComponent<CombatUI>();
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // --- Top Panel ---
        GameObject topPanel = new GameObject("TopPanel");
        topPanel.transform.SetParent(go.transform, false);
        RectTransform topRt = topPanel.AddComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = new Vector2(1, 1);
        topRt.pivot = new Vector2(0.5f, 1);
        topRt.anchoredPosition = new Vector2(0, 0);
        topRt.sizeDelta = new Vector2(0, 200);

        // 1. HP Bar (Top Left)
        GameObject hpBar = new GameObject("HPBar");
        hpBar.transform.SetParent(topPanel.transform, false);
        Image hpBg = hpBar.AddComponent<Image>();
        hpBg.color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform hpRt = hpBar.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 1);
        hpRt.anchorMax = new Vector2(0, 1);
        hpRt.pivot = new Vector2(0, 1);
        hpRt.anchoredPosition = new Vector2(20, -20);
        hpRt.sizeDelta = new Vector2(500, 40);

        GameObject hpFillObj = new GameObject("Fill");
        hpFillObj.transform.SetParent(hpBar.transform, false);
        Image hpFill = hpFillObj.AddComponent<Image>();
        hpFill.color = Color.red;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRt = hpFillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        ui.hpBarFill = hpFill;

        // HP Text
        ui.hpText = CreateTMP(topPanel.transform, "HPText", "500 / 999", 30, new Vector2(600, -40));
        ui.hpText.alignment = TextAlignmentOptions.Left;

        // Stats Grid
        GameObject grid = new GameObject("StatsGrid");
        grid.transform.SetParent(topPanel.transform, false);
        RectTransform gridRt = grid.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 1);
        gridRt.anchorMax = new Vector2(0, 1);
        gridRt.pivot = new Vector2(0, 1);
        gridRt.anchoredPosition = new Vector2(20, -80);
        gridRt.sizeDelta = new Vector2(1000, 100);

        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(250, 40);
        glg.spacing = new Vector2(20, 10);

        // Helper
        System.Action<string, Color, string> createStat = (name, col, initial) => {
            GameObject item = new GameObject(name);
            item.transform.SetParent(grid.transform, false);

            // Icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(item.transform, false);
            Image img = icon.AddComponent<Image>();
            img.color = col;
            RectTransform iRt = icon.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 0.5f);
            iRt.anchorMax = new Vector2(0, 0.5f);
            iRt.anchoredPosition = new Vector2(20, 0);
            iRt.sizeDelta = new Vector2(30, 30);

            // Text
            TextMeshProUGUI tmp = CreateTMP(item.transform, "Val", initial, 24, new Vector2(100, 0));
            tmp.alignment = TextAlignmentOptions.Left;

            if(name == "Atk") ui.atkText = tmp;
            if(name == "CD") ui.atkCooldownText = tmp;
            if(name == "Range") ui.atkRangeText = tmp;
            if(name == "Def") ui.defText = tmp;
            if(name == "Spd") ui.moveSpeedText = tmp;
            if(name == "KB") ui.knockbackText = tmp;
        };

        createStat("Atk", Color.red, "10.5");
        createStat("CD", Color.yellow, "4s");
        createStat("Range", Color.green, "4.5");
        createStat("Def", Color.gray, "25%");
        createStat("Spd", Color.cyan, "2");
        createStat("KB", new Color(1, 0.5f, 0), "1");


        // --- Inventory Panel (Bottom) ---
        GameObject invPanel = new GameObject("InventoryPanel");
        invPanel.transform.SetParent(go.transform, false);
        Image invBg = invPanel.AddComponent<Image>();
        invBg.color = new Color(0.25f, 0.4f, 0.7f, 1f);

        RectTransform invRt = invPanel.GetComponent<RectTransform>();
        invRt.anchorMin = new Vector2(0, 0);
        invRt.anchorMax = new Vector2(1, 0);
        invRt.pivot = new Vector2(0.5f, 0);
        invRt.anchoredPosition = Vector2.zero;
        invRt.sizeDelta = new Vector2(0, 150);

        GameObject invContainer = new GameObject("Container");
        invContainer.transform.SetParent(invPanel.transform, false);
        RectTransform ctrRt = invContainer.AddComponent<RectTransform>();
        ctrRt.anchorMin = new Vector2(0, 0.5f);
        ctrRt.anchorMax = new Vector2(0, 0.5f);
        ctrRt.pivot = new Vector2(0, 0.5f);
        ctrRt.anchoredPosition = new Vector2(50, 0);
        ctrRt.sizeDelta = new Vector2(800, 120);

        HorizontalLayoutGroup hlg = invContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        ui.inventoryContainer = invContainer.transform;

        return go;
    }

    static GameObject CreateUnitHUD()
    {
        GameObject go = new GameObject("UnitHUD");
        UnitHUD hud = go.AddComponent<UnitHUD>();

        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, 0.5f);

        // HP Bar
        GameObject hpBar = new GameObject("HPBar");
        hpBar.transform.SetParent(go.transform, false);
        Image hpBg = hpBar.AddComponent<Image>();
        hpBg.color = Color.gray;
        RectTransform hpRt = hpBar.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 0.5f);
        hpRt.anchorMax = new Vector2(1, 1);
        hpRt.offsetMin = Vector2.zero;
        hpRt.offsetMax = Vector2.zero;

        GameObject hpFillObj = new GameObject("Fill");
        hpFillObj.transform.SetParent(hpBar.transform, false);
        Image hpFill = hpFillObj.AddComponent<Image>();
        hpFill.color = Color.red;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform hpFillRt = hpFillObj.GetComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = Vector2.zero;
        hpFillRt.offsetMax = Vector2.zero;

        // Use reflection to set private field
        hud.GetType().GetField("hpFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hud, hpFill);

        // Attack Gauge
        GameObject atkBar = new GameObject("AtkBar");
        atkBar.transform.SetParent(go.transform, false);
        Image atkBg = atkBar.AddComponent<Image>();
        atkBg.color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform atkRt = atkBar.GetComponent<RectTransform>();
        atkRt.anchorMin = new Vector2(0, 0);
        atkRt.anchorMax = new Vector2(1, 0.5f);
        atkRt.offsetMin = Vector2.zero;
        atkRt.offsetMax = Vector2.zero;

        GameObject atkFillObj = new GameObject("Fill");
        atkFillObj.transform.SetParent(atkBar.transform, false);
        Image atkFill = atkFillObj.AddComponent<Image>();
        atkFill.color = Color.white;
        atkFill.type = Image.Type.Filled;
        atkFill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform atkFillRt = atkFillObj.GetComponent<RectTransform>();
        atkFillRt.anchorMin = Vector2.zero;
        atkFillRt.anchorMax = Vector2.one;
        atkFillRt.offsetMin = Vector2.zero;
        atkFillRt.offsetMax = Vector2.zero;

        hud.GetType().GetField("attackFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hud, atkFill);

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
