using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour {

	const float viewerMvThresholdForChunkUpdt = 25f;
	const float sqrViewerMvThresholdForChunkUpdt = viewerMvThresholdForChunkUpdt * viewerMvThresholdForChunkUpdt;
	const float colliderGenDstThreshold = 5;
	public int colliderLODIdx;
	public LODInfo[] detailLevels;
	public static float maxViewDst;
	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 viewerPosOld;
	static MapGenerator mapGenerator;

	float meshWorldSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();

		maxViewDst = detailLevels[detailLevels.Length -1].visibleDstThreshold;	
		meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize );
		UpdateVisibleChunks();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
		
		if(viewerPosition != viewerPosOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh();
			}
		}

		if((viewerPosOld - viewerPosition).sqrMagnitude > sqrViewerMvThresholdForChunkUpdt) {
			viewerPosOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}
		
	void UpdateVisibleChunks() {

		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

		for (int i = visibleTerrainChunks.Count -1; i >= 0; i--) {
			visibleTerrainChunks[i].UpdateTerrainChunk();
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
		}
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if(!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIdx, transform, mapMaterial));
					}
				}
			}
		}
	}

	public class TerrainChunk {

		public Vector2 coord;
		GameObject meshObject;
		Vector2 sampleCentre;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;
		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		int colliderLODIdx;

		HeightMap mapData;
		bool mapDataReceived;
		int prevLODIdx = -1;

		bool hasSetCollider;

		public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIdx, Transform parent, Material material) {
			this.coord = coord;
			this.detailLevels = detailLevels;
			this.colliderLODIdx = colliderLODIdx;
			
			sampleCentre = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
			Vector3 position = coord * meshWorldSize;
			bounds = new Bounds(position,Vector2.one * meshWorldSize);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;

			meshObject.transform.position = new Vector3(position.x, 0, position.y);
			meshObject.transform.parent = parent;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for(int i=0; i<detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
				lodMeshes[i].updateCallback += UpdateTerrainChunk;
				if(i== colliderLODIdx) {
					lodMeshes[i].updateCallback += UpdateCollisionMesh;
				}

			}

			mapGenerator.RequestHeightMap(position, OnMapDataReceived);
		}

		void OnMapDataReceived(HeightMap mapData) {
			this.mapData = mapData;
			mapDataReceived = true;
			UpdateTerrainChunk();
		}

		void OnMeshDataReceived(MeshData meshData) {
			meshFilter.mesh = meshData.CreateMesh ();
		}


		public void UpdateTerrainChunk() {

			if(mapDataReceived) {
				float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
				bool wasVisible = IsVisible();
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if(visible) {
					int LODIdx = 0;
					for(int i=0; i<detailLevels.Length + 1; i++) {
						if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
							LODIdx = i+1;
						} else {
							break;
						}
					}
					if(LODIdx != prevLODIdx) {
						LODMesh lodMesh = lodMeshes[LODIdx];
						if(lodMesh.hasMesh) {
							prevLODIdx = LODIdx;
							meshFilter.mesh = lodMesh.mesh;
							
						} else if(!lodMesh.hasRequestedMesh) {
							lodMesh.RequestMesh(mapData);
						}
					}
				}
				if(wasVisible != visible) {
					if(visible) {
						visibleTerrainChunks.Add(this);
					} else {
						visibleTerrainChunks.Remove(this);
					}
					SetVisible (visible);
				}
			}
		}

		public void UpdateCollisionMesh() {
			if(!hasSetCollider) {
				float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

				if(sqrDstFromViewerToEdge < detailLevels[colliderLODIdx].sqrVisableDstThreshold) {
					if(!lodMeshes[colliderLODIdx].hasRequestedMesh) {
						lodMeshes[colliderLODIdx].RequestMesh(mapData);
					}
				}

				if(sqrDstFromViewerToEdge < colliderGenDstThreshold*colliderGenDstThreshold) {
					if(lodMeshes[colliderLODIdx].hasMesh) {
						meshCollider.sharedMesh = lodMeshes[colliderLODIdx].mesh;
						hasSetCollider = true;
					}
				}
			}
		}
		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
		

	}

	class LODMesh {
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		public event System.Action updateCallback;
		
		public LODMesh(int lod) {
			this.lod = lod;
		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}
		public void RequestMesh(HeightMap mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);

		}
	}

	[System.Serializable]
	public struct LODInfo {
		[Range(0,MeshSettings.numSupportedLOD-1)]
		public int lod;
		public float visibleDstThreshold;

		public float sqrVisableDstThreshold {
			get {
				return visibleDstThreshold * visibleDstThreshold;
			}
		}
	}
	
}