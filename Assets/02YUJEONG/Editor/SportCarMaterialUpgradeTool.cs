using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    /// <summary>
    /// SportCar_1 머테리얼을 Universal Render Pipeline (URP/Lit) 셰이더로 업그레이드하고 새로고침하는 에디터 툴
    /// </summary>
    [InitializeOnLoad]
    public static class SportCarMaterialUpgradeTool
    {
        static SportCarMaterialUpgradeTool()
        {
            EditorApplication.delayCall += RefreshAndValidate;
        }

        private static void RefreshAndValidate()
        {
            // Unity 에디터가 로드되거나 스크립트 컴파일 완료 시 머테리얼 캐시 자동 갱신
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/YUJEONG/🚗 SportCar_1 머테리얼 URP 변환 및 새로고침")]
        public static void UpgradeSportCarMaterials()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[SportCarMaterialUpgradeTool] 'Universal Render Pipeline/Lit' 셰이더를 찾을 수 없습니다.");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/SportCar/Models/SportCar_1/Materials" });
            int count = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    if (mat.shader != urpLitShader)
                    {
                        mat.shader = urpLitShader;
                        EditorUtility.SetDirty(mat);
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SportCarMaterialUpgradeTool] ✅ SportCar_1 머테리얼 {count}개를 URP/Lit 셰이더로 최적화 및 새로고침 완료했습니다!");
        }
    }
}
