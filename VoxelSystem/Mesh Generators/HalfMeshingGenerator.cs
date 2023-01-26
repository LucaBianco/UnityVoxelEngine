using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem.MeshGeneration
{
    /**
      * <summary>Half mesh optimization approach. Less generation time, but more quads.</summary>
      */
    public class HalfGreedyMeshingGenerator : MeshGenerator
    {
        //Results stored
        List<Vector3[]> verticesList = new List<Vector3[]>();
        List<List<int[]>> subMeshTrianglesList = new List<List<int[]>>();
        List<Vector2[]> uvsList = new List<Vector2[]>();

        //Temporary variables
        List<Vector3> vertices = new List<Vector3>();
        List<List<int>> subMeshTriangles = new List<List<int>>();
        List<Vector2> uvs = new List<Vector2>();

        public HalfGreedyMeshingGenerator(MapInfo map, ref List<Chunk> chunks, ref Dictionary<int3, bool> emptyBorderBlocksMap, int threadIndex)
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

                //Slice along x+ (Right + Left faces)
                for (int x = 0; x < chunkSize; x++)
                {
                    Slice sliceR = new Slice(VoxelDirection.Right, chunkSize, vertices, subMeshTriangles, uvs);
                    Slice sliceL = new Slice(VoxelDirection.Left, chunkSize, vertices, subMeshTriangles, uvs);

                    //For every x, calculate the slice
                    for (int z = 0; z < chunkSize; z++)
                    {
                        for (int y = 0; y < chunkSize; y++)
                        {

                            if (isBlockEmpty(c, x + 1, y, z))
                                sliceR.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);

                            if (isBlockEmpty(c, x - 1, y, z))
                                sliceL.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);
                        }
                    }

                    sliceR.HalfGreedySimplify();
                    /*map.quads += */
                    sliceR.GenerateMesh();
                    sliceL.HalfGreedySimplify();
                    /*map.quads += */
                    sliceL.GenerateMesh();
                }

                //Slice along z+ (Forward + Backward faces)
                for (int z = 0; z < chunkSize; z++)
                {
                    Slice sliceF = new Slice(VoxelDirection.Forward, chunkSize, vertices, subMeshTriangles, uvs);
                    Slice sliceB = new Slice(VoxelDirection.Backward, chunkSize, vertices, subMeshTriangles, uvs);

                    //For every z, calculate the slice
                    for (int x = 0; x < chunkSize; x++)
                    {
                        for (int y = 0; y < chunkSize; y++)
                        {
                            if (isBlockEmpty(c, x, y, z + 1))
                                sliceF.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);

                            if (isBlockEmpty(c, x, y, z - 1))
                                sliceB.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);
                        }
                    }

                    sliceF.HalfGreedySimplify();
                    /*map.quads += */
                    sliceF.GenerateMesh();
                    sliceB.HalfGreedySimplify();
                    /*map.quads += */
                    sliceB.GenerateMesh();
                }

                //Slice along y+ (Top + Bottom faces)
                for (int y = 0; y < chunkSize; y++)
                {
                    Slice sliceU = new Slice(VoxelDirection.Up, chunkSize, vertices, subMeshTriangles, uvs);
                    Slice sliceD = new Slice(VoxelDirection.Down, chunkSize, vertices, subMeshTriangles, uvs);

                    //For every z, calculate the slice
                    for (int x = 0; x < chunkSize; x++)
                    {
                        for (int z = 0; z < chunkSize; z++)
                        {
                            if (isBlockEmpty(c, x, y + 1, z))
                                sliceU.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);

                            if (isBlockEmpty(c, x, y - 1, z))
                                sliceD.addQuad(new int3(x, y, z), c.blocks[x, y, z].type);
                        }
                    }

                    sliceU.HalfGreedySimplify();
                    /*map.quads += */
                    sliceU.GenerateMesh();
                    sliceD.HalfGreedySimplify();
                    /*map.quads += */
                    sliceD.GenerateMesh();
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
    }

}