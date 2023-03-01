using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Blit : MonoBehaviour
{
    public ComputeShader shader;
    private RenderTexture tex;
    public Mesh_TerrainSettings mesh_set;
    public int treeSizeLim;
    public int meshSizeLim;
    const int lod = 14;
    private ComputeBuffer debug;
    private ComputeBuffer treeMem;
    private ComputeBuffer meshMem;
    private ComputeBuffer treeMemFree;
    private ComputeBuffer meshMemFree;
    private ComputeBuffer count;
    private int[] kernelHandle = new int[4];
    private float time1 = 0;
    private float time2 = 0;
    private int frames = 0;

    private int[] genData(int start, int end, int skip)
    {
        int[] ret = new int[(end - start) / skip + 1];
        for (int i = 0; i < ret.Length; i++)
        {
            ret[ret.Length - 1 - i] = start + i * skip;
        }
        return ret;
    }

    private void Start()
    {
        kernelHandle[0] = shader.FindKernel("TreeUpdate");
        kernelHandle[1] = shader.FindKernel("MeshUpdate");
        kernelHandle[2] = shader.FindKernel("TreeBind");
        kernelHandle[3] = shader.FindKernel("Render");

        treeMem = new ComputeBuffer(treeSizeLim, sizeof(int) * 11 + sizeof(float) * 4);
        meshMem = new ComputeBuffer(meshSizeLim, sizeof(float) * 9 * 5 * 2 + sizeof(int));
        treeMemFree = new ComputeBuffer(treeSizeLim, sizeof(int), ComputeBufferType.Append);
        meshMemFree = new ComputeBuffer(meshSizeLim, sizeof(int), ComputeBufferType.Append);
        count = new ComputeBuffer(1, sizeof(int));
        debug = new ComputeBuffer(4, sizeof(int));

        treeMemFree.SetData(genData(1, treeSizeLim - 1, 8));
        meshMemFree.SetData(genData(0, meshSizeLim - 1, 1));
        treeMemFree.SetCounterValue((uint)(treeSizeLim - 2) / 8 + 1);
        meshMemFree.SetCounterValue((uint)meshSizeLim);
        TreeNode initNode = new TreeNode(1, lod, 0, 0, 0, 20000);
        treeMem.SetData(new TreeNode[] { initNode }, 0, 0, 1);

        shader.SetBuffer(kernelHandle[0], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[0], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[1], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[1], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[2], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[2], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[3], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[3], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[0], "treememFree", treeMemFree);
        shader.SetBuffer(kernelHandle[1], "meshmemFree", meshMemFree);

        shader.SetBuffer(kernelHandle[0], "debug", debug);
        shader.SetBuffer(kernelHandle[1], "debug", debug);
        shader.SetBuffer(kernelHandle[2], "debug", debug);
        shader.SetBuffer(kernelHandle[3], "debug", debug);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (tex == null || tex.height != Screen.height || tex.width != Screen.width)
        {
            if (tex != null)
            {
                tex.Release();
            }
            tex = new RenderTexture(Screen.width, Screen.height, 24);
            tex.enableRandomWrite = true;
            tex.Create();
        }

        time1 -= Time.realtimeSinceStartup;

        Camera cam = GetComponent<Camera>();

        ComputeBuffer buf = VoxelGenerator.SetDensityParams(shader, mesh_set, kernelHandle[1]);
        shader.SetFloat("isoLevel", mesh_set.isoLevel);
        shader.SetMatrix("camFrustum", GetFrustumCorners(cam));
        shader.SetMatrix("camToWorld", cam.cameraToWorldMatrix);
        shader.SetVector("camPos", cam.transform.position);
        shader.Dispatch(kernelHandle[0], treeSizeLim / 256, 1, 1);
        shader.Dispatch(kernelHandle[1], treeSizeLim / 256, 1, 1);
        buf.Dispose();

        int[] countArr = new int[1];
        ComputeBuffer.CopyCount(treeMemFree, count, 0);
        count.GetData(countArr);
        //print("tree used: "+(treeSizeLim-8*countArr[0]));
        ComputeBuffer.CopyCount(meshMemFree, count, 0);
        count.GetData(countArr);
        //print("mesh used: " + (meshSizeLim - countArr[0]));
        for (int lvl = lod; lvl > 0; lvl--)
        {
            shader.SetInt("lvlProcess", lvl);
            shader.Dispatch(kernelHandle[2], treeSizeLim / 256, 1, 1);
        }

        time1 += Time.realtimeSinceStartup;
        time2 += Time.deltaTime;
        frames++;
        print("Update " + time1 / frames);
        print("Render " + time2 / frames);

        shader.SetTexture(kernelHandle[3], "Result", tex);
        shader.SetInts("resSize", new int[] { tex.width, tex.height });
        shader.Dispatch(kernelHandle[3], tex.width / 16, tex.height / 16, 1);

        int[] debugArr = new int[4];
        debug.GetData(debugArr);
        Debug.Log(string.Join(", ", debugArr));

        Graphics.Blit(tex, null as RenderTexture);
    }

    private void OnApplicationQuit()
    {
        print("quit");
        treeMem.Dispose();
        meshMem.Dispose();
        treeMemFree.Dispose();
        meshMemFree.Dispose();
        count.Dispose();
    }

    private Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }

    struct TreeNode
    {
        int state;
        int level;
        int parent;
        float centerX;
        float centerY;
        float centerZ;
        float size;
        int meshInd;
        int chldst;
        int adj0, adj1, adj2, adj3, adj4, adj5;

        public TreeNode(int state, int level, float centerX, float centerY, float centerZ, float size)
        {
            this.state = state;
            this.level = level;
            this.parent = -1;
            this.centerX = centerX;
            this.centerY = centerY;
            this.centerZ = centerZ;
            this.size = size;
            this.meshInd = -1;
            this.chldst = 0;
            this.adj0 = -1; this.adj1 = -1; this.adj2 = -1; this.adj3 = -1; this.adj4 = -1; this.adj5 = -1;
        }
    }
}