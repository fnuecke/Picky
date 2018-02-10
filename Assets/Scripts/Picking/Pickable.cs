using UnityEngine;

/// <summary>
/// Component to be added to rendered objects to enable picking them using a <see cref="PickingProvider"/>.
/// <p/>
/// This component will replace materials of all renderers with a modified version that has a tag set which
/// will make the object render in the replacement shader used to render pickable ids.
/// </summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
[AddComponentMenu("Picking/Pickable", 20)]
public sealed class Pickable : MonoBehaviour
{
    private const string PickableIdPropertyName = "_PickableId"; // Name of the shader property holding the pickable id.

    private MaterialPropertyBlock properties; // Re-used property block used to set pickable id property.
    private int pickableIdPropertyId; // Cached shader property id of pickable id property.
    private Renderer[] renderers; // List of renderers below this component which will have their materials adjusted.

    private void Awake()
    {
        pickableIdPropertyId = Shader.PropertyToID(PickableIdPropertyName);
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        // Picking manager may not be instantiated, in which case we can't do anything useful, so stop.
        if (!PickingManager.Add(this))
            return;

        if (renderers == null)
            return;

        if (properties == null)
            properties = new MaterialPropertyBlock();

        int id = PickingManager.GetPickableId(this);
        Color32 packedId = PickingManager.EncodeId(id);

        foreach (Renderer r in renderers)
        {
            properties.Clear();

            // Keep existing changed properties.
            if (r.HasPropertyBlock())
                r.GetPropertyBlock(properties);

            properties.SetColor(pickableIdPropertyId, packedId);

            r.SetPropertyBlock(properties);

            r.sharedMaterial = PickingManager.GetPickableMaterial(r.sharedMaterial);
        }
    }

    private void OnDisable()
    {
        PickingManager.Remove(this);
    }
}