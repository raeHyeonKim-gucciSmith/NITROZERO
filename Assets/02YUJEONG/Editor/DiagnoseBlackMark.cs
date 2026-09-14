using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

namespace YUJEONG
{
    public static class DiagnoseBlackMark
    {
        [MenuItem("Tools/YUJEONG/도로 검은 선 원인 분석")]
        public static void Diagnose()
        {
            Debug.Log("=== 🔍 도로 검은 선 정밀 진단 시작 ===");

            // 1. 카메라 정보
            Camera cam = Camera.main;
            if (cam != null)
            {
                Debug.Log($"[Camera.main] Pos: {cam.transform.position}, Rot: {cam.transform.eulerAngles}, FOV: {cam.fieldOfView}, Near: {cam.nearClipPlane}, Far: {cam.farClipPlane}");
            }

            CinemachineCamera vcam = Object.FindFirstObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                Debug.Log($"[CinemachineCamera] Name: {vcam.name}, Pos: {vcam.transform.position}, Rot: {vcam.transform.eulerAngles}, Lens FOV: {vcam.Lens.FieldOfView}, Near: {vcam.Lens.NearClipPlane}, Far: {vcam.Lens.FarClipPlane}");
            }

            // 2. 도로 메시 렌더러 정보
            GameObject road = GameObject.Find("Boost On Asphalt Road");
            if (road != null)
            {
                MeshRenderer mr = road.GetComponent<MeshRenderer>();
                MeshFilter mf = road.GetComponent<MeshFilter>();
                if (mr != null)
                {
                    Debug.Log($"[Road Renderer] Bounds: {mr.bounds}, Min: {mr.bounds.min}, Max: {mr.bounds.max}, Center: {mr.bounds.center}");
                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            Debug.Log($"[Road Material] Name: {mat.name}, Shader: {mat.shader.name}");
                            if (mat.HasProperty("_MainTex")) Debug.Log($" - _MainTex: {mat.GetTexture("_MainTex")}");
                            if (mat.HasProperty("_BaseMap")) Debug.Log($" - _BaseMap: {mat.GetTexture("_BaseMap")}");
                        }
                    }
                }
                if (mf != null && mf.sharedMesh != null)
                {
                    Debug.Log($"[Road Mesh] VertexCount: {mf.sharedMesh.vertexCount}, Mesh Bounds: {mf.sharedMesh.bounds}");
                }
            }

            // 3. 카메라 레이캐스트: 카메라 중앙 방향으로 레이를 쏴서 무엇에 맞는지 검사
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit[] hits = Physics.RaycastAll(ray, 10000f);
                Debug.Log($"[Camera Center Raycast] Hit count: {hits.Length}");
                foreach (var hit in hits)
                {
                    Debug.Log($" - Hit: {hit.collider.gameObject.name} at {hit.point}, dist: {hit.distance}");
                }
            }

            // 4. Directional Light 및 그림자
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Debug.Log($"[Directional Light] Name: {l.name}, Rot: {l.transform.eulerAngles}, ShadowType: {l.shadows}, ShadowStrength: {l.shadowStrength}");
                }
            }

            // 5. 주변 지형 및 끝단 오브젝트
            Debug.Log("=== 🔍 진단 완료 ===");
        }
    }
}
