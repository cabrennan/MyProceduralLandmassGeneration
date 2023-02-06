using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {
    public float uniformScale = 2.5f; // scales x/y/z 
    public bool useFlatShading;
    public bool useFalloff;

	public float meshHeightMultiplier;  // scales on Y
	public AnimationCurve meshHeightCurve;
}
