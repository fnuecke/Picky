using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Manager to be used per camera that should provide picking. Usually only the main
/// camera. There can be multiple instances of this class, providing picking in camera
/// local space.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Picking/Picking Provider", 1)]
public sealed class PickingProvider : MonoBehaviour
{
    /// <summary>
    /// For picking it's usally not necessary to have the full resolution (i.e. actual pixel perfect
    /// picking), in which case we save some time and bandwidth by actually rendering the pickable
    /// ids into a lower-resolution texture. Its resolution is original resolution divided by this.
    /// </summary>
    [SerializeField, Range(1, 8), Tooltip("Value by which to divide original resolution to get lookup texture resolution.")]
    private int scaleDivisor = 4;

    /// <summary>
    /// The replacement shader used to render pickable ids as colors, to be then retrieved
    /// and converted back in a simple texture lookup.
    /// </summary>
    [SerializeField, Tooltip("The replacement shader used to render object ids for pickable objects.")]
    private Shader objectIdShader;

    private new Camera camera; // Camera used to render with replacement shader.
    private RenderTexture gpuTexture; // GPU render texture we render pickable ids into.
    private Texture2D cpuTexture; // CPU side texture we read pickable ids from.

#if UNITY_EDITOR
    public Texture _Editor_GpuTexture
    {
        get { return gpuTexture; }
    }
#endif

    /// <summary>
    /// Retrieve the pickable present at the current location in camera screenspace, if any.
    /// </summary>
    /// <param name="screenPosition">The position to get the pickable at.</param>
    /// <returns>The pickable at the specified screen position or <c>null</c> if there is none.</returns>
    [CanBeNull]
    public Pickable GetPickableAt(Vector2 screenPosition)
    {
        if (cpuTexture == null)
            return null;

        Color32 packedId = cpuTexture.GetPixel((int) screenPosition.x / scaleDivisor, (int) screenPosition.y / scaleDivisor);
        int id = PickingManager.DecodeId(packedId);
        return PickingManager.GetPickable(id);
    }

    private void Update()
    {
        if (camera == null)
            camera = GetComponent<Camera>();

        // If camera size changed (e.g. window resize) also adjust sizes of our picking textures.
        if (gpuTexture == null || gpuTexture.width != camera.pixelWidth / scaleDivisor || gpuTexture.height != camera.pixelHeight / scaleDivisor)
        {
            if (gpuTexture != null) gpuTexture.Release();
            if (cpuTexture != null) Destroy(cpuTexture);

            int depthBits = camera.depthTextureMode == DepthTextureMode.None ? 16 : 0;
            gpuTexture = new RenderTexture(camera.pixelWidth / scaleDivisor, camera.pixelHeight / scaleDivisor, depthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            gpuTexture.filterMode = FilterMode.Point;
            cpuTexture = new Texture2D(camera.pixelWidth / scaleDivisor, camera.pixelHeight / scaleDivisor, TextureFormat.ARGB32, false, true);
            cpuTexture.filterMode = FilterMode.Point;
            cpuTexture.wrapMode = TextureWrapMode.Clamp;
        }

        RenderTexture oldRenderTexture = camera.targetTexture;
        CameraClearFlags oldClearFlags = camera.clearFlags;
        Color oldBackgroundColor = camera.backgroundColor;
        RenderingPath oldRenderingPath = camera.renderingPath;
        bool oldAllowMsaa = camera.allowMSAA;

        camera.targetTexture = gpuTexture; // Render into our render texture.
        camera.clearFlags = CameraClearFlags.SolidColor; // Make sure non-rendered pixels have id zero.
        camera.backgroundColor = Color.clear; // Ditto.
        camera.renderingPath = RenderingPath.Forward; // No need for a gbuffer here.
        camera.allowMSAA = false; // Avoid interpolated colors.

        camera.RenderWithShader(objectIdShader, PickingManager.PickableType);

        camera.targetTexture = oldRenderTexture;
        camera.clearFlags = oldClearFlags;
        camera.backgroundColor = oldBackgroundColor;
        camera.renderingPath = oldRenderingPath;
        camera.allowMSAA = oldAllowMsaa;

        // TODO Do readback asynchronously.

        oldRenderTexture = RenderTexture.active;

        RenderTexture.active = gpuTexture;
        cpuTexture.ReadPixels(new Rect(0, 0, cpuTexture.width, cpuTexture.height), 0, 0);
        cpuTexture.Apply();

        RenderTexture.active = oldRenderTexture;
    }
}