using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDst = 450f;
    public Transform viewer;

    public static Vector2 viewerPos;
    int chunkSize;
    int chunkVisibleInDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInDist = Mathf.RoundToInt(maxViewDst/chunkVisibleInDist);

    }   

    void Update() {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks() {

        for(int i=0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currChunkCoordX = Mathf.RoundToInt(viewerPos.x/chunkSize);
        int currChunkCoordY = Mathf.RoundToInt(viewerPos.y/chunkSize);

        for (int yOffset = -chunkVisibleInDist; yOffset <= chunkVisibleInDist; yOffset++ ) {
            for(int xOffset = -chunkVisibleInDist; xOffset <= chunkVisibleInDist; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currChunkCoordX + xOffset, currChunkCoordY + yOffset);

                if(terrainChunkDict.ContainsKey(viewChunkCoord)) {
                    terrainChunkDict[viewChunkCoord].UpdateTerrainChunk();
                    if(terrainChunkDict[viewChunkCoord].IsVisible()) {
                            terrainChunksVisibleLastUpdate.Add(terrainChunkDict[viewChunkCoord]);
                    }
                } else {
                    terrainChunkDict.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkSize, transform));
                }
            }   
        }
    }

    public class TerrainChunk {
        GameObject meshObj;
        Vector2 pos;
        Bounds bounds;
        public TerrainChunk(Vector2 coord, int size, Transform parent) {
            pos = coord * size;
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);
            bounds = new Bounds(pos, Vector2.one * size);

            meshObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObj.transform.position = posV3;
            meshObj.transform.localScale = Vector3.one * size/10f;
            meshObj.transform.parent = parent;
            SetVisible(false);

        }

        public void UpdateTerrainChunk() {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromNearestEdge <= maxViewDst;
            SetVisible(visible);

        }

        public void SetVisible(bool visible) {
            meshObj.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObj.activeSelf;
        }
    }
}
