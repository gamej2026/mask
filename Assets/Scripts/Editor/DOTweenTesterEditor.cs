using UnityEngine;
using UnityEditor;
using DG.Tweening;
using DG.DOTweenEditor;

/// <summary>
/// DOTweenTester의 커스텀 에디터입니다.
/// 인스펙터에 Play/Stop/Reset 버튼을 추가하여 애니메이션을 쉽게 테스트할 수 있습니다.
/// Unity Play Mode가 아니어도 에디터에서 바로 테스트할 수 있습니다.
/// </summary>
[CustomEditor(typeof(DOTweenTester))]
public class DOTweenTesterEditor : Editor
{
    private DOTweenTester tester;
    
    private void OnEnable()
    {
        tester = (DOTweenTester)target;
    }

    private void OnDisable()
    {
        // 에디터가 비활성화될 때 프리뷰 정지
        if (!Application.isPlaying)
        {
            StopEditorPreview();
        }
    }

    public override void OnInspectorGUI()
    {
        // 테스트 버튼 섹션 (최상단)
        EditorGUILayout.LabelField("🎬 테스트 컨트롤", EditorStyles.boldLabel);
        
        // 에디터 모드 안내
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("✅ 에디터 모드에서도 테스트 가능!", MessageType.None);
        }
        
        EditorGUILayout.Space(5);
        
        // 상태 표시
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("상태:", GUILayout.Width(40));
        
        Color originalColor = GUI.color;
        if (tester.IsPlaying)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("▶ 재생 중", EditorStyles.boldLabel);
        }
        else
        {
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("■ 정지", EditorStyles.boldLabel);
        }
        GUI.color = originalColor;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 버튼 행 1: Play / Stop
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶ Play", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                tester.Play();
            }
            else
            {
                // 에디터 모드에서 재생
                PlayEditorPreview();
            }
        }
        
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
        if (GUILayout.Button("■ Stop", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                tester.Stop();
            }
            else
            {
                StopEditorPreview();
            }
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // 버튼 행 2: Pause / Resume (Play Mode에서만)
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.4f);
            if (GUILayout.Button("⏸ Pause", GUILayout.Height(25)))
            {
                tester.Pause();
            }
            
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
            if (GUILayout.Button("▶ Resume", GUILayout.Height(25)))
            {
                tester.Resume();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(5);
        
        // 버튼 행 3: 상태 관리
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("💾 현재 상태 저장", GUILayout.Height(25)))
        {
            tester.SaveOriginalState();
            EditorUtility.SetDirty(tester);
        }
        
        GUI.backgroundColor = new Color(0.6f, 0.4f, 0.8f);
        if (GUILayout.Button("↩ 원본 상태 복원", GUILayout.Height(25)))
        {
            if (Application.isPlaying)
            {
                tester.RestoreOriginalState();
            }
            else
            {
                RestoreEditorState();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "💡 사용법:\n" +
            "1. 아래에서 애니메이션 타입과 속성을 설정하세요.\n" +
            "2. 'Play' 버튼을 눌러 테스트하세요. (Play Mode 불필요!)\n" +
            "3. '현재 상태 저장'으로 원본 상태를 기록하고,\n" +
            "   '원본 상태 복원'으로 되돌릴 수 있습니다.",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // 구분선
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        EditorGUILayout.Space(5);
        
        // 기본 인스펙터 그리기 (속성들)
        DrawDefaultInspector();
        
        // Inspector 갱신 (재생 상태 표시를 위해)
        if (tester.IsPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// 에디터 모드에서 애니메이션 프리뷰 시작
    /// </summary>
    private void PlayEditorPreview()
    {
        StopEditorPreview();
        
        tester.IsPlaying = true;
        
        Tween tween = tester.CreateTween();
        tester.CurrentTween = tween;
        
        if (tween != null)
        {
            tween.SetDelay(tester.delay);
            tween.SetEase(tester.easeType);
            
            if (tester.loopCount != 0)
            {
                tween.SetLoops(tester.loopCount, tester.loopType);
            }
            
            tween.OnComplete(() => 
            {
                tester.IsPlaying = false;
                DOTweenEditorPreview.Stop(true);
            });
            tween.OnKill(() => tester.IsPlaying = false);
            
            DOTweenEditorPreview.PrepareTweenForPreview(tween);
            DOTweenEditorPreview.Start();
        }
    }

    /// <summary>
    /// 에디터 모드에서 애니메이션 프리뷰 정지
    /// </summary>
    private void StopEditorPreview()
    {
        DOTweenEditorPreview.Stop(true);
        
        if (tester.CurrentTween != null && tester.CurrentTween.IsActive())
        {
            tester.CurrentTween.Kill();
            tester.CurrentTween = null;
        }
        tester.IsPlaying = false;
    }

    /// <summary>
    /// 에디터 모드에서 원본 상태로 복원
    /// </summary>
    private void RestoreEditorState()
    {
        StopEditorPreview();
        tester.RestoreOriginalState();
        EditorUtility.SetDirty(tester);
    }
}
