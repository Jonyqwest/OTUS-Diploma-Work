# Hybrid terrain shader modules

`_SuperTextTriplanarHybrid+fog.shader` is the material-facing shader and keeps
all Unity properties. Its pass includes `HybridTriplanarCore.cginc`, which
defines the execution order.

- `HybridTerrainCommon.cginc` — shared vertex data, coordinate conversion and
  small math helpers.
- `SatelliteTerrain.cginc` — satellite textures, animated transitions and
  distance blur.
- `TriplanarTerrain.cginc` — slope detail, triplanar projection and normal maps.
- `TerrainLighting.cginc` — terrain lighting.
- `SnowOverlay.cginc` — old experimental snow/deformation prototype. It is not
  included in the main pass.

Snow and grass now share one optimized overlay pass in `Shaders/SnowSurface.shader`.
Its implementation is split between `Shaders/Includes/TerrainSurfaceMasks.cginc`,
`TerrainSnowLayer.cginc`, `TerrainGrassLayer.cginc` and
`TerrainSurfaceOverlayCore.cginc`. Both layers reuse one satellite texture sample
and one renderer per tile.
