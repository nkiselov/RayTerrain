using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public GameObject defaultChunk;
    public Vector3 center;
    public float scale;
    public int depth;
    private bool renderingMain;
    private Chunk[,,] subchunks = new Chunk[2,2,2];

    public void recalculate(Transform observer, Transform chunkContainer, LOD_TerrainSettings lod_set, Mesh_TerrainSettings mesh_set, MeshGenerator meshGen, VoxelGenerator voxGen)
    {
        if (depth == 0)
        {
            StartRenderMain(mesh_set, meshGen, voxGen);
            return;
        }
        float dist = Mathf.Sqrt(
            Mathf.Pow(Mathf.Max(0, Mathf.Abs(observer.position.x - center.x) - scale), 2) +
            Mathf.Pow(Mathf.Max(0, Mathf.Abs(observer.position.y - center.y) - scale), 2) +
            Mathf.Pow(Mathf.Max(0, Mathf.Abs(observer.position.z - center.z) - scale), 2));
        float resDist = lod_set.resolution(depth);
        if (dist >= resDist)
        {
            StartRenderMain(mesh_set,meshGen,voxGen);
        }
        else
        {
            StopRenderMain();
            for(int x=0; x<2; x++)
			{
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
						if (subchunks[x,y,z] == null)
						{
                            GameObject newChunk = Instantiate(defaultChunk, chunkContainer);
                            Vector3 newPos = center + (2f * (new Vector3(x, y, z)) - Vector3.one) / 4f * scale;
                            newChunk.transform.position = newPos;
                            subchunks[x,y,z] = newChunk.GetComponent<Chunk>();
                            subchunks[x, y, z].center = newPos;
                            subchunks[x, y, z].scale = scale / 2;
                            subchunks[x, y, z].depth = depth - 1;
                        }
                        subchunks[x,y,z].recalculate(observer, chunkContainer, lod_set, mesh_set, meshGen, voxGen);
                    }
                }
            }
        }
    }

    private void StopRenderMain()
    {
        if (!renderingMain) return;
        renderingMain = false;
        GetComponent<MeshFilter>().sharedMesh = null;
	}

    public void Destroy()
	{
        foreach (Chunk c in subchunks)
        {
            if (c != null)
            {
                c.Destroy();
            }
        }
        Destroy(gameObject);
    }

    private void StartRenderMain(Mesh_TerrainSettings mesh_set, MeshGenerator meshGen, VoxelGenerator voxGen)
	{
        if (renderingMain) return;
        renderingMain = true;
        foreach (Chunk c in subchunks)
        {
            if (c != null)
            {
                c.Destroy();
            }
        }
		Voxel voxel = voxGen.GenerateVoxel(mesh_set, center, scale);
		Mesh gen = meshGen.GenerateMesh(voxel, mesh_set);
		GetComponent<MeshFilter>().sharedMesh = gen;
		transform.position = center;
		transform.localScale = Vector3.one * scale;
	}

    public void OnDrawGizmos()
	{
        if (!renderingMain) return;
        Gizmos.color = Color.HSVToRGB(depth / 16f,1f,1f);
        Gizmos.DrawWireCube(center, Vector3.one * scale);
	}
}
