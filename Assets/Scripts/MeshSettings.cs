using System.Collections;
using UnityEngine;

 [CreateAssetMenu()]
public class MeshSettings : UpdatableData {

    public const int numSupportedLOD = 5;
	public const int numSupportedChunkSizes = 9;
	public const int numSuppportedFlatShadedChunkSizes = 3;
	public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    public float meshScale = 2.5f; // scales x/y/z 
    public bool useFlatShading;

    [Range(0,numSupportedChunkSizes - 1)]
	public int chunkSizeIdx;
	[Range(0, numSuppportedFlatShadedChunkSizes -1)]
	public int flatShadedChunkSizeIdx;

	//239 (240) works for flatShading=0;
	//95 (96) works for flatShading=1;
	// highest resoulution (LOD=0) num vertices per line
    // +1 - includes 2 vtx for Normals calc (not incl in final mesh)
	public int numVtxPerLine {
		get {
            return supportedChunkSizes[(useFlatShading)?flatShadedChunkSizeIdx:chunkSizeIdx] + 1;
		}
	}
    public float meshWorldSize {
        get {
            // width (dashes) *--*--* vtx -1
            // minus normals calc -2 
            return (numVtxPerLine -3 ) * meshScale;
        }
    }


}
