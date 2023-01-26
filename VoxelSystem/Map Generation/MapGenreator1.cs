using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static VoxelSystem.MapGeneratorUtils;

namespace VoxelSystem.MapGeneration
{
    public class MapGenerator1 : IMapGenerator
    {
        void IMapGenerator.Generate(ref Chunk[,,] chunkData, int3 mapSize, uint chunkSize, int seed)
        {
            int mapWidth = (int)(mapSize.x * chunkSize);
            int mapHeight = (int)(mapSize.z * chunkSize);

            float[,] heightMap = GenerateBaseHeightMap(mapWidth, mapHeight, 6, seed);
            SmoothAroundHeigt(ref heightMap, 0.25f, 0.8f);
            // Hydraulic erosion?


            for (int i = 0; i < mapSize.x; i++)
            {
                for (int j = 0; j < mapSize.y; j++)
                {
                    for (int k = 0; k < mapSize.z; k++)
                    {
                        //For Every Chunk
                        int3 chunkPos = new int3((int)(i * chunkSize), (int)(j * chunkSize), (int)(k * chunkSize));

                        chunkData[i, j, k] = new Chunk(chunkPos, chunkSize); // create new chunk data
                        Chunk myChunkData = chunkData[i, j, k];
                        myChunkData.blocks = new Block[chunkSize, chunkSize, chunkSize];



                        //Generate Chunk
                        for (int x = 0; x < chunkSize; x++)
                        {
                            for (int z = 0; z < chunkSize; z++)
                            {
                                for (int y = 0; y < chunkSize; y++)
                                {
                                    int3 finalPos = chunkPos + new int3(x, y, z);
                                    byte blockType = (byte)BlockType.Empty;

                                    float dirtHeight = 4 - heightMap[finalPos.x, finalPos.z]*2.0f;
                                    float height = (heightMap[finalPos.x, finalPos.z] * mapSize.y * chunkSize);

                                    if (finalPos.y < height-1 - dirtHeight)
                                        blockType = (byte)BlockType.Stone;
                                    else if (finalPos.y < height - 1)
                                        blockType = (byte)BlockType.Dirt;
                                    else if (finalPos.y < height)
                                        blockType = (byte)(BlockType.Grass);

                                    myChunkData.blocks[x, y, z].type = blockType;
                                    //                                               health      direction
                                    myChunkData.blocks[x, y, z].healthAndDirection = (0b11111) | ((0b000) << 5);
                                }
                            }
                        }
                        //End Chunk Generation
                    }
                }
            }
        } // End void Generate

    } // End Class
}