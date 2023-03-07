using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem
{
    public class Chunk
    {
        public int3 WorldPosition { get; private set; }
        public Block[,,] blocks;

        public Chunk(int3 worldPosition, uint chunkSize)
        {
            WorldPosition = worldPosition;
            blocks = new Block[chunkSize, chunkSize, chunkSize];
        }
    }
}