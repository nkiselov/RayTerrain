using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blit : MonoBehaviour
{
    public ComputeShader shader;
    private RenderTexture tex;
    public Mesh_TerrainSettings mesh_set;
    public int treeSizeLim;
    public int meshSizeLim;
    const int lod = 0;
    private ComputeBuffer treeMem;
    private ComputeBuffer meshMem;
    private ComputeBuffer treeMemFree;
    private ComputeBuffer meshMemFree;
    private ComputeBuffer count;
    private int[] kernelHandle = new int[3];

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
        kernelHandle[1] = shader.FindKernel("TreeBind");
        kernelHandle[2] = shader.FindKernel("Render");

        treeMem = new ComputeBuffer(treeSizeLim, sizeof(int) * 10 + sizeof(float) * 4);
        meshMem = new ComputeBuffer(meshSizeLim, sizeof(float) * 9 * 5 + sizeof(int));
        treeMemFree = new ComputeBuffer(treeSizeLim, sizeof(int), ComputeBufferType.Append);
        meshMemFree = new ComputeBuffer(meshSizeLim, sizeof(int),ComputeBufferType.Append);
        count = new ComputeBuffer(1, sizeof(int));
        treeMemFree.SetData(genData(1,treeSizeLim-1,8));
        meshMemFree.SetData(genData(0,meshSizeLim - 1, 1));
        treeMemFree.SetCounterValue((uint)(treeSizeLim - 2)/8+1);
        TreeNode initNode = new TreeNode(1,lod,0,0,0,100);
        treeMem.SetData(new TreeNode[] { initNode } ,0,0,1);

        shader.SetBuffer(kernelHandle[0], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[0], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[1], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[1], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[2], "treemem", treeMem);
        shader.SetBuffer(kernelHandle[2], "meshmem", meshMem);
        shader.SetBuffer(kernelHandle[0], "treememFree", treeMemFree);
        shader.SetBuffer(kernelHandle[0], "meshmemFree", meshMemFree);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (tex == null || tex.height != Screen.height || tex.width != Screen.width)
        {
            if(tex != null)
			{
                tex.Release();
            }
            tex = new RenderTexture(Screen.width, Screen.height, 24);
            tex.enableRandomWrite = true;
            tex.Create();
        }

        Camera cam = GetComponent<Camera>();

        ComputeBuffer buf = VoxelGenerator.SetDensityParams(shader, mesh_set, kernelHandle[0]);
        shader.SetFloat("isoLevel", mesh_set.isoLevel);
        shader.SetMatrix("camFrustum", GetFrustumCorners(cam));
        shader.SetMatrix("camToWorld", cam.cameraToWorldMatrix);
        shader.SetVector("camPos", cam.transform.position);
        shader.Dispatch(kernelHandle[0], treeSizeLim / 256, 1, 1);
        buf.Dispose();
        ComputeBuffer.CopyCount(treeMemFree, count, 0);
        int[] countArr = new int[1];
        count.GetData(countArr);
        print(treeSizeLim-8*countArr[0]);
        for(int lvl = lod; lvl>0; lvl--)
        {
            shader.SetInt("lvlProcess", lvl);
            shader.Dispatch(kernelHandle[1], treeSizeLim / 256, 1, 1);
        }

        shader.SetTexture(kernelHandle[2], "Result", tex);
        shader.SetInts("resSize", new int[] { tex.width, tex.height });
        shader.Dispatch(kernelHandle[2], tex.width / 16, tex.height / 16, 1);
        Graphics.Blit(tex, null as RenderTexture);
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
