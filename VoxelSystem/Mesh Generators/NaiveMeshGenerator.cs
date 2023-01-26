using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem.MeshGeneration
{
    /**
      * <summary>Naive approach. Lots of quads.</summary>
      */
    public class NaiveMeshGenerator : MeshGenerator
    {
        //Results stored
        List<Vector3[]> verticesList = new List<Vector3[]>();
        List<List<int[]>> subMeshTrianglesList = new List<List<int[]>>();
        List<Vector2[]> uvsList = new List<Vector2[]>();

        //Temporary variables
        List<Vector3> vertices = new List<Vector3>();
        List<List<int>> subMeshTriangles = new List<List<int>>();
        List<Vector2> uvs = new List<Vector2>();

        public NaiveMeshGenerator(MapInfo map, ref List<Chunk> chunks, ref Dictionary<int3, bool> emptyBorderBlocksMap, int threadIndex)
            : base(map, ref chunks, ref emptyBorderBlocksMap, threadIndex)
        {
        }

        public override void Generate()
        {
            verticesList.Clear();
            subMeshTrianglesList.Clear();
            uvsList.Clear();

            foreach (Chunk c in chunks)
            {
                //Clear data
                vertices.Clear();

                subMeshTriangles.Clear();
                for (int i = 0; i < Block.NumberOfNonEmptyBlockTypes; i++)
                    subMeshTriangles.Add(new List<int>());

                uvs.Clear();

                //Generate the mesh
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int y = 0; y < chunkSize; y++)
                    {
                        for (int z = 0; z < chunkSize; z++)
                        {
                            if (isBlockEmpty(c, x, y, z))
                                continue;

                            Vector3 blockPosition = new Vector3(x, y, z);
                            int subMeshIndex = c.blocks[x, y, z].type - 1; //Submesh index is block type id - 1

                            if (isBlockEmpty(c, x, y + 1, z))
                                addQuad(blockPosition, VoxelDirection.Up, subMeshIndex);
                            if (isBlockEmpty(c, x, y - 1, z))
                                addQuad(blockPosition, VoxelDirection.Down, subMeshIndex);

                            if (isBlockEmpty(c, x - 1, y, z))
                                addQuad(blockPosition, VoxelDirection.Left, subMeshIndex);
                            if (isBlockEmpty(c, x + 1, y, z))
                                addQuad(blockPosition, VoxelDirection.Right, subMeshIndex);

                            if (isBlockEmpty(c, x, y, z + 1))
                                addQuad(blockPosition, VoxelDirection.Forward, subMeshIndex);
                            if (isBlockEmpty(c, x, y, z - 1))
                                addQuad(blockPosition, VoxelDirection.Backward, subMeshIndex);

                        }
                    }
                }

                //Save created mesh
                verticesList.Add(vertices.ToArray());

                List<int[]> trisList = new List<int[]>();
                for (int i = 0; i < subMeshTriangles.Count; i++)
                    trisList.Add(subMeshTriangles[i].ToArray());
                subMeshTrianglesList.Add(trisList);

                uvsList.Add(uvs.ToArray());
            }

            //Invoke Done event and return the mesh data for the chunks managed by this thread
            OnDone(verticesList, subMeshTrianglesList, uvsList);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool isBlockEmpty(Chunk c, int x, int y, int z)
        {
            //Is the block outside of Chunk c?
            if (x < 0 || y < 0 || z < 0 || x >= chunkSize || y >= chunkSize || z >= chunkSize)
            {
                //Calculate the world position of the block
                int3 worldPos = c.WorldPosition + new int3(x, y, z);

                //If the world position is outside the map
                if (worldPos.x < 0 || worldPos.y < 0 || worldPos.z < 0 ||
                    worldPos.x >= mapBlockSize.x || worldPos.y >= mapBlockSize.y || worldPos.z >= mapBlockSize.z)
                {
                    return true;    //Return empty
                }

                //Otherwise, get the answer from the pre-computed hashmap (dictionary)
                return emptyBorderBlocksMap[worldPos];
            }

            //If it is within the chunk
            //  if its type is 0, 
            if (c.blocks[x, y, z].type == 0)
                return true;    //It's empty

            //If none of that is true, it's not empty
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void addQuad(Vector3 position, VoxelDirection direction, int subMeshIndex)
        {
            //Depending on which face of the (cubic) voxel we're drawing, add vertices accordingly to form the desired Quad
            switch (direction)
            {
                case VoxelDirection.Forward:
                    vertices.Add(position + Vector3.forward + Vector3.left);
                    vertices.Add(position + Vector3.forward);
                    vertices.Add(position + Vector3.forward + Vector3.left + Vector3.up);
                    vertices.Add(position + Vector3.forward + Vector3.up);
                    break;
                case VoxelDirection.Backward:
                    vertices.Add(position);
                    vertices.Add(position + Vector3.left);
                    vertices.Add(position + Vector3.up);
                    vertices.Add(position + Vector3.left + Vector3.up);
                    break;
                case VoxelDirection.Right:
                    vertices.Add(position + Vector3.forward);
                    vertices.Add(position);
                    vertices.Add(position + Vector3.forward + Vector3.up);
                    vertices.Add(position + Vector3.up);
                    break;
                case VoxelDirection.Left:
                    vertices.Add(position + Vector3.left);
                    vertices.Add(position + Vector3.left + Vector3.forward);
                    vertices.Add(position + Vector3.left + Vector3.up);
                    vertices.Add(position + Vector3.left + Vector3.forward + Vector3.up);
                    break;
                case VoxelDirection.Up:
                    vertices.Add(position + Vector3.up);
                    vertices.Add(position + Vector3.up + Vector3.left);
                    vertices.Add(position + Vector3.up + Vector3.forward);
                    vertices.Add(position + Vector3.up + Vector3.forward + Vector3.left);
                    break;
                case VoxelDirection.Down:
                    vertices.Add(position + Vector3.forward);
                    vertices.Add(position + Vector3.forward + Vector3.left);
                    vertices.Add(position);
                    vertices.Add(position + Vector3.left);
                    break;
            }

            //Add the newly added vertices clockwise to form triangles
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 4);
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 3);
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 2);
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 3);
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 1);
            subMeshTriangles[subMeshIndex].Add(vertices.Count - 2);

            //Add UVs
            uvs.Add(new Vector2(1.0f, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(1.0f, 1.0f));
            uvs.Add(new Vector2(0.0f, 1.0f));
        }
    }

}