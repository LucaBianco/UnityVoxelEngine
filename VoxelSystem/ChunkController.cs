using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine.Assertions;

using VoxelSystem.MeshGeneration;


namespace VoxelSystem
{
    [RequireComponent(typeof(MeshFilter))]
    public class ChunkController : MonoBehaviour
    {
        private MeshFilter meshFilter = null;
        private MeshRenderer meshRenderer = null;

        Vector3[] vertices = null;
        List<int[]> subMeshTriangles = null;
        Vector2[] uvs = null;
        Material[] materials = null;

        bool recalculating = true;

        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            Mesh m = new Mesh();
            meshFilter.mesh = m;

            meshRenderer = GetComponent<MeshRenderer>();
        }

        private int3 worldPosition;
        public int3 WorldPosition 
        {
            get { 
                return worldPosition;
            }
            set { 
                worldPosition = value; 
                transform.position = new Vector3(value.x, value.y, value.z); 
            } 
        } 
        
        public void OnChunkDone(object sender, DoneMeshData dmd)
        {
            for (int i=0; i < dmd.positions.Count; i++)
            {
                if (dmd.positions[i].Equals(WorldPosition))
                {
                    vertices = dmd.verticesList[i];
                    subMeshTriangles = dmd.subMeshTrianglesList[i];
                    uvs = dmd.uvsList[i];

                    break;
                }

            }
        }

        private void Update()
        {
            if (recalculating)
            {
                if (vertices != null && subMeshTriangles != null)
                {
                    meshFilter.mesh.subMeshCount = subMeshTriangles.Count;

                    meshFilter.mesh.vertices = vertices;

                    for (int i=0; i < subMeshTriangles.Count; i++)
                        meshFilter.mesh.SetTriangles(subMeshTriangles[i], i, true);

                    meshFilter.mesh.uv = uvs;

                    meshFilter.mesh.RecalculateBounds();
                    meshFilter.mesh.RecalculateNormals();
                    meshFilter.mesh.RecalculateTangents();
                    recalculating = false;

                    meshRenderer.materials = materials;
                }
            }
        }

        public void SetMaterials(Material[] materials)
        {
            this.materials = materials;
        }
    }

}