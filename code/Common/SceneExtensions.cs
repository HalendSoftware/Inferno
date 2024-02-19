using Sambit.Player.Client;

public static class SceneExtensions
{
    // public static GameObject InstantiatePath(this Scene _, string prefabPath, Vector3 position, Rotation rotation )
    //     => SceneUtility.Instantiate( SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( prefabPath ) ), position, rotation );

    public static Client FindClient( this Scene self, Guid id ) => self.GetAllComponents<Client>().First( x => x.ConnectionID == x.GameObject.Network.OwnerId );
    public static Client FindClient( this Scene self, GameObject obj ) => FindClient( self, obj.Network.OwnerId );
}