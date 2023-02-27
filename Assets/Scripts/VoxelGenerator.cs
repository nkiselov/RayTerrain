using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;

    public ComputeShader shader;

    public static ComputeBuffer SetDensityParams(ComputeShader shader, Mesh_TerrainSettings mesh_set, int kernelID)
    {
        System.Random prng = new System.Random(mesh_set.seed);
        Vector3[] octaveOffsets = new Vector3[mesh_set.octaves];
        for (int i = 0; i < mesh_set.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + mesh_set.offset.x;
            float offsetY = prng.Next(-100000, 100000) + mesh_set.offset.y;
            float offsetZ = prng.Next(-100000, 100000) + mesh_set.offset.z;
            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
        }

        ComputeBuffer octaveOffsetsBuffer = new ComputeBuffer(octaveOffsets.Length, sizeof(float) * 3);

        octaveOffsetsBuffer.SetData(octaveOffsets);

        shader.SetBuffer(kernelID, "octaveOffsets", octaveOffsetsBuffer);
        shader.SetFloat("baseAmplitude", mesh_set.baseAmplitude);
        shader.SetFloat("baseFrequency", mesh_set.baseFrequency);
        shader.SetInt("octaves", mesh_set.octaves);
        shader.SetFloat("persistance", mesh_set.persistance);
        shader.SetFloat("lacunarity", mesh_set.lacunarity);
        shader.SetFloat("weightMultiplier", mesh_set.weightMultiplier);
        shader.SetFloat("hardFloor", mesh_set.hardFloor);
        shader.SetFloat("hardFloorWeight", mesh_set.hardFloorWeight);
        return octaveOffsetsBuffer;
    }

    public Voxel GenerateVoxel(Mesh_TerrainSettings mesh_set, Vector3 offset, float scale)
    {
        int numPointsPerAxis = mesh_set.numPointsPerAxis;
        float[] voxel = new float[numPointsPerAxis * numPointsPerAxis * numPointsPerAxis];

        int numVoxelsPerAxis = mesh_set.numPointsPerAxis - 1;
        int numPoints = mesh_set.numPointsPerAxis * mesh_set.numPointsPerAxis * mesh_set.numPointsPerAxis;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        int kernel = shader.FindKernel("Voxel");

        ComputeBuffer voxelBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);

        shader.SetInt("numPointsPerAxis", numPointsPerAxis);
        shader.SetBuffer(kernel, "points", voxelBuffer);
        shader.SetFloat("scale", scale);
        shader.SetVector("offset", offset);
        ComputeBuffer octaveOffsetsBuffer = SetDensityParams(shader, mesh_set, kernel);

        shader.Dispatch(kernel, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        voxelBuffer.GetData(voxel);
        octaveOffsetsBuffer.Dispose();
        voxelBuffer.Dispose();
        return new Voxel(scale, offset, voxel);
    }
}
