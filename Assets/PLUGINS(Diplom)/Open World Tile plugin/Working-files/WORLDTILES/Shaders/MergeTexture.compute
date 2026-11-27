#pragma kernel CSMain

// Универсальный массив входных текстур
// В Unity compute shader нельзя иметь массивы Texture2D напрямую,
// поэтому мы передаём их как Texture2DArray.
Texture2DArray<float4> Inputs;

// Выход
RWTexture2D<float4> Result;

// Параметры
int tileSize;     // ширина одного тайла
int tileCount;    // количество тайлов по оси (например, 2, 4, 8...)
SamplerState samplerLinear;

[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint2 size;
    Result.GetDimensions(size.x, size.y);

    if (id.x >= size.x || id.y >= size.y)
        return;

    // вычисляем в какой тайл попадает текущий пиксель
    int tileX = id.x / tileSize;
    int tileY = id.y / tileSize;

    int index = tileY * tileCount + tileX;

    // локальные координаты внутри тайла
    int localX = id.x % tileSize;
    int localY = id.y % tileSize;

    // читаем пиксель из массива текстур
    float4 color = Inputs.Load(int4(localX, localY, index, 0));
    Result[id.xy] = color;
}
