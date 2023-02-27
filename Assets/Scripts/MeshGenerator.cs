using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;

    public ComputeShader shader;

    public Mesh GenerateMesh(Voxel voxel, Mesh_TerrainSettings mesh_set)
    {
        int numPoints = mesh_set.numPointsPerAxis * mesh_set.numPointsPerAxis * mesh_set.numPointsPerAxis;
        int numVoxelsPerAxis = mesh_set.numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        int kernel = shader.FindKernel("March");

        ComputeBuffer triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        ComputeBuffer pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        pointsBuffer.SetData(voxel.points);
        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(kernel, "points", pointsBuffer);
        shader.SetBuffer(kernel, "triangles", triangleBuffer);
        shader.SetInt("numPointsPerAxis", mesh_set.numPointsPerAxis);
        shader.SetFloat("isoLevel", mesh_set.isoLevel);
        shader.Dispatch(kernel, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis); ;

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];
        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        triangleBuffer.Dispose();
        pointsBuffer.Dispose();
        triCountBuffer.Dispose();

        Vector3[] vertices = new Vector3[numTris * 3];
        int[] meshTriangles = new int[numTris * 3];
        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
}
