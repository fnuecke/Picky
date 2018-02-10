using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public sealed class ColorProperty : MonoBehaviour
{
    private const string ColorPropertyName = "_Color";

    /// <summary>
    /// The tint to apply to all renderers in this or child GameObjects.
    /// </summary>
    [SerializeField, Tooltip("The color tint to apply to all renderers.")]
    private Color color = Color.white;

    private MaterialPropertyBlock properties;
    private int colorPropertyId;
    private Renderer[] renderers;

    public Color Color
    {
        get { return color; }
        set
        {
            if (value == color)
                return;

            color = value;
            ApplyColor();
        }
    }

    private void Awake()
    {
        colorPropertyId = Shader.PropertyToID(ColorPropertyName);
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        ApplyColor();
    }

    private void OnValidate()
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (renderers == null)
            return;

        if (properties == null)
            properties = new MaterialPropertyBlock();

        foreach (Renderer r in renderers)
        {
            properties.Clear();

            if (r.HasPropertyBlock())
                r.GetPropertyBlock(properties);

            properties.SetColor(colorPropertyId, color);

            r.SetPropertyBlock(properties);
        }
    }
}