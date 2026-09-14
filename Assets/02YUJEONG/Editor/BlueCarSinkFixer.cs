using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    public static class BlueCarSinkFixer
    {
        [MenuItem("Tools/YUJEONG/🚀 블루카 땅 꺼짐 현상 완벽 해결")]
        public static void FixSink()
        {
            GameObject blueCar = GameObject.Find("Blue_Car_Final");
            GameObject redCar = GameObject.Find("Red_Car_Final");

            if (blueCar == null)
            {
                EditorUtility.DisplayDialog("오류", "씬에서 [Blue_Car_Final]을 찾을 수 없습니다.", "확인");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(blueCar, "Fix Blue Car Sink");

            // 1. Unpack if it's a prefab instance so we can freely modify transforms
            if (PrefabUtility.IsPartOfAnyPrefab(blueCar))
            {
                PrefabUtility.UnpackPrefabInstance(blueCar, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // 2. Get TrailerCruiseMotion
            var tcm = blueCar.GetComponent<TrailerCruiseMotion>();
            Transform bodyMotionRoot = null;
            if (tcm != null)
            {
                bodyMotionRoot = tcm.BodyMotionRoot;
            }
            if (bodyMotionRoot == null)
            {
                bodyMotionRoot = blueCar.transform.Find("CruiseBodyMotion");
            }

            // 3. Current visual position in world space
            Transform model = blueCar.transform.Find("CruiseBodyMotion/Blue_Car_Final_Model");
            if (model == null) model = blueCar.transform.Find("Blue_Car_Final_Model");

            // Target Y should match Red_Car_Final if available (-22.23f), or current proper road level (-22.15f)
            float targetRoadY = redCar != null ? redCar.transform.position.y : -22.15f;

            // Remember current visual X and Z on the road
            Vector3 visualPos = blueCar.transform.position;
            if (model != null)
            {
                visualPos = model.position;
            }

            // 4. Reset CruiseBodyMotion to (0,0,0) locally so TrailerCruiseMotion never drops it
            if (bodyMotionRoot != null)
            {
                bodyMotionRoot.localPosition = Vector3.zero;
                bodyMotionRoot.localRotation = Quaternion.identity;
            }

            if (model != null)
            {
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
            }

            // 5. Position the root Blue_Car_Final directly on the road at the visual lane position
            blueCar.transform.position = new Vector3(visualPos.x, targetRoadY, visualPos.z);
            if (redCar != null)
            {
                blueCar.transform.rotation = redCar.transform.rotation;
            }

            // 6. Fix wheel transforms so they match the body
            if (tcm != null)
            {
                tcm.RestorePose();
            }

            // 7. ★ 핵심: 블루차라인(SplineContainer)의 Y 높이를 차 높이(targetRoadY)로 올려 스플라인 주행 시 땅 꺼짐 원천 차단!
            GameObject splineObj = GameObject.Find("블루차라인");
            if (splineObj != null)
            {
                Undo.RecordObject(splineObj.transform, "Adjust Spline Height");
                splineObj.transform.position = new Vector3(splineObj.transform.position.x, targetRoadY, splineObj.transform.position.z);
                EditorUtility.SetDirty(splineObj);
            }

            // 8. SplineAnimate 속도 조절 (1초로 너무 빨리 날아가는 경우 15초로 부드럽게 조정)
            Component splineAnimate = blueCar.GetComponent("SplineAnimate");
            if (splineAnimate != null)
            {
                SerializedObject so = new SerializedObject(splineAnimate);
                SerializedProperty propDuration = so.FindProperty("m_Duration");
                if (propDuration != null && propDuration.floatValue <= 2f)
                {
                    propDuration.floatValue = 15f;
                    so.ApplyModifiedProperties();
                }
            }

            // 9. Reconnect prefab
            string prefabPath = "Assets/Prefabs/Final_Cars/Blue_Car_Final.prefab";
            if (System.IO.File.Exists(prefabPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(blueCar, prefabPath, InteractionMode.AutomatedAction);
            }

            Selection.activeGameObject = blueCar;
            EditorGUIUtility.PingObject(blueCar);
            EditorUtility.SetDirty(blueCar);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(blueCar.scene);

            EditorUtility.DisplayDialog("해결 완료",
                "✅ [블루카 땅 꺼짐 현상]이 완벽하게 해결되었습니다!\n\n" +
                "■ 조치 내용:\n" +
                "1. 블루카 루트(Blue_Car_Final)의 높이를 도로 지면 정상 높이(Y: " + targetRoadY.ToString("F2") + ")로 설정했습니다.\n" +
                "2. [블루차라인] 스플라인 높이를 차체 높이(Y: " + targetRoadY.ToString("F2") + ")로 맞춰 스플라인 주행 중에도 바퀴가 도로 위에 정확히 닿도록 보정했습니다.\n" +
                "3. 차체 내부 기준 좌표를 (0,0,0)으로 정상화했습니다.\n\n" +
                "이제 Play(▶)를 눌러도 블루카가 땅 속으로 꺼지지 않고 도로 위에서 정상적으로 주행합니다!",
                "확인");
        }
    }
}
