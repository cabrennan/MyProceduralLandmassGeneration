using UnityEngine;
using System.Collections;

public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
		AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail ==0)?1:levelOfDetail * 2;
		int bordedSize = heightMap.GetLength (0);
		int meshSize = bordedSize - 2 * meshSimplificationIncrement;

		int meshSizeUnsimp = bordedSize - 2;
		float topLeftX = (meshSizeUnsimp - 1) / -2f;
		float topLeftZ = (meshSizeUnsimp - 1) / 2f;

		
		int vtxPerLine = (meshSize - 1)/meshSimplificationIncrement + 1;	

		MeshData meshData = new MeshData (vtxPerLine);

		int[,] vtxIdxMap = new int[bordedSize,bordedSize];
		int meshVtxIdx = 0;
		int borderVtxIdx = -1;

		for (int y = 0; y < bordedSize; y+=meshSimplificationIncrement) {
			for (int x = 0; x < bordedSize; x+=meshSimplificationIncrement) {
				bool isBorderVtx = y == 0 || y == bordedSize - 1 || x == 0 || x == bordedSize - 1;
				if(isBorderVtx) {
					vtxIdxMap[x,y] = borderVtxIdx;
					borderVtxIdx--;
				} else {
					vtxIdxMap[x,y] = meshVtxIdx;
					meshVtxIdx++;
				}
			}
		}

		for (int y = 0; y < bordedSize; y+=meshSimplificationIncrement) {
			for (int x = 0; x < bordedSize; x+=meshSimplificationIncrement) {

				int vtxIdx = vtxIdxMap[x,y];
				Vector2 pct = new Vector2 ((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
				float height = heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier;
				Vector3 vtxPos = new Vector3 (topLeftX + pct.x * meshSizeUnsimp, height, topLeftZ - pct.y * meshSizeUnsimp);
				
				meshData.AddVtx(vtxPos, pct, vtxIdx);

				if (x < bordedSize - 1 && y < bordedSize - 1) {
					int a = vtxIdxMap[x,y];
					int b = vtxIdxMap[x + meshSimplificationIncrement, y];
					int c = vtxIdxMap[x, y + meshSimplificationIncrement];
					int d = vtxIdxMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

					meshData.AddTriangle (a, d, c);
					meshData.AddTriangle (d, a, b);
				}

			}
		}

		return meshData;

	}
}

public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] borderVtx;
	int[] borderTris;

	int triIdx;
	int borderTriIdx;

	public MeshData(int vtxPerLine) {
		vertices = new Vector3[vtxPerLine * vtxPerLine];
		uvs = new Vector2[vtxPerLine * vtxPerLine];
		triangles = new int[(vtxPerLine-1)*(vtxPerLine-1)*6];
		borderVtx = new Vector3[vtxPerLine * 4 + 4];
		borderTris = new int[6 * 4 * vtxPerLine];
	}

	public void AddVtx(Vector3 vtxPos, Vector2 uv, int vtxIdx) {
		if(vtxIdx<0) {
			borderVtx[-vtxIdx-1] = vtxPos;
		} else {
			vertices[vtxIdx] = vtxPos;
			uvs[vtxIdx] = uv;
		}
	}

	public void AddTriangle(int a, int b, int c) {

		if(a<0 || b<0 || c<0) {
			borderTris [borderTriIdx] = a;
			borderTris [borderTriIdx + 1] = b;
			borderTris [borderTriIdx + 2] = c;
			borderTriIdx += 3;
		} else {
			triangles [triIdx] = a;
			triangles [triIdx + 1] = b;
			triangles [triIdx + 2] = c;
			triIdx += 3;
		}
	}

	Vector3[] CalculateNormals() {

		// -1   -2   -3   -4   -5   
		// -6    0    1    2   -7
		// -8    3    4    5   -9
		// -10   6    7    8   -11
		// -12  -13  -14  -15  -16



		Vector3[] vtxNormals = new Vector3[vertices.Length];
		int triCount = triangles.Length/3;
		for(int i=0; i<triCount; i++) {
			int normalTriIdx = i * 3;
			int vtxIdxA = triangles[normalTriIdx];
			int vtxIdxB = triangles[normalTriIdx + 1];
			int vtxIdxC = triangles[normalTriIdx + 2];

			Vector3 triNormal = SurfNormalIdx(vtxIdxA, vtxIdxB, vtxIdxC);
			vtxNormals[vtxIdxA] += triNormal;
			vtxNormals[vtxIdxB] += triNormal;
			vtxNormals[vtxIdxC] += triNormal;
		}

		int borderTriCount = borderTris.Length/3;
		for(int i=0; i<borderTriCount; i++) {
			int normalTriIdx = i * 3;
			int vtxIdxA = borderTris[normalTriIdx];
			int vtxIdxB = borderTris[normalTriIdx + 1];
			int vtxIdxC = borderTris[normalTriIdx + 2];

			Vector3 triNormal = SurfNormalIdx(vtxIdxA, vtxIdxB, vtxIdxC);
			if(vtxIdxA >=0 ) {
				vtxNormals[vtxIdxA] += triNormal;
			}
			if(vtxIdxB >= 0) {
				vtxNormals[vtxIdxB] += triNormal;
			}
			if(vtxIdxC >= 0) {
				vtxNormals[vtxIdxC] += triNormal;
			}
		}

		for (int i=0; i<vtxNormals.Length; i++) {
			vtxNormals[i].Normalize();
		}

		return vtxNormals;
	}

	Vector3 SurfNormalIdx(int idxA, int idxB, int idxC) {
			Vector3 pointA = (idxA < 0)?borderVtx[-idxA - 1]:vertices[idxA];
			Vector3 pointB = (idxB < 0)?borderVtx[-idxB - 1]:vertices[idxB];
			Vector3 pointC = (idxC < 0)?borderVtx[-idxC - 1]:vertices[idxC];

			//Cross Product
			Vector3 sideAB = pointB - pointA;
			Vector3 sideAC = pointC - pointA;
			return Vector3.Cross(sideAB, sideAC).normalized;

	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		//mesh.normals = CalculateNormals();
		mesh.RecalculateNormals();
		return mesh;
	}

}