using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MoveToTrailUV))][CanEditMultipleObjects]
public class MoveToTrailUVEditor : Editor
{
    // 초기에 상황에 맞게 똑똑한 Sync 방향을 할 수 있도록 했었으나
    // 재질의 타일링 값과 Material Data의 타일링 값을 바꿔도 잘 작동하며, Revert라던가, 렌더러 변경 등 다양한 예외상황을 모두 코딩으로 하려나 너무 지저분해져서
    // 작업자가 수동으로 Sync 방향을 정하도록 함.
    // 기능 봉인 // SerializedProperty m_overrideMaterial_sp; // 디폴트로는 재질 값이 우선하지만, 이 체크를 켜면 Material Data의 값이 우선하여 재질의 타일링을 덮어쓴다.

    SerializedProperty m_moveObject_sp;
    SerializedProperty m_shaderPropertyName_sp;
    SerializedProperty m_shaderPropertyID_sp;
    SerializedProperty m_materialData_sp;

    private MoveToTrailUV m_mttuv;

    private void OnEnable()
    {
        // 기능 봉인 // m_overrideMaterial_sp = serializedObject.FindProperty("m_overrideMaterial");
        m_moveObject_sp = serializedObject.FindProperty("m_moveObject");
        m_shaderPropertyName_sp = serializedObject.FindProperty("m_shaderPropertyName");
        m_shaderPropertyID_sp = serializedObject.FindProperty("m_shaderPropertyID");
        m_materialData_sp = serializedObject.FindProperty("m_materialData");

        m_mttuv = target as MoveToTrailUV;
        
        serializedObject.Update();
        InitializeEditor();
        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        serializedObject.Update(); // 최신 상태 반영
        // 모든 재질의 _MoveToMaterialUV 값을 0으로 리셋.
        // 이렇게 하는 이유? 셰이더 프로퍼티에 존재하지 않더라도 경우에 따라 재질의 Saved Property에 존재할 수 있어서 자꾸 Dirty 상태가 됨. 셰이더나 셰이더그래프에서 한 번이라도 인스펙터에 Expose 되면 재질에는 프로퍼티가 저장됨.
        // 이렇게 해도 Saved Property가 존재하면 사용자 조작에 따라서 Dirty 되는 경우를 피할 수 없음. 유니티 API에서 재질의 Saved Property에 접근하는 방법을 아직 알아내지 못함.
        for (int i = 0; i < m_materialData_sp.arraySize; i++)
        {
            SerializedProperty materialDataElement_sp = m_materialData_sp.GetArrayElementAtIndex(i);
            TrailRenderer trailRenderer = (TrailRenderer)materialDataElement_sp.FindPropertyRelative("m_trailRenderer").objectReferenceValue;
            if (trailRenderer != null)
            {
                Material mat = trailRenderer.sharedMaterial;
                if (mat != null)
                {
                    mat.SetFloat(m_shaderPropertyID_sp.intValue, 0f);
                }
            }
        }
        //serializedObject.ApplyModifiedProperties(); // 재질 변경만 하므로 ApplyModifiedProperties는 안해도 됨
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        int checkTrailRenderertile = CheckTrailRendererTile();
        if (checkTrailRenderertile != -1)
        {
            string message = String.Format("Element {0}번 Trail Renderer의 Texture Mode가 Tile이 아닙니다.", checkTrailRenderertile);
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        string checkTrailRendererShader = CheckTrailRendererShader();
        if (checkTrailRendererShader != "")
        {
            EditorGUILayout.HelpBox(checkTrailRendererShader, MessageType.Warning);
        }

        EditorGUI.BeginChangeCheck();
        {
            // 기능 봉인 // m_overrideMaterial_sp.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Override Material", "UV Tiling 값을 어디에서 수정할지 방향을 결정합니다. 체크를 켜면 Move To Trail UV 컴포넌트의 UV Tiling 값이 우선하게 되고, 체크를 끄면 재질의 UV Tiling 값이 우선하게 됩니다."), m_overrideMaterial_sp.boolValue); // ToggleLeft를 사용하기 위해 boolValue 사용
            EditorGUILayout.PropertyField(m_moveObject_sp);
            EditorGUILayout.PropertyField(m_shaderPropertyName_sp);
            EditorGUILayout.PropertyField(m_materialData_sp);
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "MoveToTrailUV changed");
            InitializeEditor(); // 뭔가 변화가 생기면 초기화
        }

        for (int i = 0; i < m_materialData_sp.arraySize; i++)
        {
            SerializedProperty materialDataElement_sp = m_materialData_sp.GetArrayElementAtIndex(i);
            SyncElementTiling(materialDataElement_sp); // 별다른 변화가 없으면 수시로 재질과 동기화
        }
        serializedObject.ApplyModifiedProperties();
        //base.OnInspectorGUI();
    }

    private void SyncElementTiling(SerializedProperty materialDataElement_sp) // 재질의 타일링만 동기화
    {
        TrailRenderer trailRenderer = (TrailRenderer)materialDataElement_sp.FindPropertyRelative("m_trailRenderer").objectReferenceValue;
        // 렌더러가 존재하면
        if (trailRenderer != null)
        {
            // 렌더러가 사용하는 재질 Get
            Material mat = trailRenderer.sharedMaterial;
            if (mat != null)
            {
                if (materialDataElement_sp.FindPropertyRelative("m_uvTiling").vector2Value != mat.mainTextureScale)
                {
                    // 기능 봉인 //
                    //if (m_overrideMaterial_sp.boolValue)
                    if (false)
                    {
                        // MoveToTrailUV 데이터를 기준으로 재질을 변경
                        //mat.mainTextureScale = materialDataElement_sp.FindPropertyRelative("m_uvTiling").vector2Value;
                    }
                    else
                    {
                        // 재질을 기준으로 MoveToTrailUV 데이터를 변경
                        materialDataElement_sp.FindPropertyRelative("m_uvTiling").vector2Value = mat.mainTextureScale;
                    }
                }
            }
        }
    }

    // 에디터용 초기화 함수. Undo 등의 상황을 위해
    private void InitializeEditor()
    {
        if (m_materialData_sp.serializedObject.targetObject == null || m_materialData_sp.arraySize == 0)
            return;

        m_shaderPropertyID_sp.intValue = Shader.PropertyToID(m_shaderPropertyName_sp.stringValue);

        // m_materialData 초기화
        for (int i = 0; i < m_materialData_sp.arraySize; i++)
        {
            SerializedProperty materialDataElement_sp = m_materialData_sp.GetArrayElementAtIndex(i);
            SyncElementTiling(materialDataElement_sp); // Tiling만 동기화

            // m_move 값 초기화
            materialDataElement_sp.FindPropertyRelative("m_move").floatValue = 0f;
        }
    }

    // Trail Renderer의 Texture Mode가 Tile로 되어있는지 체크. 문제 없으면 -1 리턴, 문제 있으면 해당 번호 리턴
    private int CheckTrailRendererTile()
    {
        if (m_mttuv.m_materialData.Length == 0)
            return -1;

        for (int i = 0; i < m_mttuv.m_materialData.Length; i++)
        {
            if (m_mttuv.m_materialData[i].m_trailRenderer == null)
                continue;
            TrailRenderer trailRenderer = (TrailRenderer)m_mttuv.m_materialData[i].m_trailRenderer;
            if (trailRenderer.textureMode != LineTextureMode.Tile)
            {
                return i;
            }
        }
        return -1;
    }

    private string CheckTrailRendererShader()
    {
        if (m_mttuv.m_materialData.Length == 0)
            return "";

        for (int i = 0; i < m_mttuv.m_materialData.Length; i++)
        {
            TrailRenderer trailRenderer = m_mttuv.m_materialData[i].m_trailRenderer;
            if (trailRenderer == null)
                continue;
            Material mat = trailRenderer.sharedMaterial;
            // 재질이 비었는지 검사
            if (mat == null)
            {
                string message = String.Format("Element {0}번째 렌더러의 재질이 없습니다.", i);
                return message;
            }
            
            // 필요시 다른 재질 검사 추가 후 message 리턴
        }

        return "";
    }
}
