using UnityEngine;

public class SSTDDeformer : MonoBehaviour
{
    [SerializeField]
    private PrimitiveType m_PrimitiveType;
    [SerializeField]
    private GameObject m_CustomPrimitive = null;
    [SerializeField]
    private float m_Offset = 0.0035f;
    [SerializeField]
    private Texture m_Texture = null;
    [SerializeField]
    private Color m_Color = new Color(0.77f, 0.19f, 0.19f);
    [SerializeField]
    private bool m_Animate = true;

    private Vector3 m_PreviousToolPosition = new Vector3();

    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<RegisterMesh>())
        {
            using (var _x = new ProfilerScope("SSTD"))
            {
                SSTDManager.Get.ContinuousDeformation(this.gameObject, ref m_PreviousToolPosition, m_Offset, () =>
                {
                    if (m_CustomPrimitive == null)
                        SSTDManager.Get.CreatePrimitive(m_PrimitiveType, this.gameObject, m_Color, m_Texture, m_Animate);
                    else
                        SSTDManager.Get.CreatePrimitive(m_CustomPrimitive, this.gameObject, m_Color, m_Texture, m_Animate);
                });
            }
        }
    }
}
