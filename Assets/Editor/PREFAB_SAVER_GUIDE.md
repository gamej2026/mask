# Prefab Saver Editor - 사용 가이드 / User Guide

## 기능 설명 / Feature Description

런타임에 생성된 GameObject를 에디터 메뉴를 통해 Resources 폴더에 프리팹으로 저장할 수 있는 기능입니다.

This feature allows you to save runtime-generated GameObjects as prefabs in the Resources folder using an editor menu.

## 사용 방법 / How to Use

### 1. GameObject 생성 / Create GameObject

런타임 또는 에디터에서 GameObject를 생성합니다.

Create a GameObject at runtime or in the editor.

#### 테스트를 위한 빠른 생성 / Quick Creation for Testing

Unity 에디터에서:
- **Menu**: `Tools > Test > Create Test GameObject`
- 또는 / Or: `Tools > Test > Create Complex Test GameObject`

이 메뉴들은 테스트용 GameObject를 자동으로 생성합니다.

These menus automatically create test GameObjects.

### 2. GameObject 선택 / Select GameObject

Hierarchy 창에서 저장하고 싶은 GameObject를 선택합니다.

Select the GameObject you want to save in the Hierarchy window.

### 3. 프리팹으로 저장 / Save as Prefab

#### 방법 1: 메뉴 사용 / Method 1: Using Menu
- **Menu**: `Tools > Save Selected as Prefab`

#### 방법 2: 단축키 사용 / Method 2: Using Shortcut
- **Windows**: `Ctrl + Shift + S`
- **macOS**: `Cmd + Shift + S`

### 4. 저장 위치 지정 / Specify Save Location

파일 다이얼로그에서 저장 위치와 이름을 지정합니다.

Specify the save location and name in the file dialog.

- 기본 경로: `Assets/Resources/Prefabs/`
- Default path: `Assets/Resources/Prefabs/`

### 5. 확인 / Confirmation

- 새 프리팹: 바로 저장됩니다.
- 기존 프리팹: 덮어쓰기 확인 다이얼로그가 표시됩니다.

- New prefab: Saved immediately.
- Existing prefab: Overwrite confirmation dialog is shown.

## 주요 기능 / Key Features

### 자동 디렉토리 생성 / Automatic Directory Creation
`Assets/Resources/Prefabs/` 디렉토리가 없으면 자동으로 생성됩니다.

If the `Assets/Resources/Prefabs/` directory doesn't exist, it's created automatically.

### 프리팹 덮어쓰기 보호 / Overwrite Protection
기존 프리팹을 덮어쓸 때 확인 다이얼로그가 표시되어 실수로 인한 데이터 손실을 방지합니다.

A confirmation dialog is shown when overwriting existing prefabs to prevent accidental data loss.

### 저장 후 자동 선택 / Auto-selection After Save
저장된 프리팹이 자동으로 선택되고 Project 창에서 강조 표시됩니다.

The saved prefab is automatically selected and highlighted in the Project window.

## 코드 예제 / Code Examples

### 스크립트에서 런타임 GameObject 생성 / Create Runtime GameObject in Script

```csharp
using UnityEngine;

public class RuntimeObjectCreator : MonoBehaviour
{
    void Start()
    {
        // 런타임에 GameObject 생성
        // Create GameObject at runtime
        GameObject runtimeObject = new GameObject("MyRuntimeObject");
        
        // 컴포넌트 추가
        // Add components
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(runtimeObject.transform);
        
        // 이 오브젝트를 나중에 Hierarchy에서 선택하고
        // Tools > Save Selected as Prefab 메뉴를 사용하여 저장 가능
        // You can later select this object in Hierarchy and save it
        // using Tools > Save Selected as Prefab menu
    }
}
```

## 파일 구조 / File Structure

```
Assets/
├── Editor/
│   └── PrefabSaverEditor.cs          # 메인 기능 / Main feature
├── Scripts/
│   └── Editor/
│       └── PrefabSaverEditorTest.cs  # 테스트 유틸리티 / Test utility
└── Resources/
    └── Prefabs/                       # 저장 위치 / Save location
```

## 문제 해결 / Troubleshooting

### 메뉴가 보이지 않는 경우 / Menu Not Visible
- Unity 에디터를 재시작하세요.
- Restart Unity editor.

### GameObject가 선택되지 않는 경우 / GameObject Not Selected
- Hierarchy 창에서 GameObject를 클릭하여 선택하세요.
- Click the GameObject in the Hierarchy window to select it.

### 저장이 실패하는 경우 / Save Fails
- 파일 경로가 `Assets/` 폴더 내부인지 확인하세요.
- Check if the file path is inside the `Assets/` folder.
- 파일 이름에 특수 문자가 없는지 확인하세요.
- Check if the file name doesn't contain special characters.

## 기술 세부 사항 / Technical Details

### API 사용 / API Usage
- `PrefabUtility.SaveAsPrefabAsset()`: 프리팹 저장
- `EditorUtility.SaveFilePanel()`: 파일 저장 다이얼로그
- `Selection.activeGameObject`: 선택된 GameObject 가져오기

### 메뉴 단축키 / Menu Shortcut
- `%` = Ctrl (Windows) / Cmd (macOS)
- `#` = Shift
- `&` = Alt

## 라이센스 / License

이 코드는 MIT 라이센스를 따릅니다.

This code follows the MIT License.
