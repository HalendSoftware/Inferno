namespace Sambit;

[Title("Sambit Spawn Point")]
[Category("Game")]
[Icon("accessibility_new")]
public class SambitSpawnpoint : Component
{
    public Color Color { get; set; } = "#FFFFFF";

    [Property] public Team Team { get; set; } = Team.None;

    // Force players to spawn at this spawnpoint on map load
    [Property, Description("Force players to spawn here")]
    public bool ForceSpawn { get; set; } = false;

    protected override void DrawGizmos()
    {
        base.DrawGizmos();
        Model model = Model.Load("models/editor/spawnpoint.vmdl");
        Gizmo.Hitbox.Model(model);
        Gizmo.Draw.Color = Color.WithAlpha((Gizmo.IsHovered || Gizmo.IsSelected) ? 1f : 0.7f);
        SceneObject sceneObject = Gizmo.Draw.Model(model);
        if (sceneObject != null)
        {
            Color = Team.GetColor();
            sceneObject.Flags.CastShadows = true;
        }
    }
}