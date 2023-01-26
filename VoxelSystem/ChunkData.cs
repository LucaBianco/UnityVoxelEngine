using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem
{
    public enum VoxelDirection
    {
        Forward,
        Backward,
        Right,
        Left,
        Up,
        Down
    }

    public struct Block
    {
        public byte type;   //8 bits for type (255 types + null type)
        public byte healthAndDirection; //5 bits health (32 hp) 3 bits direction (allows for 8 directions, while we need 6)

        public const byte NumberOfNonEmptyBlockTypes = 3;
    }

    public enum BlockType
    {
        Empty,
        Dirt,
        Grass,
        Stone
    }

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