using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Damin.VFX.TireSmoke;

[CustomEditor(typeof(TireSmokeWheelController))]
public sealed class TireSmokeWheelControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("이 스크립트는 연기 부모(02_TireSmoke)에 붙입니다. 차량에 붙이지 않습니다.\nVehicle Root에 사용할 차량, 각 Wheel에 해당 차량의 타이어를 넣으세요.", MessageType.Info);
        DrawDefaultInspector();
        var so = serializedObject;
        if (!so.FindProperty("vehicleRoot").objectReferenceValue)
            EditorGUILayout.HelpBox("Vehicle Root가 비어 있습니다. 차량과 바퀴가 연결되기 전에는 기존 테스트 연기를 그대로 보여줍니다.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
            if (GUILayout.Button("연기 다시 연결 / Rebuild")) ((TireSmokeWheelController)target).RebuildSmoke();
        EditorGUILayout.HelpBox("연기 오브젝트를 차량별로 복제하고 각 차량을 연결하면 됩니다. 연결 후 Play에서 원래 위치의 연기만 숨기고 바퀴별 연기를 생성합니다.\n기존 Local-space 연출 유지: 속도/슬립 판정 및 주행 후 월드 공간에 남는 연기는 아직 미구현입니다.", MessageType.None);
    }

    public static TireSmokeWheelController AttachToSmoke(GameObject smoke)
    {
        if (!smoke || EditorUtility.IsPersistent(smoke) || !smoke.scene.IsValid() || smoke.GetComponentsInChildren<VisualEffect>(true).Length == 0)
            throw new System.InvalidOperationException("씬의 연기 부모 오브젝트를 선택해야 합니다.");
        var component = smoke.GetComponent<TireSmokeWheelController>();
        if (!component) component = Undo.AddComponent<TireSmokeWheelController>(smoke);
        var so = new SerializedObject(component);
        if (!so.FindProperty("smokePrefab").objectReferenceValue)
            so.FindProperty("smokePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03DAMIN/VFX/VFX_TireSmoke_Vehicle/Prefabs/PF_TireSmoke_Final_Vehicle.prefab");
        so.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        return component;
    }

    [MenuItem("NITRO ZERO/VFX/Attach Wheel Controller to Selected Smoke")]
    private static void AddToSelectedSmoke()
    {
        try { var component = AttachToSmoke(Selection.activeGameObject); Selection.activeGameObject = component.gameObject; }
        catch (System.Exception e) { EditorUtility.DisplayDialog("연기 부모 선택", e.Message, "확인"); }
    }
}
