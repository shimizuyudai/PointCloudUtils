using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PointCloudPoint
{
    public Vector3 position;
    public Color color;

    public PointCloudPoint(Vector3 position, Color color)
    {
        this.position = position;
        this.color = color;
    }
}