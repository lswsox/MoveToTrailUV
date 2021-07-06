using UnityEngine;

// Trail Renderer의 Head가 움직일 때 이동한 거리를 재질의 스크롤 UV로 전달하는 스크립트
// Trail Renderer의 Texture Mode가 Tile이여야함
// 재질에 전달되는 값은 0~1 사이의 값이다.
[ExecuteAlways]
public class MoveToTrailUV : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialData
    {
        public MaterialData(TrailRenderer trailRenderer, Material material, Vector2 uvScale, float move)
        {
            m_trailRenderer = trailRenderer;
            m_uvTiling = uvScale;
            m_move = move;
        }
        
        public TrailRenderer m_trailRenderer;
        [HideInInspector] public Vector2 m_uvTiling;
        [HideInInspector] public float m_move;
    }

#if UNITY_EDITOR
    //public bool m_overrideMaterial = true;
#endif
    public Transform m_moveObject;
    public string m_shaderPropertyName = "_MoveToMaterialUV"; // 셰이더에서 UV 값을 받아들일 프로퍼티 이름
    public int m_shaderPropertyID; // 셰이더 프로퍼티에 문자열을 사용하지 않기 위한 ID
    public MaterialData[] m_materialData = new MaterialData[1] { new MaterialData ( null, null, new Vector2(1, 1), 0f ) };

    private Vector3 m_beforePosW = Vector3.zero;
    void Start()
    {
        Initialize();
    }

    void LateUpdate()
    {
        if (m_moveObject == null)
            return;
        if (m_materialData == null || m_materialData.Length == 0)
            return;

        Vector3 nowPosW = m_moveObject.transform.position;
        if (nowPosW == m_beforePosW)
            return; // 위치 변화가 없으면 아무 작업도 안함
        
        float distance = Vector3.Distance(nowPosW, m_beforePosW);
        m_beforePosW = nowPosW;

        for (int i = 0; i < m_materialData.Length; i++)
        {
            if (m_materialData[i].m_trailRenderer == null)
                continue;

            m_materialData[i].m_move += distance * m_materialData[i].m_uvTiling.x;
            // m_move 값이 지나치게 커지지 않도록 하기 위해 1 이상은 나머지 값만 전달. (이미 m_uvTiling.x 가 곱해진 값이어야함)
            if (m_materialData[i].m_move > 1f)
            {
                m_materialData[i].m_move = m_materialData[i].m_move % 1f;
            }

            // 프로퍼티 존재 체크 없이 기록. 프로퍼티가 존재하면 재질 버전이 계속 변경된 것으로 처리되는 문제가 있음.
            TrailRenderer trailRenderer = m_materialData[i].m_trailRenderer;
            if (trailRenderer != null)
            {
                Material mat = trailRenderer.sharedMaterial;
                if (mat != null)
                {
                    mat.SetFloat(m_shaderPropertyID, m_materialData[i].m_move);
                }
            }
        }
    }

    public void Initialize()
    {
        if (m_materialData == null || m_materialData.Length == 0)
            return;
        
        m_shaderPropertyID = Shader.PropertyToID(m_shaderPropertyName);

        for (int i = 0; i < m_materialData.Length; i++)
        {
            m_materialData[i].m_move = 0f;
            TrailRenderer trailRenderer = m_materialData[i].m_trailRenderer;
            if (trailRenderer != null)
            {
                Material mat = trailRenderer.sharedMaterial;
                if (mat != null)
                {
                    m_materialData[i].m_uvTiling = mat.mainTextureScale;
                }
            }
        }
    }
}
