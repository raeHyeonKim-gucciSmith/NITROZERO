using UnityEditor;

[InitializeOnLoad]
public static class RebuildRacingNaturalTerrainOnce
{
    private const string Key = "NITROZERO.RebuildRacingNaturalTerrain.v1";

    static RebuildRacingNaturalTerrainOnce()
    {
        if (!EditorPrefs.GetBool(Key, false)) EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        JCurveDriftTerrainGenerator.RebuildNaturalHeightsOnly();
        EditorPrefs.SetBool(Key, true);
    }
}
