using Sandbox.UI;

namespace Sandbox;

[Title("Bendable Screen Panel")]
[Category("UI")]
[Icon("desktop_windows")]
public class BendableScreenPanel : Component
{
    [Property, Range(0.0f, 1f)] public float Opacity { get; set; } = 1f;

    [Property] public Shader Shader { get; set; }
    [Property] public float Distortion { get; set; } = 0.2f;

    private RootPanel rootPanel;
    private bool childrenUpdated;

    IDisposable renderHook;


    protected override void OnEnabled()
    {
        rootPanel = new()
        {
            RenderedManually = true,
        };

        childrenUpdated = false;
        UpdateChildren();

        var cam = Components.Get<CameraComponent>();
        renderHook = cam.AddHookAfterUI("bend_ui_overlay", 0, RenderEffect);
    }

    protected override void OnFixedUpdate()
    {
        if (!childrenUpdated)
        {
            UpdateChildren();
            childrenUpdated = true;
        }
    }

    void UpdateChildren()
    {
        foreach (var child in Components.GetAll<PanelComponent>())
        {
            if (child.Panel != null)
                child.Panel.Parent = rootPanel;
        }
    }

    protected override void OnDestroy()
    {
        rootPanel?.Delete();
        rootPanel = null;
    }

    void RenderEffect(SceneCamera camera)
    {
        var material = Material.FromShader(Shader);

        using var rt = RenderTarget.GetTemporary(1, ImageFormat.Default, ImageFormat.None);
        Graphics.RenderTarget = rt;
        Graphics.Attributes.SetCombo("D_WORLDPANEL", 0);
        Graphics.Viewport = new Rect(0, new Vector2(rt.Width, rt.Height));
        Graphics.Clear();
        rootPanel.RenderManual(Opacity);

        Graphics.RenderTarget = null;
        Graphics.Attributes.Set("DistortionAmount", Distortion);
        Graphics.Attributes.Set("UiTexture", rt.ColorTarget);
        Graphics.Blit(material, Graphics.Attributes);
    }
}