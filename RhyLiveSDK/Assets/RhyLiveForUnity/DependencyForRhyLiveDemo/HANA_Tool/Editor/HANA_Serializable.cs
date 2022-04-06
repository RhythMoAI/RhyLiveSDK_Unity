using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*!
[2020] [kuniyan]
Please read included license

 */

[Serializable]
public class BSData
{
    public string shapeKeyName;
    public float blendShapeWeight;
    public int vertexCount;
    public int[] elements;
    public Vector3[] v3_vertices;
}

[Serializable]
public class ListSerialize<BSData>
{
    public List<BSData> blendShapeDatas;

    public ListSerialize(List<BSData> blendShapeDatas)
    {
        this.blendShapeDatas = blendShapeDatas;
    }

    public List<BSData> ToList()
    {
        return blendShapeDatas;
    }
}

