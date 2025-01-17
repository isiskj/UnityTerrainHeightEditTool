using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


[ExecuteInEditMode]
public class HeightEditTool : MonoBehaviour
{
    [SerializeField]
    private RenderTexture sourceTerrainHeight;

    private Terrain terrainReference;
    private Vector2 terrainSize;

    private Material projectionMaterial;

    private RectInt sourceRect;
    private Vector2Int dest;
    [HideInInspector]
    public List<ProjectorSerialize> projectors = new List<ProjectorSerialize>();
    public Button myButton; // インスペクターからボタンを参照するためのフィールド 

    private bool isInitialize = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
#if UNITY_EDITOR
    void Initialize()
    {
        terrainReference = GetComponent<Terrain>();
        var terrainData = terrainReference.terrainData;
        terrainSize = new Vector2(terrainData.size.x, terrainData.size.z);

        sourceTerrainHeight = HeightMapCopy(terrainData.heightmapTexture);
        projectionMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TerrainTools/ProjectionMat.mat");

        sourceRect = new RectInt(0, 0, sourceTerrainHeight.width, sourceTerrainHeight.height);
        dest = new Vector2Int(0, 0);
        isInitialize = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(projectors == null)
        {
            isInitialize = false;
            return;
        }

        if (!isInitialize)
            Initialize();

        // プロジェクターを動かした場合TerrainをUpdateする
        foreach(var projector in projectors)
        {
            if(projector.lastPosition != projector.projectorObject.transform.position || projector.lastRotation != projector.projectorObject.transform.rotation.eulerAngles.y)
            {
                projector.lastPosition = projector.projectorObject.transform.position;
                projector.lastRotation = projector.projectorObject.transform.rotation.eulerAngles.y;
                UpdateTerrain();
                
                return;
            }
        }
    }

    public void PropertyChange()
    {
        UpdateTerrain();
    }

    private void UpdateTerrain()
    {
        var tmpRt = new RenderTexture(sourceTerrainHeight.width, sourceTerrainHeight.height, 0, RenderTextureFormat.R16);
        Graphics.Blit(sourceTerrainHeight, tmpRt);
        foreach (ProjectorSerialize pj in projectors)
        {
            RaycastHit hit;
            if (Physics.Raycast(pj.projectorObject.transform.position, Vector3.down, out hit))
            {
                var hitterrain = hit.collider.gameObject.GetComponent<Terrain>();
                if (hitterrain == terrainReference)
                {
                    Vector2 uv = GetTerrainUV(terrainReference, hit.point);
                    SetProjectionMaterialProperty(uv, pj.projectionTexture, pj.scaleXY, pj.heightStrength, pj.heightOffset, pj.projectorObject.transform.rotation.eulerAngles.y, pj.shaderKeyword);
                    BlitRenderTexture(tmpRt, projectionMaterial);
                }
            }
        }
        UpdateHightMap(tmpRt);
    }

    private void SetProjectionMaterialProperty(Vector2 brushUV, Texture2D brushTexture, Vector2 brushScale, float heightStrength, float heightOffset, float rotation, ProjectionShaderKeyword keyword)
    {
        var calculateScale = new Vector2(terrainSize.x/ brushScale.x, terrainSize.y/ brushScale.y);
        projectionMaterial.SetVector("_BrushUV", new Vector4(brushUV.x, brushUV.y, calculateScale.x, calculateScale.y));
        projectionMaterial.SetTexture("_BrushTex", brushTexture);
        projectionMaterial.SetFloat("_HeightStrength", heightStrength);
        projectionMaterial.SetFloat("_HeightOffset", heightOffset);
        projectionMaterial.SetFloat("_Rotation", rotation);
        SetShaderKeyword(keyword);
    }

    private void SetShaderKeyword(ProjectionShaderKeyword keyword)
    {
        projectionMaterial.DisableKeyword("paint_max");
        projectionMaterial.DisableKeyword("paint_overlay");
        projectionMaterial.DisableKeyword("paint_min");
        projectionMaterial.DisableKeyword("paint_blend");
        string key = null;
        switch (keyword)
        {
            case ProjectionShaderKeyword.paint_max:
                key = "paint_max";
                break;
            case ProjectionShaderKeyword.paint_overlay:
                key = "paint_overlay";
                break;
            case ProjectionShaderKeyword.paint_min:
                key = "paint_min";
                break;
            case ProjectionShaderKeyword.paint_blend:
                key = "paint_blend";
                break;
        }
        projectionMaterial.EnableKeyword(key);
    }
    private void BlitRenderTexture(RenderTexture renderTexture, Material material)
    {
        var renderTextureBuffer = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, 0, RenderTextureFormat.R16);

        Graphics.Blit(renderTexture, renderTextureBuffer, material);
        Graphics.Blit(renderTextureBuffer, renderTexture);
    }
    private RenderTexture HeightMapCopy(RenderTexture heightMap)
    {
        var tmpRenderTexture = new RenderTexture(heightMap.width, heightMap.height, 0, RenderTextureFormat.R16);
        tmpRenderTexture.name = "HeightMapSource";
        Graphics.Blit(heightMap, tmpRenderTexture);
        return tmpRenderTexture;
    }

    Vector2 GetTerrainUV(Terrain terrain, Vector3 worldPos)
    {
        Vector3 terrainPos = terrain.transform.position;

        // ハイトマップの相対座標を計算
        float relativeX = (worldPos.x - terrainPos.x) / terrainSize.x;
        float relativeZ = (worldPos.z - terrainPos.z) / terrainSize.y;

        // UV座標を計算
        Vector2 uv = new Vector2(relativeX, relativeZ);
        return uv;
    }

    private void UpdateHightMap(RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;

        terrainReference.terrainData.CopyActiveRenderTextureToHeightmap(sourceRect, dest, TerrainHeightmapSyncControl.HeightAndLod);
        terrainReference.Flush();        
    }

    public void Bake()
    {
        sourceTerrainHeight = HeightMapCopy(terrainReference.terrainData.heightmapTexture);
        foreach(var projector in projectors)
        {
            DestroyImmediate(projector.projectorObject);
        }
        projectors.Clear();
    }


    private void OnDrawGizmos()
    {
        if (projectors == null)
        {
            return;
        }
        var length = 1000;
        foreach (ProjectorSerialize pj in projectors)
        {
            var size = new Vector3(pj.scaleXY.x, 1000, pj.scaleXY.y);
            var trans = pj.projectorObject.transform;
            Gizmos.color = pj.gizmoColor;
            Matrix4x4 originalMatrix = Gizmos.matrix; // 新しい行列を設定：位置、回転（Y軸）、スケールを含む
            var currentRotation = trans.rotation;
            Quaternion rotation = Quaternion.Euler(0, currentRotation.eulerAngles.y, 0);
            Gizmos.matrix = Matrix4x4.TRS(trans.position, rotation * Quaternion.Euler(0, trans.rotation.y, 0), trans.lossyScale);
            // ボックスの中心をオブジェクトの下方向に移動
            Vector3 boxCenter = new Vector3(0, -length / 2, 0);
            Gizmos.DrawWireCube(boxCenter, size);
            // 元のギズモ行列を復元
            Gizmos.matrix = originalMatrix;
        }        
    }
#endif
}