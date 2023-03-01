using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public Transform observer;
    public Transform chunkContainer;
    public Chunk topChunk;
    public LOD_TerrainSettings lod_set;
    public Mesh_TerrainSettings mesh_set;
    public MeshGenerator meshGen;
    public VoxelGenerator voxGen;

    void Start()
	{
        Physics.autoSimulation = false;
    }

    public void Recalculate()
	{
        topChunk.depth = lod_set.resolutionWidth.Length;
        topChunk.scale = Mathf.Pow(2, lod_set.resolutionWidth.Length) * lod_set.unitSize;
        topChunk.recalculate(observer, chunkContainer, lod_set, mesh_set,meshGen,voxGen);
	}

    public void Update()
	{
        Recalculate();
    }
}
