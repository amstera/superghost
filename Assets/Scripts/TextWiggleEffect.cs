using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextWiggleEffect : MonoBehaviour
{
    public float frequency = 5f;
    public float amplitude = 5f;

    private TextMeshProUGUI tmpUGUI;
    private Mesh mesh;
    private Vector3[] vertices;

    void Awake()
    {
        tmpUGUI = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(tmpUGUI.text))
        {
            tmpUGUI.canvasRenderer.SetMesh(null);
            return;
        }

        tmpUGUI.ForceMeshUpdate();
        mesh = tmpUGUI.mesh;
        vertices = mesh.vertices;

        for (int i = 0; i < tmpUGUI.textInfo.characterCount; ++i)
        {
            TMP_CharacterInfo charInfo = tmpUGUI.textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            int vertexIndex = charInfo.vertexIndex;
            float offset = Mathf.Sin(Time.time * frequency + i) * amplitude;

            for (int j = 0; j < 4; ++j)
            {
                Vector3 temp = vertices[vertexIndex + j];
                temp.y += offset;
                vertices[vertexIndex + j] = temp;
            }
        }

        mesh.vertices = vertices;
        tmpUGUI.canvasRenderer.SetMesh(mesh);
    }
}
