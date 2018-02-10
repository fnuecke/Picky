using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Main manager for shared pickable logic, such as conversion of pickable references to ids
/// and vice versa, as well as from ids to colors representing that id and vice versa. Also
/// provides management of material replacements with the tag set to make the replacement
/// shader render pickable objects (to avoid re-creating shared materials over and over).
/// <p/>
/// There can only be one global shared instance of this class.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Picking/Picking Manager", 0)]
public sealed class PickingManager : MonoBehaviour
{
    public const string PickableType = "PickableType";
    public const string Pickable = "Pickable";

    private static PickingManager instance; // Singleton instance for global access via static methods.

    private readonly Dictionary<int, Pickable> entries = new Dictionary<int, Pickable>(); // Known pickables.
    private readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>(); // Tracked materials with pickable tag set.

    /// <summary>
    /// Get the id of the specified pickable.
    /// </summary>
    /// <param name="pickable">The pickable to get the id of.</param>
    /// <returns>The id of the specified pickable.</returns>
    internal static int GetPickableId([NotNull] Pickable pickable)
    {
        return pickable.GetInstanceID();
    }

    /// <summary>
    /// Retrieve the pickable instance with the specified id, if any.
    /// </summary>
    /// <param name="id">The id of the pickable to retrieve.</param>
    /// <returns>The pickable with the specified id, or <c>null</c> if no such pickable exists.</returns>
    [CanBeNull]
    internal static Pickable GetPickable(int id)
    {
        if (instance == null)
            return null;

        Pickable entry;
        if (instance.entries.TryGetValue(id, out entry))
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// Gets a cached adjusted material or creates a new one based on the specified material.
    /// Pickable materials are identical to the specified material except that they have the
    /// <c>PickableType</c> tag set so they are rendered in the replacement pass used to render
    /// pickable ids.
    /// <p/>
    /// Note that all adjusted materials are destroyed when the picking manager is destroyed!
    /// </summary>
    /// <param name="material">The material to get the pickable version of.</param>
    /// <returns>An adjusted instance of the specified material with the pickable tag set.</returns>
    [NotNull]
    internal static Material GetPickableMaterial([NotNull] Material material)
    {
        if (instance == null)
            return material;

        Material pickableMaterial;
        if (!instance.materials.TryGetValue(material, out pickableMaterial))
        {
            pickableMaterial = Instantiate(material);
            pickableMaterial.SetOverrideTag(PickableType, Pickable);
            instance.materials.Add(material, pickableMaterial);
        }

        return pickableMaterial;
    }

    /// <summary>
    /// Encodes a pickable id into a color later to be decoded via <see cref="DecodeId"/>.
    /// </summary>
    /// <param name="id">The pickable id to encode.</param>
    /// <returns>The color representing the pickable id.</returns>
    internal static Color32 EncodeId(int id)
    {
        return new Color32(
            (byte) (id >> (3 * 8)),
            (byte) (id >> (2 * 8)),
            (byte) (id >> (1 * 8)),
            (byte) (id >> (0 * 8)));
    }

    /// <summary>
    /// Decodes a color into a pickable id previously encoded via <see cref="EncodeId"/>.
    /// </summary>
    /// <param name="color">The color to decode into a pickable id.</param>
    /// <returns>The decoded pickable id.</returns>
    internal static int DecodeId(Color32 color)
    {
        return (color.r << (3 * 8)) |
               (color.g << (2 * 8)) |
               (color.b << (1 * 8)) |
               (color.a << (0 * 8));
    }

    /// <summary>
    /// Register a pickable with this manager to allow lookup by id via <see cref="GetPickable"/>.
    /// </summary>
    /// <param name="pickable">The pickable to register.</param>
    internal static bool Add([NotNull] Pickable pickable)
    {
        if (instance == null)
        {
            Debug.LogWarning("A Pickable tried to register with the PickableManager, but no instance exists. Pickable has been disabled.");
            pickable.enabled = false;
            return false;
        }

        if (!instance.entries.ContainsKey(GetPickableId(pickable)))
            instance.entries.Add(GetPickableId(pickable), pickable);

        return true;
    }

    /// <summary>
    /// Unregister a pickable, it will no longer be accessible via <see cref="GetPickable"/>.
    /// Typically used to remove pickables from the index when they are disabled/destroyed.
    /// </summary>
    /// <param name="pickable">The pickable to unregister.</param>
    internal static void Remove([NotNull] Pickable pickable)
    {
        if (instance == null)
            return;

        instance.entries.Remove(GetPickableId(pickable));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("PickingManager instance already set. Are there multiple instances in the scene?");
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        entries.Clear();

        foreach (Material material in materials.Values)
        {
            Destroy(material);
        }

        materials.Clear();

        if (instance == this)
            instance = null;
    }
}