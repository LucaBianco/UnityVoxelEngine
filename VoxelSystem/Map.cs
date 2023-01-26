using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;

using VoxelSystem.MeshGeneration;
using VoxelSystem.MapGeneration;

namespace VoxelSystem
{
    public enum MapState
    {
        Idle,
        GeneratingMap,
        GeneratingMeshes,
        Updating,
    }

    [Serializable]
    public struct MapInfo
    {
        public int3 size;
        public uint chunkSize;
    }

    public class Map : MonoBehaviour
    {
        //Prefabs
        public GameObject ChunkPrefab;

        //Map Data
        public MapInfo mapInfo; //EDITABLE

        private MapState _state = MapState.Idle;
        public MapState State { get => _state; set { Debug.Log("The state is now: " + value.ToString()); _state = value; } }

        //Map Generation
        public MapGeneratorType mapGeneratorType = MapGeneratorType.SnoiseTestMapGenerator; //EDITABLE
        public int seed = 0;    //EDITABLE
        private IMapGenerator mapGenerator;
        
        private Chunk[,,] chunks = null;
        private List<ChunkController> chunkControllers;
        bool mapGenerationDone = false;

        //Mesh Generation
        public MeshGeneratorType meshGeneratorType = MeshGeneratorType.NaiveMeshGenerator; //EDITABLE
        MultiThreadMeshGenerationController meshGenerationController = null;

        public Material[] Materials;            //EDITABLE



        void Start()
        {
            Assert.IsTrue(Materials.Length == Block.NumberOfNonEmptyBlockTypes);
            meshGenerationController = new MultiThreadMeshGenerationController(meshGeneratorType);
        }

        void Update()
        {
            switch (State)
            {
                case MapState.GeneratingMap:
                    if (mapGenerationDone)
                    {
                        State = MapState.GeneratingMeshes;
                        startMeshGeneration();
                    }
                    break;

                case MapState.GeneratingMeshes:
                    if (meshGenerationController.AllGenerationDone)
                        State = MapState.Idle;

                    break;

                case MapState.Idle:
                    if (chunks == null)
                    {
                        mapGenerationDone = false;
                        State = MapState.GeneratingMap;
                        startMapGeneration();
                    }
                    break;

                default:
                    State = MapState.Idle;
                    break;
            }
        }

        private void startMapGeneration()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            switch (mapGeneratorType)
            {
                case MapGeneratorType.FlatDirtMapGenerator:
                    mapGenerator = new FlatDirtMapGenerator();
                    break;

                case MapGeneratorType.SnoiseTestMapGenerator:
                    mapGenerator = new SnoiseTestMapGenerator();
                    break;

                case MapGeneratorType.MapGenerator1:
                    mapGenerator = new MapGenerator1();
                    break;

                default:
                    throw new NotImplementedException();
            }

            chunks = new Chunk[mapInfo.size.x, mapInfo.size.y, mapInfo.size.z];

            mapGenerator.Generate(ref chunks, mapInfo.size, mapInfo.chunkSize, seed);

            mapGenerationDone = true; 

            watch.Stop();
            Debug.Log("Time taken for generating map: " + watch.ElapsedMilliseconds + "ms");
        }

        private void startMeshGeneration()
        {
            List<Chunk> chunksList = new List<Chunk>();
            chunkControllers = new List<ChunkController>();
            for (int i = 0; i < mapInfo.size.x; i++)
                for (int j = 0; j < mapInfo.size.y; j++)
                    for (int k = 0; k < mapInfo.size.z; k++)
                    {
                        chunksList.Add(chunks[i, j, k]);

                        GameObject chunkGameObject = Instantiate(ChunkPrefab, transform);

                        ChunkController chunkController = chunkGameObject.GetComponent<ChunkController>();
                        chunkController.WorldPosition = chunks[i, j, k].WorldPosition;
                        chunkController.SetMaterials(Materials);

                        chunkControllers.Add(chunkController);
                    }

            meshGenerationController.StartGeneration(mapInfo, ref chunks, ref chunksList, ref chunkControllers);
        }
    }
}