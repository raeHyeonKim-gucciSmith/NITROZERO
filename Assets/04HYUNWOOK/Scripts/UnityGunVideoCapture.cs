#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Linq;
public sealed class UnityGunVideoCapture : MonoBehaviour
{
    IEnumerator Start()
    {
        string folder=Path.GetFullPath("Documentation/ModelRepair/UnityGunFrames");Directory.CreateDirectory(folder);
        var original=FindObjectsByType<GunFireController>(FindObjectsSortMode.None).FirstOrDefault();
        if(original==null) { Debug.LogError("No scene gun for video"); EditorApplication.isPlaying=false; yield break; }
        var clone=Instantiate(original.gameObject,new Vector3(1000,1000,1000),original.transform.rotation);
        clone.name="VideoGun";var fire=clone.GetComponent<GunFireController>();fire.fireWithLeftMouse=false;
        foreach(var t in clone.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        var renderers=clone.GetComponentsInChildren<MeshRenderer>();Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
        var cam=new GameObject("VideoCamera").AddComponent<Camera>();cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.055f,.07f);cam.nearClipPlane=.001f;cam.farClipPlane=100;cam.orthographic=true;cam.orthographicSize=Mathf.Max(b.size.y*.85f,b.size.x*.43f);cam.cullingMask=(1<<30)|1;
        var direction=clone.transform.TransformDirection(new Vector3(.16f,.1f,-1)).normalized;cam.transform.position=b.center+direction*b.size.magnitude*2;cam.transform.LookAt(b.center,clone.transform.up);
        var rt=new RenderTexture(1280,720,24);rt.Create();cam.targetTexture=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var key=new GameObject("VideoFillLight").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.cullingMask=1<<30;key.transform.rotation=Quaternion.Euler(35,-30,0);
        var recoil=fire.recoil;
        File.WriteAllText(Path.Combine(folder,"settings.txt"),"Source="+original.name+"\nrecoilDistance="+recoil.recoilDistance+"\nretractSeconds="+recoil.retractSeconds+"\nreturnSeconds="+recoil.returnSeconds+"\nprojectileSpeed="+fire.projectileSpeed+"\nshotVolume="+fire.shotVolume+"\nclip="+AssetDatabase.GetAssetPath(fire.shotSound)+"\nmaterials="+string.Join(",",renderers.SelectMany(r=>r.sharedMaterials).Select(m=>m!=null?m.name:"null")));
        int oldRate=Time.captureFramerate;Time.captureFramerate=60;
        yield return null;
        for(int frame=0;frame<180;frame++)
        {
            if(frame==60 && !fire.Fire())throw new System.Exception("Video shot failed");
            yield return null;
            cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),tex.EncodeToPNG());
        }
        Time.captureFramerate=oldRate;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);Destroy(clone);Destroy(cam.gameObject);Destroy(key.gameObject);
        File.WriteAllText(Path.Combine(folder,"complete.txt"),"Unity camera captured 180 frames, one real Fire() call, original materials and settings.");
        EditorApplication.isPlaying=false;
    }
}
#endif

