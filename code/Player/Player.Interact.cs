using Sambit.Player.Camera;
using Sambit.Player.Health;
using System.Linq;
using System;
using Sandbox.Util;

namespace Sambit.Player.Interact;

public sealed class PlayerInteract : Component
{
    [Property] private CameraMovement _cameraController;
    [Property] private Collider _collider;
    [Property] private PlayerHealth _playerHealth => Components.Get<PlayerHealth>();
    [Property] private float InteractDistance = 250f;
    [Property] private float Cooldown = 0.6f;
    [Property] public float FieldOfView = 80f;
    

    public GameObject CurrentInteractable;
    public float Progress = Single.NaN;

    private TimeSince timeSinceLastInteraction;
    private TimeSince interactionStart = 0;
    private bool interacting = false;

    protected override void OnAwake()
    {
        timeSinceLastInteraction = Cooldown;
    }

    protected override void OnFixedUpdate()
    {
        if (IsProxy)
            return;
        if (_playerHealth.LifeState == LifeState.Dead)
            return;
        if (timeSinceLastInteraction < Cooldown)
        {
            interacting = false;
            CurrentInteractable = null;
            Progress = Single.NaN;
            return;
        }


        GameObject hitInteractable = null;

        var interactables = _collider.Touching.Select(x => x.Components.Get<Interactable>())
            .Where(y => y != null)
            .ToList();
        interactables.OrderBy(a => a.Transform.Position.Distance(this.Transform.Position)).ToArray();

        foreach (var hit in interactables)
        {
            if (hit.GameObject == null)
                continue;

            var interactable = hit.GameObject.Components.Get<Interactable>();

            if (interactable.IsValid() && interactable.CanInteract(this.GameObject))
            {
                var angle = _cameraController.Head.Transform.World.Position.GetAngleFromPoints(
                    interactable.Transform.World.Position, _cameraController.Camera.Transform.World.Rotation.Forward);

                if (!interactable.FieldBased)
                {
                    if (angle > FieldOfView)
                        continue;
                }
                hitInteractable = hit.GameObject; 
                if (interactable.TimeToInteract > 0) // Hold interaction
                {
                    if (Input.Down("Interact") && CurrentInteractable == hitInteractable)
                    {
                        if (!interacting)
                        {
                            interactionStart = 0;
                            interacting = true;
                        }

                        Progress = Math.Clamp(interactionStart / interactable.TimeToInteract, 0, 1);
                        if (interactionStart > interactable.TimeToInteract)
                        {
                            try
                            {
                                interactable.Interact(GameObject);
                            }
                            catch (Exception e)
                            {
                                timeSinceLastInteraction = 0;
                                ResetState();
                                throw e;
                            }

                            timeSinceLastInteraction = 0;
                            ResetState();
                        }
                    }
                    else 
                    {
                        ResetState();
                        Progress = 0;
                    }
                }
                else // Single press interaction
                { 
                    Progress = Single.NaN;
                    if (Input.Pressed("Interact"))
                    {
                        try
                        {
                            interactable.Interact(GameObject);
                        }
                        catch (Exception e)
                        {
                            timeSinceLastInteraction = 0;
                            throw e;
                        }

                        timeSinceLastInteraction = 0;
                    }
                }

                break;
            }
        }

        if (hitInteractable.IsValid())
            CurrentInteractable = hitInteractable;
        else
            ResetState();
    }

    private void ResetState()
    {
        CurrentInteractable = null;
        Progress = Single.NaN;
        interacting = false;
    }

    /// <summary>
    /// Runs a trace with all the data we have supplied it, and returns the result
    /// </summary>
    /// <returns></returns>
    private IEnumerable<SceneTraceResult> GetInteractTrace()
    {
        var tr = Scene.Trace.Ray(_cameraController.AimRay, InteractDistance)
            .WithAnyTags("solid", "interactable")
            .Run();

        yield return tr;
    }
}