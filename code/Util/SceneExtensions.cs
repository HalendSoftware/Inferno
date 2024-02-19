namespace Sandbox.Util;

public static class SceneExtensions
{
    public static GameObject InstantiatePath(this Scene _, string prefabPath, Vector3 position,
        Rotation rotation)
        => SceneUtility.Instantiate(SceneUtility.GetPrefabScene(ResourceLibrary.Get<PrefabFile>(prefabPath)), position,
            rotation);
}