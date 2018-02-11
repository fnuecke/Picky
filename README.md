# Introduction
Pixel-perfect collider-free picking of objects in Unity. Picking here meaning getting an object reference for an arbitrary screen location. More of a proof-of-concept for future reference than anything else.

# Limitations

- To use this in production code, you'll want to replace the current readback to the CPU with an asynchronous texture read to avoid pipeline stalls, e.g. as demonstrated in [this Github project](https://github.com/SlightlyMad/AsyncTextureReader).
- If your models use multiple materials contributing to the pickable area you may also want to adjust `Pickable` to adjust all materials, not just the primary one.
 
# How it works
A script attached to a camera (`PickingProvider`) renders pickable objects' ids into a `RenderTexture` using a replacement shader which gets the id via a shader property named `_PickableId`, set by a component that has to be present on each pickable object, `Pickable`. The `RenderTexture` is then read back into a `Texture2D` to allow reading pickable ids at any screen coordinate in constant time.

The following components are needed: 
- `Pickable`, adds override render tag `PickableType=Pickable` to the primary material of all renderers on its `GameObject` and all child `GameObjects` and adds the pickable's id as a shader property named `_PickableId` to the `MaterialPropertyBlock` of these renderers (adds one if necessary).
- `PickingProvider`, must be attached to a camera, manages GPU and CPU side data (`RenderTexture` and `Texture2D`) and renders pickable ids into these textures. Provides lookup of screen position to pickable object reference.
- `PickingManager`, a singleton, i.e. only one can exist at a time. Tracks all adjusted material instances for their original materials, so `Pickable`s can re-use already adjusted materials (avoids `Pickable` components creating new materials all over the place to add the override tag). The adjusted materials get destroyed when the `PickingManager` gets destroyed. Also provides lookup of pickable ids to pickable objects.