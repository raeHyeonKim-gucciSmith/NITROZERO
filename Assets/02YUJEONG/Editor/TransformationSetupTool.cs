using UnityEditor;
using UnityEngine;

namespace YUJEONG
{
    public static class TransformationSetupTool
    {
        [MenuItem("Tools/YUJEONG/SportCar_4change_2에 변형 컨트롤러 자동 장착")]
        public static void AttachControllerToCar()
        {
            GameObject car = GameObject.Find("SportCar_4change_2");
            if (car == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에서 'SportCar_4change_2' 오브젝트를 찾을 수 없습니다. 하이어라키에서 차량 이름을 확인해주세요.", "확인");
                return;
            }

            VehicleTransformationController controller = car.GetComponent<VehicleTransformationController>();
            if (controller == null)
            {
                controller = car.AddComponent<VehicleTransformationController>();
                Undo.RegisterCreatedObjectUndo(controller, "Add VehicleTransformationController");
            }

            controller.targetCarRoot = car.transform;
            controller.AutoBindParts();

            EditorUtility.SetDirty(car);
            EditorUtility.SetDirty(controller);

            Selection.activeGameObject = car;

            EditorUtility.DisplayDialog("성공!", 
                $"'{car.name}' 차량에 변형 컨트롤러(VehicleTransformationController)가 완벽히 장착되었습니다!\n\n" +
                $"• VentLeft: {(controller.ventLeft != null ? controller.ventLeft.name : "미연결")}\n" +
                $"• VentRight: {(controller.ventRight != null ? controller.ventRight.name : "미연결")}\n" +
                $"• Scanner3D: {(controller.scanner3D != null ? controller.scanner3D.name : "미연결")}\n" +
                $"• CouplerLeft: {(controller.couplerLeft != null ? controller.couplerLeft.name : "미연결")}\n" +
                $"• CouplerRight: {(controller.couplerRight != null ? controller.couplerRight.name : "미연결")}\n" +
                $"• ArmoredCowl: {(controller.armoredCowl != null ? controller.armoredCowl.name : "미연결")}\n" +
                $"• BoosterNozzle: {(controller.boosterNozzle != null ? controller.boosterNozzle.name : "미연결")}\n" +
                $"• MainCover: {(controller.mainCover != null ? controller.mainCover.name : "미연결")}\n\n" +
                "이제 Play(▶)를 누르시면 스페이스바(Space) 또는 화면 상단 버튼으로 전체 풀 변형 시퀀스(1→2→3→4+5)가 발동하며, 1~5번 개별 키로도 조작 가능합니다.", "확인");
        }
    }
}
