using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PickingProvider))]
public sealed class PickingProviderEditor : Editor
{
    private Material material;
    private Vector2 levels;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        material = new Material(Shader.Find("Hidden/PickableProviderEditor"));
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        Destroy(material);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
            return;

        PickingProvider pickingProvider = target as PickingProvider;
        Texture texture = pickingProvider._Editor_GpuTexture;

        if (texture == null)
            return;

        EditorGUILayout.MinMaxSlider("Levels", ref levels.x, ref levels.y, 0, 1);

        float width = EditorGUIUtility.currentViewWidth;
        float height = width * texture.height / texture.width;

        material.SetVector("_Levels", levels);
        EditorGUI.DrawPreviewTexture(EditorGUILayout.GetControlRect(false, height), texture, material);
    }
}