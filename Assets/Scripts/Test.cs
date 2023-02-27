using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Mesh_TerrainSettings mesh_set;
    public MeshFilter filter;
    public MeshGenerator meshGen;
    public VoxelGenerator voxGen;
    public Vector3 offset;
    public float scale;

    public void Generate()
    {
        Voxel voxel = voxGen.GenerateVoxel(mesh_set, offset, scale);
        Mesh gen = meshGen.GenerateMesh(voxel, mesh_set);
        filter.sharedMesh = gen;
		transform.position = voxel.offset;
		transform.localScale = Vector3.one * voxel.scale;
	}
}
