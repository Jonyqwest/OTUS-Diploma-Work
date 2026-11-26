#pragma kernel CSMain

Texture2D<float4> Input0;
Texture2D<float4> Input1;
Texture2D<float4> Input2;
Texture2D<float4> Input3;
RWTexture2D<float4> Result;

[numthreads(8,8,1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    int size = Result.GetDimensions().x;
    int half = size / 2;

    int2 uv = id.xy;
    float2 localUV;

    if (uv.x < half && uv.y < half) {
        // нижний левый
        localUV = float2(uv.x, uv.y) / half;
        Result[uv] = Input0.SampleLevel(samplerlinear, localUV, 0);
    }
    else if (uv.x >= half && uv.y < half) {
        // нижний правый
        localUV = float2(uv.x - half, uv.y) / half;
        Result[uv] = Input1.SampleLevel(samplerlinear, localUV, 0);
    }
    else if (uv.x < half && uv.y >= half) {
        // верхний левый
        localUV = float2(uv.x, uv.y - half) / half;
        Result[uv] = Input2.SampleLevel(samplerlinear, localUV, 0);
    }
    else {
        // верхний правый
        localUV = float2(uv.x - half, uv.y - half) / half;
        Result[uv] = Input3.SampleLevel(samplerlinear, localUV, 0);
    }
}
