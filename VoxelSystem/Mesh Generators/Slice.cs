using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using VoxelSystem;


namespace VoxelSystem
{
    public struct Quad
    {
        public Vector3 pos;
        public int2 size;
        public byte type;
    }

    public class Slice
    {
        public List<Vector3> vertices;
        public List<List<int>> triangles;
        public List<Vector2> uvs;

        VoxelDirection direction;
        Quad[,] quads;
        int chunkSide;

        public Slice(VoxelDirection direction, int chunkSide, List<Vector3> vertices, List<List<int>> triangles, List<Vector2> uvs)
        {
            this.direction = direction;
            this.chunkSide = chunkSide;

            quads = new Quad[chunkSide, chunkSide];

            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
        }

        public void addQuad(int3 pos, byte type)
        {
            int i, j;
            switch (direction)
            {
                case VoxelDirection.Left:
                case VoxelDirection.Right:
                    i = pos.z;
                    j = pos.y;
                    break;

                case VoxelDirection.Forward:
                case VoxelDirection.Backward:
                    i = pos.x;
                    j = pos.y;
                    break;

                case VoxelDirection.Up:
                case VoxelDirection.Down:
                    i = pos.x;
                    j = pos.z;
                    break;

                default:
                    i = 0;
                    j = 0;
                    break;
            }

            quads[i, j].pos = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
            quads[i, j].size = new int2(1, 1);
            quads[i, j].type = type;
        }

        public void HalfGreedySimplify()
        {
            // For each vertical column
            for (int i = 0; i < chunkSide; i++)
            {
                //Vertical Simplify
                for (int j = 1; j < chunkSide; j++)
                {
                    if (quads[i, j].type == quads[i, j - 1].type)
                    {
                        quads[i, j].size.y += quads[i, j - 1].size.y;
                        quads[i, j - 1].size = new int2(0, 0);
                    }
                }
            }
        }

        public void GreedySimplify()
        {
            HalfGreedySimplify();

            //Starting from the 2nd column,
            // look left, if quad has same height add ...
            for (int i = 1; i < chunkSide; i++)
            {
                //Vertical scan
                for (int j = 0; j < chunkSide; j++)
                {
                    if (quads[i, j].size.x != 0)
                    {
                        if (quads[i, j].size.y == quads[i - 1, j].size.y &&
                            quads[i, j].type == quads[i - 1, j].type)
                        {
                            quads[i, j].size.x += quads[i - 1, j].size.x;
                            quads[i - 1, j].size = new int2(0, 0);
                        }
                    }
                }
            }
        }

        public void GenerateMesh()
        {
            //int quadCount = 0;

            for (int i = 0; i < chunkSide; i++)
            {
                for (int j = 0; j < chunkSide; j++)
                {
                    /*quadCount +=*/
                    addMeshQuad(quads[i, j]);
                }
            }

            //return quadCount;
        }

        void /*int*/ addMeshQuad(Quad q)
        {
            if (q.type == 0 || (q.size.x == 0 || q.size.y == 0))
                return /*0*/;

            int subMeshIndex = q.type - 1; //Submesh index is quad type id - 1

            Vector3 position = q.pos;
            float iScale = q.size.x;
            float jScale = -q.size.y;

            //Depending on which face of the (cubic) voxel we're drawing, add vertices accordingly to form the desired Quad
            switch (this.direction)
            {
                case VoxelDirection.Backward:
                    position += Vector3.right + Vector3.up - Vector3.forward;
                    vertices.Add(position + Vector3.forward + Vector3.left * iScale);
                    vertices.Add(position + Vector3.forward);
                    vertices.Add(position + Vector3.forward + Vector3.left * iScale + Vector3.up * jScale);
                    vertices.Add(position + Vector3.forward + Vector3.up * jScale);
                    break;
                case VoxelDirection.Forward:
                    position += Vector3.right + Vector3.up + Vector3.forward;
                    vertices.Add(position);
                    vertices.Add(position + Vector3.left * iScale);
                    vertices.Add(position + Vector3.up * jScale);
                    vertices.Add(position + Vector3.left * iScale + Vector3.up * jScale);
                    break;
                case VoxelDirection.Right:
                    iScale *= -1;
                    position += Vector3.up + Vector3.right + Vector3.forward;
                    vertices.Add(position + Vector3.forward * iScale);
                    vertices.Add(position);
                    vertices.Add(position + Vector3.forward * iScale + Vector3.up * jScale);
                    vertices.Add(position + Vector3.up * jScale);
                    break;
                case VoxelDirection.Left:
                    iScale *= -1;
                    position += Vector3.up + Vector3.right + Vector3.forward;
                    vertices.Add(position + Vector3.left);
                    vertices.Add(position + Vector3.left + Vector3.forward * iScale);
                    vertices.Add(position + Vector3.left + Vector3.up * jScale);
                    vertices.Add(position + Vector3.left + Vector3.forward * iScale + Vector3.up * jScale);
                    break;
                case VoxelDirection.Down:
                    position += -Vector3.up + Vector3.right + Vector3.forward;
                    vertices.Add(position + Vector3.up);
                    vertices.Add(position + Vector3.up + Vector3.left * iScale);
                    vertices.Add(position + Vector3.up + Vector3.forward * jScale);
                    vertices.Add(position + Vector3.up + Vector3.forward * jScale + Vector3.left * iScale);
                    break;
                case VoxelDirection.Up:
                    position += Vector3.up + Vector3.right + Vector3.forward;
                    vertices.Add(position + Vector3.forward * jScale);
                    vertices.Add(position + Vector3.forward * jScale + Vector3.left * iScale);
                    vertices.Add(position);
                    vertices.Add(position + Vector3.left * iScale);
                    break;
            }

            //Add the newly created vertices clockwise to form triangles
            triangles[subMeshIndex].Add(vertices.Count - 4);
            triangles[subMeshIndex].Add(vertices.Count - 3);
            triangles[subMeshIndex].Add(vertices.Count - 2);
            triangles[subMeshIndex].Add(vertices.Count - 3);
            triangles[subMeshIndex].Add(vertices.Count - 1);
            triangles[subMeshIndex].Add(vertices.Count - 2);

            //Texture UVs
            uvs.Add(new Vector2(iScale, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(iScale, jScale));
            uvs.Add(new Vector2(0.0f, jScale));
            //return 1;
        }
    }
}