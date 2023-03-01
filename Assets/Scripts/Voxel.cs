using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public float scale;
    public Vector3 offset;
    public float[] points;

    public Voxel(float scale, Vector3 offset, float[] points)
	{
        this.scale = scale;
        this.offset = offset;
        this.points = points;
	}
}
