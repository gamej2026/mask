using UnityEngine;
using UnityEditor;

/// <summary>
/// PrefabSaverEditor 기능 테스트를 위한 에디터 유틸리티
/// Editor utility for testing PrefabSaverEditor functionality
/// </summary>
public class PrefabSaverEditorTest
{
    /// <summary>
    /// 테스트용 런타임 GameObject 생성
    /// Creates a test runtime GameObject
    /// </summary>
    [MenuItem("Tools/Test/Create Test GameObject")]
    private static void CreateTestGameObject()
    {
        // 테스트용 GameObject 생성
        // Create test GameObject
        GameObject testObj = new GameObject("TestRuntimeObject");
        
        // Cube 추가
        // Add cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.SetParent(testObj.transform);
        cube.transform.localPosition = Vector3.zero;
        
        // 색상 설정
        // Set color
        var renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material to avoid modifying the shared default material
            Material mat = new Material(renderer.sharedMaterial);
            mat.color = new Color(Random.value, Random.value, Random.value);
            renderer.sharedMaterial = mat;
        }
        
        // 자식 오브젝트 추가 (구)
        // Add child object (sphere)
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere";
        sphere.transform.SetParent(testObj.transform);
        sphere.transform.localPosition = new Vector3(0, 1.5f, 0);
        sphere.transform.localScale = Vector3.one * 0.5f;
        
        // 생성된 오브젝트 선택
        // Select created object
        Selection.activeGameObject = testObj;
        
        Debug.Log("[PrefabSaverTest] Test GameObject created. You can now use 'Tools/Save Selected as Prefab' to save it.");
        
        // 사용 안내 다이얼로그
        // Usage guide dialog
        EditorUtility.DisplayDialog(
            "테스트 오브젝트 생성됨 / Test Object Created",
            "테스트 GameObject가 생성되었습니다.\n\n사용 방법:\n" +
            "1. Hierarchy에서 오브젝트가 선택되어 있는지 확인\n" +
            "2. 메뉴: Tools > Save Selected as Prefab 실행\n" +
            "   또는 단축키: Ctrl+Shift+S (Windows) / Cmd+Shift+S (Mac)\n" +
            "3. 저장 위치와 이름 지정\n\n" +
            "Test GameObject has been created.\n\n" +
            "How to use:\n" +
            "1. Make sure the object is selected in Hierarchy\n" +
            "2. Menu: Tools > Save Selected as Prefab\n" +
            "   Or shortcut: Ctrl+Shift+S (Win) / Cmd+Shift+S (Mac)\n" +
            "3. Specify save location and name",
            "확인 / OK"
        );
    }
    
    /// <summary>
    /// 복잡한 테스트 GameObject 생성 (여러 컴포넌트 포함)
    /// Creates a complex test GameObject with multiple components
    /// </summary>
    [MenuItem("Tools/Test/Create Complex Test GameObject")]
    private static void CreateComplexTestGameObject()
    {
        // 복잡한 구조의 GameObject 생성
        // Create GameObject with complex structure
        GameObject parent = new GameObject("ComplexRuntimeObject");
        
        // 여러 자식 오브젝트 추가
        // Add multiple child objects
        for (int i = 0; i < 3; i++)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = $"Child_{i}";
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = new Vector3(i * 1.5f - 1.5f, 0, 0);
            child.transform.localScale = Vector3.one * 0.8f;
            
            // 랜덤 색상
            // Random color
            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create a new material to avoid modifying the shared default material
                Material mat = new Material(renderer.sharedMaterial);
                mat.color = new Color(Random.value, Random.value, Random.value);
                renderer.sharedMaterial = mat;
            }
            
            // 손자 오브젝트 추가
            // Add grandchild object
            GameObject grandChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grandChild.name = "Detail";
            grandChild.transform.SetParent(child.transform);
            grandChild.transform.localPosition = Vector3.up * 0.7f;
            grandChild.transform.localScale = Vector3.one * 0.3f;
        }
        
        // 생성된 오브젝트 선택
        // Select created object
        Selection.activeGameObject = parent;
        
        Debug.Log("[PrefabSaverTest] Complex test GameObject created with multiple children.");
        
        EditorUtility.DisplayDialog(
            "복잡한 테스트 오브젝트 생성됨 / Complex Test Object Created",
            "여러 자식 오브젝트를 포함한 테스트 GameObject가 생성되었습니다.\n" +
            "'Tools > Save Selected as Prefab'를 사용하여 저장할 수 있습니다.\n\n" +
            "Complex test GameObject with multiple children has been created.\n" +
            "You can save it using 'Tools > Save Selected as Prefab'.",
            "확인 / OK"
        );
    }
}
