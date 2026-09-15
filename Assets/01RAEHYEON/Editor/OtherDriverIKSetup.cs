using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Builds the same internal-target/vehicle-source hierarchy used by v1.
/// Grip markers are deliberately editable: each avatar and cockpit needs its own contact calibration.
/// </summary>
public static class OtherDriverIKSetup
{
    [MenuItem("NITROZERO/Drivers/Set up v2 and v3 hand IK")]
    private static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before setting up the driver rigs.");
            return;
        }

        var blue = FindRoot("Final_BlueCarCockpit");
        var green = FindRoot("Final_GreenCarCockpit");
        if (blue == null || green == null)
        {
            Debug.LogError("Open the scene containing Final_BlueCarCockpit and Final_GreenCarCockpit first.");
            return;
        }

        SetUpDriver(blue, "v2", true);
        SetUpDriver(green, "v3", false);
        EditorSceneManager.MarkSceneDirty(blue.scene);
        Debug.Log("v2/v3 hand IK created. Calibrate each hand grip in the Scene view before recording Timeline keys.");
    }

    private static GameObject FindRoot(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static Transform Marker(Transform parent, string name, Vector3 worldPosition, Quaternion worldRotation)
    {
        var existing = FindDescendant(parent, name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create driver IK marker");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(worldPosition, worldRotation);
        return go.transform;
    }

    private static TwoBoneIKConstraint Arm(Transform rigRoot, string prefix, string side,
        Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand,
        Transform target)
    {
        var rootBone = animator.GetBoneTransform(upper);
        var midBone = animator.GetBoneTransform(lower);
        var tipBone = animator.GetBoneTransform(hand);
        if (rootBone == null || midBone == null || tipBone == null)
        {
            Debug.LogError(prefix + " " + side + " arm bones are missing from its Humanoid Avatar.");
            return null;
        }

        var constraintObject = Marker(rigRoot, prefix + "_" + side + "ArmIK", rigRoot.position, rigRoot.rotation);
        var ik = constraintObject.GetComponent<TwoBoneIKConstraint>();
        if (ik == null) ik = Undo.AddComponent<TwoBoneIKConstraint>(constraintObject.gameObject);
        var data = ik.data;
        data.root = rootBone;
        data.mid = midBone;
        data.tip = tipBone;
        data.target = target;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = 0f;
        ik.data = data;
        ik.weight = 1f;
        EditorUtility.SetDirty(ik);
        return ik;
    }

    private static void Follow(Transform target, Transform source)
    {
        var follower = target.GetComponent<DriverIKTargetFollower>();
        if (follower == null) follower = Undo.AddComponent<DriverIKTargetFollower>(target.gameObject);
        var so = new SerializedObject(follower);
        so.FindProperty("source").objectReferenceValue = source;
        so.ApplyModifiedProperties();
        follower.CaptureOffset();
    }

    private static void SetUpDriver(GameObject cockpit, string prefix, bool actions)
    {
        var driver = FindDescendant(cockpit.transform, prefix + "_Driver");
        var wheel = FindDescendant(cockpit.transform, "SteeringWheel")
            ?? FindDescendant(cockpit.transform, "Ctrl_SteeringWheel")
            ?? FindDescendant(cockpit.transform, "Ctrl_SteeringWheel_Hub");
        if (driver == null || wheel == null)
        {
            Debug.LogError(prefix + ": driver or steering wheel is missing.");
            return;
        }

        var animator = driver.GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError(prefix + ": a valid Humanoid Animator is required.");
            return;
        }

        var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (leftHand == null || rightHand == null) return;

        var leftGrip = Marker(wheel, prefix + "_LeftHandTarget", leftHand.position, leftHand.rotation);
        var rightGrip = Marker(wheel, prefix + "_RightHandTarget", rightHand.position, rightHand.rotation);
        var rigRoot = Marker(driver, prefix + "_DriverRig", driver.position, driver.rotation);
        var rig = rigRoot.GetComponent<Rig>();
        if (rig == null) rig = Undo.AddComponent<Rig>(rigRoot.gameObject);
        rig.weight = 1f;

        // Constraints only read internal targets under this driver's Rig.
        var leftTarget = Marker(rigRoot, prefix + "_LeftHandIKTarget", leftHand.position, leftHand.rotation);
        var rightTarget = Marker(rigRoot, prefix + "_RightHandIKTarget", rightHand.position, rightHand.rotation);
        Follow(leftTarget, leftGrip);
        var rightIK = Arm(rigRoot, prefix, "Right", animator,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rightTarget);
        Arm(rigRoot, prefix, "Left", animator,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, leftTarget);

        if (actions)
        {
            var shifter = FindDescendant(cockpit.transform, "Ctrl_Shifter_Lever");
            var switchPanel = FindDescendant(cockpit.transform, "Ctrl_SwitchPanel_Toggles");
            var shifterGrip = shifter == null ? null : Marker(shifter, prefix + "_ShifterGripTarget", shifter.position, rightHand.rotation);
            var buttonGrip = switchPanel == null ? null : Marker(switchPanel, prefix + "_RedSwitchPressTarget", switchPanel.position, rightHand.rotation);
            var blend = rightTarget.GetComponent<DriverHandTargetBlend>();
            if (blend == null) blend = Undo.AddComponent<DriverHandTargetBlend>(rightTarget.gameObject);
            var so = new SerializedObject(blend);
            so.FindProperty("steeringSource").objectReferenceValue = rightGrip;
            so.FindProperty("shifterSource").objectReferenceValue = shifterGrip;
            so.FindProperty("buttonSource").objectReferenceValue = buttonGrip;
            so.FindProperty("rightArmIK").objectReferenceValue = rightIK;
            so.ApplyModifiedProperties();
            blend.CaptureSteeringOffset();
            if (shifter == null || switchPanel == null)
                Debug.LogWarning(prefix + ": shifter or switch panel was not found. Assign its source before animating.");
        }
        else Follow(rightTarget, rightGrip);

        var builder = driver.GetComponent<RigBuilder>();
        if (builder == null) builder = Undo.AddComponent<RigBuilder>(driver.gameObject);
        builder.layers.Clear();
        builder.layers.Add(new RigLayer(rig, true));
        EditorUtility.SetDirty(builder);
    }
}
