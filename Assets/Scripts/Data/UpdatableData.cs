using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

    public event System.Action OnValuesUpdated;

    public bool autoUpdate;

    protected virtual void OnValidate() {
        // on change to inspector value
        if(autoUpdate) {
            // Because shader compiles after heights are set - so it's not receiving values (all white terrain)
            UnityEditor.EditorApplication.update += NotifyUpdatedValues;
            
        }        
    }
    public void NotifyUpdatedValues() {
        // Unsubscribe so it's not called ever frame
        UnityEditor.EditorApplication.update  -= NotifyUpdatedValues;
        if(OnValuesUpdated != null) {
            OnValuesUpdated(); 
        }

    }
}
