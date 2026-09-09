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
                $"• BoosterTurbine: {(controller.boosterTurbine != null ? controller.boosterTurbine.name : "미연결 (인스펙터에서 직접 할당 가능)")}\n" +
                $"• Splines (빨강->파랑->노랑->주황): [{(controller.splineRed != null ? "빨강 O" : "빨강 X")}, {(controller.splineBlue != null ? "파랑 O" : "파랑 X")}, {(controller.splineYellow != null ? "노랑 O" : "노랑 X")}, {(controller.splineOrange != null ? "주황 O" : "주황 X")}]\n" +
                $"• MainCover: {(controller.mainCover != null ? controller.mainCover.name : "미연결")}\n\n" +
                "이제 Play(▶)를 누르시면 스페이스바(Space) 또는 화면 상단 버튼으로 전체 풀 변형 시퀀스(1→2→3→4+5)가 발동하며, 1~5번 개별 키로도 조작 가능합니다.", "확인");
        }

        [MenuItem("Tools/YUJEONG/터빈 위치·각도 원복 및 중심 회전축 완벽 세팅")]
        public static void FixTurbinePivotAndSetup()
        {
            GameObject car = GameObject.Find("SportCar_4change_2");
            if (car == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에서 'SportCar_4change_2' 차량을 찾을 수 없습니다.", "확인");
                return;
            }

            VehicleTransformationController controller = car.GetComponent<VehicleTransformationController>();
            if (controller == null)
            {
                controller = car.AddComponent<VehicleTransformationController>();
            }
            controller.targetCarRoot = car.transform;
            controller.AutoBindParts();

            GameObject turbineObj = GameObject.Find("제트부스터_터빈");
            if (turbineObj == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에서 '제트부스터_터빈'을 찾을 수 없습니다.", "확인");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(car, "Fix Turbine Pivot and Setup");

            Transform nozzle = controller.boosterNozzle;
            if (nozzle == null)
            {
                GameObject nObj = GameObject.Find("대형_단발_제트_부스터_노즐");
                if (nObj != null) nozzle = nObj.transform;
            }

            // 기존에 제트부스터_터빈_회전축이 있었다면 부모를 nozzle로 잠시 분리 후 삭제
            Transform existingPivot = nozzle != null ? nozzle.Find("제트부스터_터빈_회전축") : null;
            if (existingPivot != null)
            {
                if (turbineObj.transform.IsChildOf(existingPivot))
                {
                    turbineObj.transform.SetParent(nozzle, true);
                }
                Object.DestroyImmediate(existingPivot.gameObject);
            }

            // 1. 뒤틀려 있는 각도 및 위치를 깨끗한 초기 원본 상태로 복구
            turbineObj.transform.SetParent(nozzle, false);
            turbineObj.transform.localPosition = new Vector3(0.38648725f, 0.0017319769f, 0.12650263f);
            turbineObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            turbineObj.transform.localScale = new Vector3(0.26703718f, 0.2670372f, 0.26703727f);

            // 2. 메쉬 바운드 중심(Z: 0.4892)을 계산하여 원판 정중앙에 회전축 래퍼 생성
            MeshFilter mf = turbineObj.GetComponentInChildren<MeshFilter>();
            Vector3 localCenter = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds.center : new Vector3(0f, 0f, 0.4892f);
            Vector3 worldCenter = mf != null ? mf.transform.TransformPoint(localCenter) : turbineObj.transform.TransformPoint(localCenter);

            GameObject pivotObj = new GameObject("제트부스터_터빈_회전축");
            Undo.RegisterCreatedObjectUndo(pivotObj, "Create Turbine Pivot");
            pivotObj.transform.SetParent(nozzle, false);
            pivotObj.transform.position = worldCenter;
            pivotObj.transform.rotation = turbineObj.transform.rotation;
            pivotObj.transform.localScale = Vector3.one;

            // 터빈을 새 회전축의 자식으로 지정
            turbineObj.transform.SetParent(pivotObj.transform, true);

            // 컨트롤러에 새 회전축 연결 및 기본 축(Up_Y) 설정
            controller.boosterTurbine = pivotObj.transform;
            controller.turbineAxis = VehicleTransformationController.TurbineAxis.Up_Y;

            EditorUtility.SetDirty(car);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(turbineObj);
            EditorUtility.SetDirty(pivotObj);

            Selection.activeGameObject = pivotObj;

            EditorUtility.DisplayDialog("완벽 세팅 완료!",
                "🎉 터빈의 피벗 오프셋(Z축 가장자리 편향) 문제가 완전히 해결되었습니다!\n\n" +
                "1. 원판 정중앙(Center)에 '제트부스터_터빈_회전축' 오브젝트가 생성되었습니다.\n" +
                "2. 밖으로 튀어나가고 기울어졌던 터빈 위치와 각도가 노즐 정중앙으로 깨끗하게 복구되었습니다.\n" +
                "3. 컨트롤러에 중심축이 자동 연결되었습니다.\n\n" +
                "이제 Play(▶)를 누르시면 터빈이 밖으로 튕겨나가지 않고 '제자리에 딱 고정된 채' 팽팽하게 시계/반시계 방향으로 회전합니다!", "확인");
        }

        [MenuItem("Tools/YUJEONG/바퀴 4개 회전 컨트롤러(VehicleWheelSpinController) 자동 장착")]
        public static void AttachWheelControllerToCar()
        {
            GameObject car = GameObject.Find("SportCar_4change_2");
            if (car == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에서 'SportCar_4change_2' 차량을 찾을 수 없습니다.", "확인");
                return;
            }

            VehicleWheelSpinController wheelController = car.GetComponent<VehicleWheelSpinController>();
            if (wheelController == null)
            {
                wheelController = car.AddComponent<VehicleWheelSpinController>();
                Undo.RegisterCreatedObjectUndo(wheelController, "Add VehicleWheelSpinController");
            }

            wheelController.targetCarRoot = car.transform;
            wheelController.isSpinning = false; // 기본 멈춤 상태로 초기화 (T 키를 눌러 출발)
            wheelController.AutoBindWheels();

            VehicleTransformationController transController = car.GetComponent<VehicleTransformationController>();
            if (transController != null)
            {
                transController.wheelSpinController = wheelController;
                EditorUtility.SetDirty(transController);
            }

            EditorUtility.SetDirty(car);
            EditorUtility.SetDirty(wheelController);

            Selection.activeGameObject = car;

            EditorUtility.DisplayDialog("바퀴 세팅 완료!",
                $"'{car.name}' 차량에 바퀴 회전 컨트롤러(VehicleWheelSpinController)가 완벽히 장착 및 부스터 컨트롤러와 연동되었습니다!\n\n" +
                $"• Front Left (FL): {(wheelController.wheelFL != null ? wheelController.wheelFL.name : "미연결")}\n" +
                $"• Front Right (FR): {(wheelController.wheelFR != null ? wheelController.wheelFR.name : "미연결")}\n" +
                $"• Rear Left (RL): {(wheelController.wheelRL != null ? wheelController.wheelRL.name : "미연결")}\n" +
                $"• Rear Right (RR): {(wheelController.wheelRR != null ? wheelController.wheelRR.name : "미연결")}\n\n" +
                "★ [조작 안내 - 각각 따로따로 독립 작동]\n" +
                "- T 키 / 화면 버튼: [일반 주행] 속도로 바퀴 달리기 (1080 deg/s)\n" +
                "- Space 키 / 4번 키: [차체 부스터] 변신 시퀀스 작동 (노즐 돌출, 터빈 가속, 네온 점등)\n" +
                "- B 키 / 화면 버튼: [초고속] 바퀴 회전 (2520 deg/s)\n\n" +
                "세 가지 기능이 모두 독립되어 있어 원하시는 동작을 원하는 타이밍에 자유롭게 따로 조작하실 수 있습니다!", "확인");
        }
    }
}
