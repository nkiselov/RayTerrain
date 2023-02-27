using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class LOD_TerrainSettings : ScriptableObject
{
    public float[] resolutionWidth;
    public float unitSize;

    public float resolution(int r)
	{
        float res = 0;
        for(int i=0; i<r; i++)
		{
            res += Mathf.Pow(2, i) * resolutionWidth[i];
		}
        return res*unitSize;
	}
}
