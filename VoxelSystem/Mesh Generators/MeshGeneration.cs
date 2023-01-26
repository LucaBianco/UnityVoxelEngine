using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.VisualScripting;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.Linq;
using System;

namespace VoxelSystem.MeshGeneration
{
    public class MultiThreadMeshGenerationController
    {
        public bool AllGenerationDone { get; private set; } = false;

        uint numThreads = 1;
        Thread[] threads;

        MeshGeneratorType meshGeneratorType;
        MeshGenerator[] meshGenerators;
        bool[] doneThreads;

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        Dictionary<int3, bool> emptyBoundaryBlocksMap = new Dictionary<int3, bool>();

        Chunk[,,] chunkData = null;

        public MultiThreadMeshGenerationController(MeshGeneratorType meshGeneratorType)
        {
            this.meshGeneratorType = meshGeneratorType;
        }

        public void StartGeneration(MapInfo map, ref Chunk[,,] chunkData, ref List<Chunk> allChunks, ref List<ChunkController> allChunkControllers)
        {
            //Store map data
            this.chunkData = chunkData;

            //Reset Done flag to False
            AllGenerationDone = false;
            stopwatch.Start();

            //Basic sanity checks
            Assert.AreEqual(allChunks.Count, allChunkControllers.Count);

            //Calc num threads
            numThreads = 1;
            if (SystemInfo.processorCount > 2)
            {
                numThreads = (uint)(SystemInfo.processorCount - 2);
            }
            numThreads = (uint) Mathf.Min((int)numThreads, Mathf.Max(allChunks.Count/128, 1));

            Debug.Log("# threads = " + numThreads);

            //Precompute empty boundary blocks
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            precomputeEmptyBoundaryBlocksMap(map);
            watch.Stop();
            Debug.Log("Precompute took " + watch.ElapsedMilliseconds + " ms Empty block count = " + emptyBoundaryBlocksMap.Count);

            //Init data for mesh generators
            threads = new Thread[numThreads];
            meshGenerators = new MeshGenerator[numThreads];
            doneThreads = new bool[numThreads];

            int chunksCount = allChunks.Count; //Total number of chunks
            int threadDataStart = 0;                        //Start of i-th thread data pool
            int threadDataEnd = chunksCount / (int)numThreads; //End of i-th thread data pool

            //For every thread
            for (int i = 0; i < numThreads; i++)
            {
                if (i == numThreads - 1) //The last thread 
                    threadDataEnd = chunksCount - 1; // has an extended data pool to the last chunk.

                int threadDataCount = threadDataEnd - threadDataStart + 1;

                //Log workload data pool info
                Debug.Log("Start=" + threadDataStart + " - End=" + threadDataEnd + " - Load=" + threadDataCount + " - Tot=" + chunksCount);

                //Get 
                List<Chunk> myChunks = allChunks.GetRange(threadDataStart, threadDataCount);
                List<ChunkController> myChunkControllers = allChunkControllers.GetRange(threadDataStart, threadDataCount);

                //Init "Done" Flag list - defaults to false
                doneThreads[i] = false;

                //Create i-th Mesh Generator of chosen type
                switch (meshGeneratorType)
                {
                    case MeshGeneratorType.NaiveMeshGenerator:
                        meshGenerators[i] = new NaiveMeshGenerator(map, ref myChunks, ref emptyBoundaryBlocksMap, i);
                        break;

                    case MeshGeneratorType.HalfGreedyMeshingGenerator:
                        meshGenerators[i] = new HalfGreedyMeshingGenerator(map, ref myChunks, ref emptyBoundaryBlocksMap, i);
                        break;

                    case MeshGeneratorType.FullGreedyMeshingGenerator:
                        meshGenerators[i] = new FullGreedyMeshingGenerator(map, ref myChunks, ref emptyBoundaryBlocksMap, i);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                //Event subscription
                meshGenerators[i].Done += OnThreadDone;
                foreach(ChunkController cc in myChunkControllers)
                    meshGenerators[i].Done += cc.OnChunkDone;

                //Instantiate new thread and start it
                MeshGenerator mg = meshGenerators[i];

                threads[i] = new Thread(() => mg.Generate());
                threads[i].Start();

                threadDataStart = threadDataEnd + 1;
                threadDataEnd += chunksCount / (int)numThreads;
            }
        }

        void precomputeEmptyBoundaryBlocksMap(MapInfo map)
        {
            //Chunk[,,] mapChunkData = map.getChunkData();

            //Scan planes x=x1 and x=x2
            for (int cx = 1; cx < map.size.x; cx++)
            {
                int x1 = cx * (int)map.chunkSize - 1;
                int x2 = x1 + 1;

                int x1x = x1 % (int)map.chunkSize;
                int x2x = x2 % (int)map.chunkSize;

                for (int cy = 0; cy < map.size.y; cy++)
                    for (int cz = 0; cz < map.size.z; cz++)
                    {
                        Block[,,] chunk1Blocks = chunkData[cx-1, cy, cz].blocks;
                        Block[,,] chunk2Blocks = chunkData[cx, cy, cz].blocks;

                        int yy_0 = cy * (int)map.chunkSize;
                        int zz_0 = cz * (int)map.chunkSize;

                        for (int yy = 0; yy < map.chunkSize; yy++)
                        {
                            int y = yy_0 + yy;

                            for (int zz = 0; zz < map.chunkSize; zz++)
                            {
                                int z = zz_0 + zz;

                                int3 p1 = new int3(x1, y, z); //World position p1
                                if (!emptyBoundaryBlocksMap.ContainsKey(p1))
                                {
                                    emptyBoundaryBlocksMap.Add(p1, (chunk1Blocks[x1x, yy, zz].type == 0));
                                }

                                int3 p2 = new int3(x2, y, z); //World position p2
                                if (!emptyBoundaryBlocksMap.ContainsKey(p2))
                                {
                                    emptyBoundaryBlocksMap.Add(p2, (chunk2Blocks[x2x, yy, zz].type == 0));
                                }
                            }
                        }
                    }
            }

            //Scan planes y=y1 and y=y2
            for (int cy = 1; cy < map.size.y; cy++)
            {
                int y1 = cy * (int)map.chunkSize - 1;
                int y2 = y1 + 1;

                int y1y = y1 % (int)map.chunkSize;
                int y2y = y2 % (int)map.chunkSize;

                for (int cx = 0; cx < map.size.x; cx++)
                    for (int cz = 0; cz < map.size.z; cz++)
                    {
                        Block[,,] chunk1Blocks = chunkData[cx, cy-1, cz].blocks;
                        Block[,,] chunk2Blocks = chunkData[cx, cy, cz].blocks;

                        int xx_0 = cx * (int)map.chunkSize;
                        int zz_0 = cz * (int)map.chunkSize;

                        for (int xx = 0; xx < map.chunkSize; xx++)
                        {
                            int x = xx_0 + xx;

                            for (int zz = 0; zz < map.chunkSize; zz++)
                            {
                                int z = zz_0 + zz;

                                int3 p1 = new int3(x, y1, z);
                                if (!emptyBoundaryBlocksMap.ContainsKey(p1))
                                {
                                    emptyBoundaryBlocksMap.Add(p1, (chunk1Blocks[xx, y1y, zz].type == 0));
                                }

                                int3 p2 = new int3(x, y2, z);
                                if (!emptyBoundaryBlocksMap.ContainsKey(p2))
                                {
                                    emptyBoundaryBlocksMap.Add(p2, (chunk2Blocks[xx, y2y, zz].type == 0));
                                }
                            }
                        }
                    }
            }

            //Scan planes z=z1 and z=z2
            for (int cz = 1; cz < map.size.z; cz++)
            {
                int z1 = cz * (int)map.chunkSize - 1;
                int z2 = z1 + 1;

                int z1z = z1 % (int)map.chunkSize;
                int z2z = z2 % (int)map.chunkSize;

                for (int cx = 0; cx < map.size.x; cx++)
                    for (int cy = 0; cy < map.size.y; cy++)
                    {
                        Block[,,] chunk1Blocks = chunkData[cx, cy, cz-1].blocks;
                        Block[,,] chunk2Blocks = chunkData[cx, cy, cz].blocks;

                        int xx_0 = cx * (int)map.chunkSize;
                        int yy_0 = cy * (int)map.chunkSize;

                        for (int xx = 0; xx < map.chunkSize; xx++)
                        {
                            int x = xx_0 + xx;

                            for (int yy = 0; yy < map.chunkSize; yy++)
                            {
                                int y = yy_0 + yy;

                                int3 p1 = new int3(x, y, z1);
                                if (!emptyBoundaryBlocksMap.ContainsKey(p1))
                                {
                                    emptyBoundaryBlocksMap.Add(p1, (chunk1Blocks[xx, yy, z1z].type == 0));
                                }

                                int3 p2 = new int3(x, y, z2);
                                if (!emptyBoundaryBlocksMap.ContainsKey(p2))
                                {
                                    emptyBoundaryBlocksMap.Add(p2, (chunk2Blocks[xx, yy, z2z].type == 0));
                                }
                            }
                        }
                    }
            }
        }

        void OnThreadDone(object sender, DoneMeshData dmd)
        {
            Debug.Log("Thread " + dmd.workloadIndex + " done!");

            Assert.IsFalse(dmd.workloadIndex < 0 || dmd.workloadIndex >= numThreads);

            doneThreads[dmd.workloadIndex] = true;

            bool allDone = true;
            foreach (bool t in doneThreads)
                allDone = allDone && t;

            if (allDone)
            {
                AllGenerationDone = true;
                stopwatch.Stop();
                Debug.Log(numThreads + "-threaded mesh generation took " + stopwatch.ElapsedMilliseconds + "ms");
            }
        }
    }

    #region Mesh Generator Abstract Class
    public enum MeshGeneratorType
    {
        NaiveMeshGenerator,
        HalfGreedyMeshingGenerator,
        FullGreedyMeshingGenerator
    }

    public struct DoneMeshData
    {
        public int workloadIndex;
        public List<Vector3[]> verticesList;
        public List<List<int[]>> subMeshTrianglesList;
        public List<Vector2[]> uvsList;
        public List<int3> positions;
    }

    public delegate void DoneHandler(object sender, DoneMeshData dmd);

    public abstract class MeshGenerator
    {
        public event DoneHandler Done;

        //private Map map;
        protected List<Chunk> chunks;
        protected Dictionary<int3, bool> emptyBorderBlocksMap;
        protected int workloadIndex = -1;

        protected int chunkSize;
        protected int3 mapChunkSize;
        protected int3 mapBlockSize;

        public MeshGenerator(MapInfo map, ref List<Chunk> chunks, ref Dictionary<int3, bool> emptyBorderBlocksMap, int workloadIndex)
        {
            this.chunks = chunks;
            this.emptyBorderBlocksMap = emptyBorderBlocksMap;
            this.workloadIndex = workloadIndex;

            chunkSize = (int)map.chunkSize;
            mapChunkSize = map.size;
            mapBlockSize = map.size * chunkSize;
        }

        public abstract void Generate();
        protected virtual void OnDone(List<Vector3[]> verticesList, List<List<int[]>> subMeshTrianglesList, List<Vector2[]> uvsList)
        {
            DoneMeshData dmd = new DoneMeshData();

            dmd.workloadIndex = this.workloadIndex;
            dmd.verticesList = verticesList;
            dmd.subMeshTrianglesList = subMeshTrianglesList;
            dmd.uvsList = uvsList;
            dmd.positions = new List<int3>();
            foreach (Chunk cd in chunks)
            {
                dmd.positions.Add(cd.WorldPosition);
            }

            Done?.Invoke(this, dmd);
        }
    }

    #endregion
}

