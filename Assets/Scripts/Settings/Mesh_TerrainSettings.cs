using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Mesh_TerrainSettings : ScriptableObject
{
    [Header("Terrain Settings")]
    [Range(2, 24)]
    public int numPointsPerAxis = 16;
    public float isoLevel;

    [Header("Noise Settings")]
    public int seed;
    public float scale;
    public float baseAmplitude;
    public float baseFrequency;
    public Vector3 offset;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public float weightMultiplier;
    public float hardFloor;
    public float hardFloorWeight;
}
