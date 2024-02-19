namespace Sambit.Common;

public class SelfDestructFlicker : Component
{
    [Property] public float FlickerRate { get; set; } = 4f;
    [Property] public float StartTime { get; set; } = 5f;
    [Property] private List<Component> _renderers { get; set; }
    private SelfDestruct _selfDestruct;

    protected override void OnAwake()
    {
        // var renderers = Components.GetAll<Renderer>().Select(r => r as Component);
        // var particleSystems = Components.GetAll<ParticleEffect>().Select(p => p as Component);
        //
        // _renderers = renderers.Concat(particleSystems).ToList();
        _selfDestruct = Components.Get<SelfDestruct>();

        if (!_selfDestruct.IsValid())
        {
            Log.Error("SelfDestructFlicker component requires a SelfDestruct component");
            Enabled = false;
        }
    }

    protected override void OnPreRender()
    {
        if (_selfDestruct.Timer < StartTime)
        {
            var rateDelta = 1 / FlickerRate;
            var enabled = _selfDestruct.Timer.Relative % rateDelta < rateDelta / 2;
            _renderers.ForEach(r => r.Enabled = enabled);
        }
        else
        {
            _renderers.ForEach(r => r.Enabled = true);
        }
    }
}