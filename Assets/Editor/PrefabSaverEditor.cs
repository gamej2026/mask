using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 런타임에 생성된 프리팹을 Resources 폴더에 저장하는 에디터 유틸리티
/// Editor utility to save runtime-generated prefabs to the Resources folder
/// </summary>
public class PrefabSaverEditor
{
    private const string PREFABS_PATH = "Assets/Resources/Prefabs/";

    /// <summary>
    /// 선택한 GameObject를 Resources/Prefabs 폴더에 프리팹으로 저장
    /// Saves the selected GameObject as a prefab in the Resources/Prefabs folder
    /// </summary>
    [MenuItem("Tools/Save Selected as Prefab %#S")]
    private static void SaveSelectedAsPrefab()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog(
                "프리팹 저장 실패 / Save Failed",
                "저장할 GameObject를 선택해주세요.\nPlease select a GameObject to save.",
                "확인 / OK"
            );
            return;
        }

        // Resources/Prefabs 디렉토리 확인 및 생성
        // Check and create Resources/Prefabs directory
        EnsureDirectoryExists(PREFABS_PATH);

        // 프리팹 이름 입력 받기
        // Get prefab name from user
        string prefabName = selectedObject.name;
        string inputName = EditorUtility.SaveFilePanel(
            "프리팹 저장 / Save Prefab",
            PREFABS_PATH,
            prefabName,
            "prefab"
        );

        if (string.IsNullOrEmpty(inputName))
        {
            // 취소됨
            // Cancelled
            return;
        }

        // 전체 경로를 상대 경로로 변환
        // Convert absolute path to relative path
        string relativePath = GetRelativePath(inputName);
        
        // 경로가 Assets 내부가 아니면 기본 경로 사용
        // Use default path if not inside Assets
        if (!relativePath.StartsWith("Assets/"))
        {
            string fileName = Path.GetFileName(inputName);
            relativePath = PREFABS_PATH + fileName;
        }

        // 프리팹 저장
        // Save prefab
        SavePrefab(selectedObject, relativePath);
    }

    /// <summary>
    /// 메뉴 항목 활성화 조건: GameObject가 선택되어 있을 때만
    /// Menu item validation: Only enabled when a GameObject is selected
    /// </summary>
    [MenuItem("Tools/Save Selected as Prefab %#S", true)]
    private static bool ValidateSaveSelectedAsPrefab()
    {
        return Selection.activeGameObject != null;
    }

    /// <summary>
    /// 디렉토리가 없으면 생성
    /// Creates directory if it doesn't exist
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// GameObject를 프리팹으로 저장
    /// Saves GameObject as a prefab
    /// </summary>
    private static void SavePrefab(GameObject obj, string path)
    {
        // 기존 프리팹이 있는지 확인
        // Check if prefab already exists
        bool isNewPrefab = !File.Exists(path);

        GameObject prefab;
        if (isNewPrefab)
        {
            // 새 프리팹 생성
            // Create new prefab
            prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        }
        else
        {
            // 기존 프리팹 업데이트 여부 확인
            // Confirm update of existing prefab
            bool confirm = EditorUtility.DisplayDialog(
                "프리팹 덮어쓰기 / Overwrite Prefab",
                $"'{Path.GetFileName(path)}' 프리팹이 이미 존재합니다.\n덮어쓰시겠습니까?\n\nPrefab '{Path.GetFileName(path)}' already exists.\nDo you want to overwrite it?",
                "덮어쓰기 / Overwrite",
                "취소 / Cancel"
            );

            if (!confirm)
            {
                return;
            }

            // 기존 프리팹 덮어쓰기
            // Overwrite existing prefab
            prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        }

        if (prefab != null)
        {
            // 성공 메시지
            // Success message
            EditorUtility.DisplayDialog(
                "저장 완료 / Save Complete",
                $"프리팹이 저장되었습니다.\nPrefab saved:\n{path}",
                "확인 / OK"
            );

            // 프로젝트 뷰에서 저장된 프리팹 선택
            // Select saved prefab in project view
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"[PrefabSaver] Prefab saved: {path}");
        }
        else
        {
            // 실패 메시지
            // Failure message
            EditorUtility.DisplayDialog(
                "저장 실패 / Save Failed",
                "프리팹 저장에 실패했습니다.\nFailed to save prefab.",
                "확인 / OK"
            );

            Debug.LogError($"[PrefabSaver] Failed to save prefab: {path}");
        }
    }

    /// <summary>
    /// 절대 경로를 Assets 기준 상대 경로로 변환
    /// Converts absolute path to relative path from Assets
    /// </summary>
    private static string GetRelativePath(string absolutePath)
    {
        string projectPath = Application.dataPath;
        
        if (absolutePath.StartsWith(projectPath))
        {
            string relativePath = "Assets" + absolutePath.Substring(projectPath.Length);
            return relativePath.Replace("\\", "/");
        }
        
        return absolutePath.Replace("\\", "/");
    }
}
