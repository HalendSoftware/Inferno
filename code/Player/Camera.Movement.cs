using Sandbox.Citizen;
using Sandbox.Player;

namespace Sambit.Player.Camera;

public sealed class CameraMovement : Component
{
    [Property] public PlayerController Player { get; set; }
    [Property] public CameraComponent Camera;
    [Property] public GameObject Body { get; set; }
    [Property] public GameObject Head { get; set; }
    [Property] public CitizenAnimationHelper AnimationHelper { get; set; }
    [Property] public float Distance { get; set; } = 0f;

    //This should let us do Third Person and First Person swaps pretty easy
    [Property] public bool FirstPersonMode { get; set; }

    //Variables
    public bool IsFirstPerson => Distance == 0;
    [Sync] public Angles EyeAngles { get; set; }

    private Vector3 CurrentOffset = Vector3.Zero;
    private ModelRenderer BodyRenderer;
    private PlayerDresser PlayerDresser;
    private float FirstPersonDistance = 0f;
    private float ThirdPersonDistance = 150f;
    public Ray AimRay => new Ray(Camera.Transform.Position, Camera.Transform.Rotation.Forward);

    protected override void OnAwake()
    {
        // var camera = Scene.GetAllComponents<CameraComponent>().Where(x => x.IsMainCamera).FirstOrDefault();
        // if (camera is null) return;
        // Camera = camera; // Components.Get<CameraComponent>();
        BodyRenderer = Body.Components.Get<ModelRenderer>();
        PlayerDresser = Body.Components.Get<PlayerDresser>();
        AnimationHelper = GameObject.Components.Get<CitizenAnimationHelper>();
    }

    protected override void OnUpdate()
    {
        Camera.Enabled = !IsProxy && GameObject.Network.IsOwner;

        var renderType = (!IsProxy && IsFirstPerson)
            ? ModelRenderer.ShadowRenderType.ShadowsOnly
            : ModelRenderer.ShadowRenderType.On;
        foreach (var modelRenderer in AnimationHelper.Components.GetAll<ModelRenderer>())
        {
            modelRenderer.RenderType = renderType;
        }

        if (FirstPersonMode)
        {
            Distance = FirstPersonDistance;
        }
        else
        {
            Distance = ThirdPersonDistance;
        }

        // Update the camera position
        Camera.Transform.Position = Body.Transform.Position + CurrentOffset;

        //Rotate based on mouse movement
        var eyeAngles = EyeAngles;
        if (!IsProxy)
        {
            eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
            eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
            eyeAngles.roll = 0f;
            eyeAngles.pitch = eyeAngles.pitch.Clamp(-89f, 89f);
            EyeAngles = eyeAngles.ToRotation();
        }

        Head.Transform.Rotation = EyeAngles;

        var targetOffset = Head.Transform.Rotation.Right * 16.0f;


        if (IsFirstPerson) targetOffset = Vector3.Zero;
        if (Player.IsCrouching) targetOffset += Vector3.Down * 32f;

        // Tried some lerping to make going from 100 to 0 less, jarring, but it didn't work :( - Retro

        //float lerpFactor = Distance/100f;
        //targetOffset = Vector3.Lerp(targetOffset, Vector3.Zero, lerpFactor); 

        CurrentOffset = Vector3.Lerp(CurrentOffset, targetOffset, Time.Delta * 10f);
        if (Camera is not null && !IsProxy)
        {
            if (Input.Pressed("View"))
            {
                FirstPersonMode = !FirstPersonMode;
            }

            var camPos = Head.Transform.Position + CurrentOffset;
            if (!IsFirstPerson)
            {
                //perform a trace backwards to see where we can safely place the cam
                var camForward = eyeAngles.ToRotation().Forward;
                var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance))
                    .WithoutTags("player", "trigger")
                    .Run();

                if (camTrace.Hit)
                {
                    camPos = camTrace.HitPosition + camTrace.Normal;
                }
                else
                {
                    camPos = camTrace.EndPosition;
                }

                //show body if we're not in first person
                BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
            }
            else
            {
                BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
            }

            Camera.Transform.Position = camPos;
            Camera.Transform.Rotation = eyeAngles.ToRotation();
        }
    }
}