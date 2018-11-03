using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PTSPoint
{
    public Vector3 position;
    public float intensity;
    public Vector3 color;
    public PTSPoint(Vector3 position, float intensity, Vector3 color)
    {
        this.position = position;
        this.intensity = intensity;
        this.color = color;

    }
}
