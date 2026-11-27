#ifndef TERRAIN_GRASS_GPU_COMMON_CGINC
#define TERRAIN_GRASS_GPU_COMMON_CGINC

// 64-byte layout shared by the compute generator and procedural vertex shader.
// Packing fields in float4-sized blocks keeps the layout stable across Metal,
// Vulkan and Direct3D.
struct TerrainGrassBlade
{
    float4 rootWS;      // xyz = root position, w = height
    float4 normalWS;    // xyz = normal, w = halfWidth
    float4 groundColor; // rgb = satellite color, a = unused
    float4 data;        // x = random phase, y = green strength, z/w reserved
};

#endif
