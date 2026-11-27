namespace WorldTile
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Утилита для фильтрации и сглаживания карт высот (спутниковых DEM данных).
    /// Включает устранение резких пиков/дыр (Despike) и Гауссово сглаживание (Gaussian Blur).
    /// </summary>
    public static class HeightmapFilter
    {
        /// <summary>
        /// Фильтр гашения одиночных пиков и отвесных дыр (выбросов) на основе медианы соседей.
        /// Если перепад между текущей точкой и медианой ее соседей превышает thresholdMeters,
        /// точка считается артефактом/шумом и заменяется значением медианы.
        /// </summary>
        public static void Despike(float[,] heights, float thresholdMeters)
        {
            if (heights == null || thresholdMeters <= 0f) return;

            int width = heights.GetLength(0);
            int height = heights.GetLength(1);
            if (width < 3 || height < 3) return;

            float[,] original = (float[,])heights.Clone();
            float[] neighbors = new float[8];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cur = original[x, y];
                    int n = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height) continue;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            if (nx < 0 || nx >= width) continue;

                            neighbors[n++] = original[nx, ny];
                        }
                    }

                    if (n >= 3)
                    {
                        // Быстрая сортировка вставками для небольшого массива (n <= 8)
                        for (int i = 1; i < n; i++)
                        {
                            float key = neighbors[i];
                            int j = i - 1;
                            while (j >= 0 && neighbors[j] > key)
                            {
                                neighbors[j + 1] = neighbors[j];
                                j--;
                            }
                            neighbors[j + 1] = key;
                        }

                        float median = (n % 2 == 1)
                            ? neighbors[n / 2]
                            : 0.5f * (neighbors[n / 2 - 1] + neighbors[n / 2]);

                        if (Mathf.Abs(cur - median) > thresholdMeters)
                        {
                            heights[x, y] = median;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Разделимое 2D Гауссово сглаживание карты высот с сохранением краев.
        /// </summary>
        /// <param name="heights">Массив высот</param>
        /// <param name="sigma">Сила размытия (радиус / сигма)</param>
        /// <param name="iterations">Количество проходов</param>
        public static void GaussianBlur(float[,] heights, float sigma, int iterations = 1)
        {
            if (heights == null || sigma <= 0.01f || iterations <= 0) return;

            int width = heights.GetLength(0);
            int height = heights.GetLength(1);
            if (width < 2 || height < 2) return;

            int radius = Mathf.Clamp(Mathf.CeilToInt(2f * sigma), 1, 4);
            int kernelSize = radius * 2 + 1;
            float[] weights = new float[kernelSize];
            float twoSigmaSq = 2f * sigma * sigma;
            float sumWeights = 0f;

            for (int i = -radius; i <= radius; i++)
            {
                float w = Mathf.Exp(-(i * i) / twoSigmaSq);
                weights[i + radius] = w;
                sumWeights += w;
            }

            float invSum = 1f / sumWeights;
            for (int i = 0; i < kernelSize; i++)
            {
                weights[i] *= invSum;
            }

            float[,] temp = new float[width, height];

            for (int it = 0; it < iterations; it++)
            {
                // Горизонтальный проход
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0f;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int nx = x + k;
                            if (nx < 0) nx = 0;
                            else if (nx >= width) nx = width - 1;

                            sum += heights[nx, y] * weights[k + radius];
                        }
                        temp[x, y] = sum;
                    }
                }

                // Вертикальный проход
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float sum = 0f;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int ny = y + k;
                            if (ny < 0) ny = 0;
                            else if (ny >= height) ny = height - 1;

                            sum += temp[x, ny] * weights[k + radius];
                        }
                        heights[x, y] = sum;
                    }
                }
            }
        }

        /// <summary>
        /// Перегрузка Despike для массива short[,] с промежуточной float-точностью.
        /// </summary>
        public static void Despike(short[,] heights, float thresholdMeters)
        {
            if (heights == null) return;
            int width = heights.GetLength(0);
            int height = heights.GetLength(1);

            float[,] floatMap = new float[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    floatMap[x, y] = heights[x, y];

            Despike(floatMap, thresholdMeters);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int rounded = Mathf.RoundToInt(floatMap[x, y]);
                    if (rounded > short.MaxValue) rounded = short.MaxValue;
                    else if (rounded < short.MinValue) rounded = short.MinValue;
                    heights[x, y] = (short)rounded;
                }
            }
        }

        /// <summary>
        /// Перегрузка GaussianBlur для массива short[,] с промежуточной float-точностью.
        /// </summary>
        public static void GaussianBlur(short[,] heights, float sigma, int iterations = 1)
        {
            if (heights == null) return;
            int width = heights.GetLength(0);
            int height = heights.GetLength(1);

            float[,] floatMap = new float[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    floatMap[x, y] = heights[x, y];

            GaussianBlur(floatMap, sigma, iterations);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int rounded = Mathf.RoundToInt(floatMap[x, y]);
                    if (rounded > short.MaxValue) rounded = short.MaxValue;
                    else if (rounded < short.MinValue) rounded = short.MinValue;
                    heights[x, y] = (short)rounded;
                }
            }
        }
    }
}
