using UnityEngine;
using System.Collections;
using UnityEditor;

// Inherit to child classes - true
[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor: Editor {

    public override void OnInspectorGUI() {

        base.OnInspectorGUI();

        UpdatableData data = (UpdatableData)target;

        if(GUILayout.Button("Update")){
            data.NotifyUpdatedValues();
            EditorUtility.SetDirty(target); // tell unity to update when settings changed
        }
    }

}
