using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;

namespace VoxelSystem
{
    public static class MapGeneratorUtils
    {
        /**
         * <summary>This function generates a heightmap using layered 2D simplex noise.</summary>
         * <param name="width">Map Width in blocks.</param>
         * <param name="height">Map Height in blocks.</param>
         * <param name="baseNoiseScale">The noise scale of Level 0.</param>
         * <param name="seed">Seed for the Simplex Noise.</param>
         * <param name="octaves">Number of levels/layers. More = more complex irregular map, more computation needed.</param>
         */
        public static float[,] GenerateBaseHeightMap(int width, int height, int octaves = 4, int seed = 0, float baseNoiseScale = 1 / 256.0f)
        {
            //Allocate 2D array for heightmap
            float[,] heightMap = new float[width, height];

            //Init to zero
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    heightMap[i, j] = 0.0f;

            //Layered 2D simplex noise
            Noise.Seed = seed;
            for (int oct = 0; oct < octaves; oct++)
            {
                float[,] data = Noise.Calc2D(width, height, baseNoiseScale * Mathf.Pow(2, oct));

                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        heightMap[i, j] += data[i, j] / Mathf.Pow(2f, oct);
            }

            //Normalize from 0 to 1
            // Find min and max
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    if (heightMap[i, j] < min)
                        min = heightMap[i, j];
                    if (heightMap[i, j] > max)
                        max = heightMap[i, j];
                }

            // Normalize y1 = (y0 - min) / (max - min)
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    heightMap[i, j] = (heightMap[i, j] - min) / (max - min);


            return heightMap;
        }

        public static void SmoothAroundHeigt(ref float[,] heightMap, float height, float power = 0.5f)
        {
            int mapWidth = heightMap.GetLength(0);
            int mapHeight = heightMap.GetLength(1);

            for (int i = 0; i < mapWidth; i++)
                for (int j = 0; j < mapHeight; j++)
                {
                    float factor = Mathf.Pow(Mathf.Abs(heightMap[i, j] - height), power);
                    heightMap[i, j] = heightMap[i, j] * (factor) + height * (1.0f - factor);
                }
        }
    }
}
