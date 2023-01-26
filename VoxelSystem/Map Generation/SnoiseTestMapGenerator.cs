using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem.MapGeneration
{
    public class SnoiseTestMapGenerator : IMapGenerator
    {
        void IMapGenerator.Generate(ref Chunk[,,] chunkData, int3 mapSize, uint chunkSize, int seed)
        {
            float noiseScale = 1 / 16.0f;
            Noise.Seed = seed;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            float[,,] noise = Noise.Calc3D(mapSize.x * (int)chunkSize, mapSize.y * (int)chunkSize, mapSize.z * (int)chunkSize, noiseScale);
            sw.Stop();
            Debug.Log("Calc 3D Takes... " + sw.ElapsedMilliseconds + "ms");

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
                            for (int y = 0; y < chunkSize; y++)
                            {
                                for (int z = 0; z < chunkSize; z++)
                                {
                                    int3 finalPos = chunkPos + new int3(x, y, z);

                                    myChunkData.blocks[x, y, z].type = (byte)((noise[finalPos.x, finalPos.y, finalPos.z] + 1.0f) * 2.99f / 2.0f);
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