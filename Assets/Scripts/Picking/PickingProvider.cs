using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
    private int dataWidth, dataHeight; // Width and height of scaled data on CPU.
    private RenderTexture gpuTexture; // GPU render texture we render pickable ids into.
    private NativeArray<Color32> cpuData; // CPU color data, same values as stored in CPU texture.
    private bool hasAsyncRequest; // Whether we have a running request currently.
    private AsyncGPUReadbackRequest request; // The request we're currently waiting for to complete.

#if UNITY_EDITOR
    public Texture _Editor_GpuTexture => gpuTexture;
#endif

    /// <summary>
    /// Retrieve the pickable present at the current location in camera screenspace, if any.
    /// </summary>
    /// <param name="screenPosition">The position to get the pickable at.</param>
    /// <returns>The pickable at the specified screen position or <c>null</c> if there is none.</returns>
    [CanBeNull]
    public Pickable GetPickableAt(Vector2 screenPosition)
    {
        if (!cpuData.IsCreated)
            return null;

        int scaledX = (int) screenPosition.x / scaleDivisor;
        int scaledY = (int) screenPosition.y / scaleDivisor;
        Color32 packedId = cpuData[scaledX + dataWidth * scaledY];
        int id = PickingManager.DecodeId(packedId);
        return PickingManager.GetPickable(id);
    }

    private void OnDisable()
    {
        camera = null;

        dataWidth = 0;
        dataHeight = 0;

        if (gpuTexture != null) gpuTexture.Release();
        if (cpuData.IsCreated) cpuData.Dispose();

        hasAsyncRequest = false;
        request = default(AsyncGPUReadbackRequest);
    }

    private void Update()
    {
        if (camera == null)
            camera = GetComponent<Camera>();

        // If camera size changed (e.g. window resize) also adjust sizes of our picking textures.
        int currentWidth = camera.pixelWidth / scaleDivisor;
        int currentHeight = camera.pixelHeight / scaleDivisor;
        if (gpuTexture == null || dataWidth != currentWidth || dataHeight != currentHeight)
        {
            if (gpuTexture != null) gpuTexture.Release();
            if (cpuData.IsCreated) cpuData.Dispose();

            dataWidth = currentWidth;
            dataHeight = currentHeight;

            int depthBits = camera.depthTextureMode == DepthTextureMode.None ? 16 : 0;
            gpuTexture = new RenderTexture(dataWidth, dataHeight, depthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            gpuTexture.filterMode = FilterMode.Point;
            cpuData = new NativeArray<Color32>(dataWidth * dataHeight, Allocator.Persistent);
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

        if (!hasAsyncRequest)
        {
            hasAsyncRequest = true;
            request = AsyncGPUReadback.Request(gpuTexture);
        }
        else if (request.done)
        {
            if (!request.hasError)
            {
                request.GetData<Color32>().CopyTo(cpuData);
            }

            request = AsyncGPUReadback.Request(gpuTexture);
        }
    }
}