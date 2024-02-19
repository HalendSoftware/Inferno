
namespace Sambit.Player;

//Do we want to keep PlayerController as this big ass component, or do we want to start splitting it into seperate components? - Retro



partial class PlayerController
{

    public TimeSince hasJumped;
    public bool hasSpecialJumped;

    //Maybe we shouldn't have this many JumpModes, but I'm sure it's probably fine. - Retro
    public enum JumpModes
    {
        HighJump,
        StrafeJump,
        TripleJump,
        BurstGlide,
        StrafeGlide,
        BalanceGlide,
        CatapultLift,
        StrafeLift,
        HighLift,
        Blink,

        //Pending Removal (probably)
        BulletJump
    }

    //Jump Modes 

    [Category("Jump Mechanics")][Property] public JumpModes JumpMode { get; set; }

    [Category("Jump Mechanics")][Property] public int NumJumps { get; set; }

    [Category("Jump Mechanics")][Property] public int MaxJumps { get; set; }
    [Category("Jump Mechanics")][Property] public float GlideBurstForce { get; set; }
    [Category("Jump Mechanics")][Property] public float LiftBurstForce { get; set; }
    [Category("Jump Mechanics")][Property] public float LiftControl { get; set; }
    [Category("Jump Mechanics")][Property] public float LiftFuel { get; set; } = 100f;


    public void JumpModeSwitch()
    {
        //It'd probably be better to have this in OnAwake, and then if we let people change it via UI we update it that way.
        //Or maybe we leave it like this and let the game figure it out itself, either way it just works. - Retro
        switch (JumpMode)
        {
            case JumpModes.TripleJump:
                MaxJumps = 2;
                break;
            case JumpModes.HighJump:
                MaxJumps = 2;
                break;
            case JumpModes.StrafeJump:
                MaxJumps = 2;
                break;

            case JumpModes.BurstGlide:
                GlideBurstForce = 400f;
                break;

            case JumpModes.StrafeGlide:
                GlideBurstForce = 100f;
                break;
            case JumpModes.BalanceGlide:
                GlideBurstForce = 200f;
                break;
            case JumpModes.CatapultLift:
                LiftBurstForce = 5f;
                LiftControl = 100f;
                break;
            case JumpModes.StrafeLift:
                LiftBurstForce = 2f;
                LiftControl = 200f;
                break;
            case JumpModes.HighLift:
                LiftBurstForce = 2f;
                LiftControl = 100f;
                break;

            case JumpModes.BulletJump:
                break;
        }
    }

    public void SpecialJump()
    {
        switch (JumpMode)
        {
            case JumpModes.TripleJump:
                if (Input.Pressed("Jump")) TripleJump();
                break;

            case JumpModes.BurstGlide:
                if (Input.Pressed("Jump")) Glide();

                break;

            case JumpModes.StrafeLift:
                if (Input.Pressed("Jump")) Lift();

                break;

            case JumpModes.BulletJump:
                if (Input.Pressed("Jump")) BulletJump();
                break;
        }
    }

    //Unbound normal Jumps from the special Jumps.
    public void Jump()
    {
        if (characterController.IsOnGround)
        {
            characterController.Punch(Vector3.Up * JumpForce);
            animationHelper.TriggerJump();
            hasJumped = 0;
        }
    }

    private void TripleJump()
    {
        Jump();
        if (NumJumps < MaxJumps)
        {
            characterController.Velocity = characterController.Velocity.WithZ(0);
            characterController.Punch(Vector3.Up * JumpForce);
            animationHelper.TriggerJump();

            NumJumps++;
        }

    }

    private void GlideBurst()
    {
        if (characterController.IsOnGround)
        {
            characterController.ApplyFriction(AirFriction);
            characterController.Punch(Vector3.Up * JumpForce);
            animationHelper.TriggerJump();
        }
    }
    private void Glide()
    {

        if (characterController.IsOnGround)
        {
            characterController.Punch(Vector3.Up * JumpForce);
            animationHelper.TriggerJump();
        }
        else if (!characterController.IsOnGround && !isGliding)
        {

            PlayerGravity= Vector3.Down * 100f;
            isGliding = true;
        }
        else if (isGliding)
        {
            characterController.Scene.PhysicsWorld.Gravity = Vector3.Down * 850f;
            isGliding = false;
        }

    }

    private void Lift()
    {

        if (characterController.IsOnGround) Jump();

        else { isLifted = !isLifted; LiftFuel -= 20f; }
    }

    private async void LiftUpdate()
    {
        var rot = Head.Transform.Rotation;
        if (LiftFuel > 0)
        {
            isLifted = true;
            if (Input.Down("Forward")) characterController.Velocity += rot.Forward * LiftBurstForce;
            if (Input.Down("Backward")) characterController.Velocity += rot.Backward * LiftBurstForce;
            if (Input.Down("Left")) characterController.Velocity += rot.Left * LiftBurstForce;
            if (Input.Down("Right")) characterController.Velocity += rot.Right * LiftBurstForce;
            characterController.Scene.PhysicsWorld.Gravity = Vector3.Down * 80f;
            AirControl = LiftControl;
            characterController.Punch(Vector3.Up * 5);
            await Task.Delay(10);
            LiftFuel -= 0.5f;
        }
        if (LiftFuel == 0 || !isLifted)
        {
            AirControl = 50;
            characterController.Scene.PhysicsWorld.Gravity = Vector3.Down * 850f;
        }
    }

    private void BulletJump()
    {
        if (characterController.IsOnGround)
        {
            characterController.Punch(Vector3.Up * JumpForce);
            animationHelper.TriggerJump();
        }

        var rot = Head.Transform.Rotation;
        if (IsCrouching)
        {
            characterController.Punch(Vector3.Up * JumpForce);
            characterController.Punch(rot.Forward * BulletJumpForce);
            animationHelper.TriggerJump();
        }
    }

    public void ResetJumps(){
        if (characterController.IsOnGround){
            NumJumps = 0;
            hasJumped = 0;
            isGliding = false;
            isLifted = false;
            characterController.Scene.PhysicsWorld.Gravity = Vector3.Down * 850f;
        }
    }
}