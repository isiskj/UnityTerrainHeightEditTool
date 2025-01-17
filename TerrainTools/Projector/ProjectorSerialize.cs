using System;
using UnityEngine;

public enum ProjectionShaderKeyword
{
    paint_max,
    paint_overlay,
    paint_min,
    paint_blend,
}

[System.Serializable]
public class ProjectorSerialize
{
    public GameObject projectorObject;
    public Vector3 lastPosition;
    public float lastRotation;
    public Vector2 scaleXY;
    public Texture2D projectionTexture;
    public float heightStrength;
    public float heightOffset;
    public ProjectionShaderKeyword shaderKeyword;

    // Debug
    public Color gizmoColor;
}
