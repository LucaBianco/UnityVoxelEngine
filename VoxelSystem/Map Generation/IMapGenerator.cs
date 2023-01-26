using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using SimplexNoise;

namespace VoxelSystem.MapGeneration
{
    public enum MapGeneratorType
    {
        FlatDirtMapGenerator,
        SnoiseTestMapGenerator,
        MapGenerator1
    }

    public interface IMapGenerator
    {
        void Generate(ref Chunk[,,] chunkData, int3 mapSize, uint chunkSize, int seed);
    }
}