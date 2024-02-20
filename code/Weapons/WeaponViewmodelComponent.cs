using Sambit.Player.Camera;

namespace Sandbox.Weapons;

public class WeaponViewmodelComponent : Component {
    
    private SkinnedModelRenderer ModelRenderer => Components.Get<SkinnedModelRenderer>();
    private CameraMovement _camera;

    private Vector3 LocalPosition { get; set; }
    private Rotation LocalRotation { get; set; }
    private Vector3 LerpedLocalPosition { get; set; }
    private Rotation LerpedLocalRotation { get; set; }

    protected override void OnAwake() {
        _camera = Components.GetInAncestors<CameraMovement>();
    }

    protected override void OnUpdate() {
        LocalRotation = Rotation.Identity;
        LocalPosition = Vector3.Zero;

        LerpedLocalRotation = Rotation.Lerp( LerpedLocalRotation, LocalRotation, Time.Delta * 10f );
        LerpedLocalPosition = LerpedLocalPosition.LerpTo( LocalPosition, Time.Delta * 10f );
		
        Transform.LocalRotation = LerpedLocalRotation;
        Transform.LocalPosition = LerpedLocalPosition;
    }

    public void ChangeWeapon(WeaponAsset weapon) {
        ModelRenderer.Model = weapon.Model;
    }
}