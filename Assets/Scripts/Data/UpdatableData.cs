using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

    public event System.Action OnValuesUpdated;

    public bool autoUpdate;

    protected virtual void OnValidate() {
        // on change to inspector value
        if(autoUpdate) {
            NotifyUpdatedValues();
        }        
    }
    public void NotifyUpdatedValues() {
        if(OnValuesUpdated != null) {
            OnValuesUpdated(); 
        }

    }
}
