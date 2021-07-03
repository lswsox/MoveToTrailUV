using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MoveToTrailUV))][CanEditMultipleObjects]
public class MoveToTrailUVEditor : Editor
{
    SerializedProperty m_moveObject_sp;
    SerializedProperty m_shaderPropertyName_sp;
    SerializedProperty m_shaderPropertyID_sp;
    SerializedProperty m_materialData_sp;

    private Renderer[] m_renderersBefore; // 프로퍼티의 변경사항이 렌더러 변경인지를 체크하기 위한 백업 렌더러 배열

    private MoveToTrailUV m_mttuv;
    
    private void OnEnable()
    {
        m_moveObject_sp = serializedObject.FindProperty("m_moveObject");
        m_shaderPropertyName_sp = serializedObject.FindProperty("m_shaderPropertyName");
        m_shaderPropertyID_sp = serializedObject.FindProperty("m_shaderPropertyID");
        m_materialData_sp = serializedObject.FindProperty("m_materialData");

        m_mttuv = target as MoveToTrailUV;
        
        serializedObject.Update();
        InitializeEditor(false);
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
            SerializedProperty materialData_sp = m_materialData_sp.GetArrayElementAtIndex(i);
            Renderer renderer = (Renderer)materialData_sp.FindPropertyRelative("m_renderer").objectReferenceValue;
            if (renderer != null)
            {
                Material mat = renderer.sharedMaterial;
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
            EditorGUILayout.PropertyField(m_moveObject_sp);
            EditorGUILayout.PropertyField(m_shaderPropertyName_sp);
            EditorGUILayout.PropertyField(m_materialData_sp);
        }
        if(EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "MoveToTrailUV changed");
            InitializeEditor(true);
        }
        
        serializedObject.ApplyModifiedProperties();
        //base.OnInspectorGUI();
    }

    // 에디터용 초기화 함수. Undo 등의 상황을 위해
    public void InitializeEditor(bool valueChanged)
    {
        if (m_materialData_sp.serializedObject.targetObject == null || m_materialData_sp.arraySize == 0)
            return;

        m_shaderPropertyID_sp.intValue = Shader.PropertyToID(m_shaderPropertyName_sp.stringValue);

        // 렌더러 변경이 발생했는지 체크
        // 먼저 Null과 숫자 체크
        if (m_renderersBefore == null)
        {
            m_renderersBefore = new Renderer[1]; // 초기화가 안된 Null이면 아무거나 하나 채운다.
        }
        if (m_renderersBefore.Length != m_materialData_sp.arraySize)
        {
            // 숫자가 다르면
            m_renderersBefore = new Renderer[m_materialData_sp.arraySize];
            for (int i = 0; i < m_renderersBefore.Length; i++)
            {
                SerializedProperty materialData_sp = m_materialData_sp.GetArrayElementAtIndex(i);
                Renderer renderer = (Renderer)materialData_sp.FindPropertyRelative("m_renderer").objectReferenceValue;
                if (renderer == null)
                {
                    m_renderersBefore[i] = null;
                }
                else
                {
                    m_renderersBefore[i] = renderer;
                }
            }
        }

        // m_materialData 초기화
        for (int i = 0; i < m_materialData_sp.arraySize; i++)
        {
            SerializedProperty materialData_sp = m_materialData_sp.GetArrayElementAtIndex(i);
            
            // m_move 값 초기화
            materialData_sp.FindPropertyRelative("m_move").floatValue = 0f;

            Renderer renderer = (Renderer)materialData_sp.FindPropertyRelative("m_renderer").objectReferenceValue;
            // 렌더러가 존재하면
            if (renderer != null)
            {
                bool rendererChanged = false;
                if (m_renderersBefore[i] != renderer)
                {
                    // 전에 저장해둔 렌더러와 현재 렌더러가 다르면 변경 체크 후 저장본 업데이트
                    rendererChanged = true;
                    m_renderersBefore[i] = renderer;
                }

                // 렌더러가 사용하는 재질 Get
                Material mat = renderer.sharedMaterial;
                materialData_sp.FindPropertyRelative("m_material").objectReferenceValue = mat;
                if (mat != null)
                {
                    if (valueChanged)
                    {
                        if (rendererChanged)
                        {
                            // 렌더러가 변경된 경우 재질 변경이 아닌 재질값을 다시 가져옴
                            materialData_sp.FindPropertyRelative("m_uvTiling").floatValue = mat.mainTextureScale.x;
                        }
                        else
                        {
                            // 에디터 인스펙터에서 값이 변경된 경우 재질의 타일링 값을 변경
                            Undo.RecordObject(mat, "MoveToTrailUV Material changed");
                            Vector2 mainTextureScale = mat.mainTextureScale;
                            mainTextureScale.x = materialData_sp.FindPropertyRelative("m_uvTiling").floatValue;
                            mat.mainTextureScale = mainTextureScale;
                        }
                    }
                    else
                    {
                        // 값의 변경이 아닌 단순 초기화
                        materialData_sp.FindPropertyRelative("m_uvTiling").floatValue = mat.mainTextureScale.x;
                    }
                }
            }
            else
            {
                materialData_sp.FindPropertyRelative("m_material").objectReferenceValue = null;
            }
        }
    }

    // Trail Renderer의 Texture Mode가 Tile로 되어있는지 체크. 문제 없으면 -1 리턴, 문제 있으면 해당 번호 리턴
    private int CheckTrailRendererTile()
    {
        if (m_mttuv.m_materialData.Length == 0)
            return -1;

        for (int i = 0; i < m_mttuv.m_materialData.Length; i++)
        {
            if (m_mttuv.m_materialData[i].m_renderer == null)
                continue;
            TrailRenderer trailRenderer = (TrailRenderer)m_mttuv.m_materialData[i].m_renderer;
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
            if (m_mttuv.m_materialData[i].m_renderer == null)
                continue;
            Material mat = m_mttuv.m_materialData[i].m_material;
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
