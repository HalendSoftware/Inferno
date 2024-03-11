using Sambit.Player.Health;
using Sandbox.Citizen;
using Sambit.Player.Client;

namespace Sambit.Player;

//Do we want to keep PlayerController as this big ass component, or do we want to start splitting it into seperate components? - Retro

public enum LifeState
{
	Alive,
	Dead
}

public enum ShieldState
{
	Charged,
	Charging,
	Broken
}

public partial class PlayerController : Component
{
	public CitizenAnimationHelper animationHelper;
	public CharacterController characterController;
	public PlayerHealth playerHealth;
	private ParticleEmitter particleEmitter;

	[Property] public bool IsEscaping { get; set; }
	public TimeSince EscapingTime { get; set; }
	public float EscapingTimeLeft { get; set; }
	[Sync] public bool IsCrouching { get; set; }
	public bool IsSprinting;
	public bool isGliding;
	public bool isLifted;

	private Vector3 originalGravityScale;

	public Vector3 WishVelocity = Vector3.Zero;


	[Sync] public long SteamId { get; set; }

	[Category( "Movement Mechanics" ), Property]
	public float GroundControl { get; set; } = 4.0f;

	[Category( "Movement Mechanics" ), Property]
	public float AirFriction { get; set; } = 0.1f;

	[Category( "Movement Mechanics" ), Property]
	public float AirControl { get; set; } = 50f;

	[Category( "Movement Mechanics" ), Property]
	public float Speed { get; set; } = 160f;

	[Category( "Movement Mechanics" ), Property]
	public float RunSpeed { get; set; } = 300f;

	[Category( "Movement Mechanics" ), Property]
	public float CrouchSpeed { get; set; } = 90f;

	[Category( "Movement Mechanics" ), Property]
	public float JumpForce { get; set; } = 400f;

	[Category( "Movement Mechanics" ), Property]
	public float BulletJumpForce { get; set; } = 600f;

	[Category( "Movement Mechanics" ), Property]
	public Vector3 PlayerGravity { get; set; }

	//Object References

	[Category( "Object References" ), Property]
	public GameObject Head { get; set; }

	[Category( "Object References" ), Property]
	public GameObject Body { get; set; }

	//This is for positioning for spawning Farticles when doing the various jumps
	[Category( "Object References" ), Property]
	public GameObject Feet { get; set; }

	private PlayerHealth healthManager => Components.Get<PlayerHealth>();

	// OnAwake is a method that is called when the GameObject is awakened from its inactive state.
	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
		particleEmitter = Feet.Components.Get<ParticleEmitter>();
		playerHealth = Components.Get<PlayerHealth>();
	}

	// OnUpdate is a protected method that is called every frame to update the player's state.
	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			SteamId = Game.SteamId;
		}

		UpdateAnimations();
		RotateBody();
	}

	// This method is called every fixed frame rate. 
	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;
		if ( playerHealth.LifeState == LifeState.Dead ) return;

		if ( Input.Pressed( "Jump" ) )
			Jump();
		if ( Input.Pressed( "Jump" ) && hasJumped > 0.1f )
		{
			SpecialJump();
			hasSpecialJumped = true;
		}

		IsSprinting = Input.Down( "Run" );
		UpdateCrouch();
		ResetJumps();
		BuildWishVelocity();
		Move();
		JumpModeSwitch();
	}


	// Builds the wish velocity based on the input and the current rotation of the head.
	private void BuildWishVelocity()
	{
		WishVelocity = 0;

		var rot = Head.Transform.Rotation;
		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );
		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( IsCrouching ) WishVelocity *= CrouchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	// Moves the character based on the current state of the character controller.
	private void Move()
	{
		//Get Gravity from our scene
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( characterController.IsOnGround )
		{
			//apply friction
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			//apply air control
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( AirControl ) );
			characterController.ApplyFriction( AirFriction );
		}

		//Move the character
		characterController.Move();

		//apply the second half of gravity after movement
		if ( !characterController.IsOnGround )
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		else
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
	}

	// Rotates the body of the character towards a target angle.
	private void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
		var rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 2f );
	}

	private void UpdateAnimations()
	{
		if ( animationHelper is null ) return;

		animationHelper.WithWishVelocity( WishVelocity );
		animationHelper.WithVelocity( characterController.Velocity );
		animationHelper.AimAngle = Head.Transform.Rotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook( Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f );
		animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	private void UpdateCrouch()
	{
		var eyeAngles = Head.Transform.Rotation.Angles();
		var headPos = Head.Transform.LocalPosition;
		var headUpward = eyeAngles.ToRotation().Forward;


		if ( characterController is null ) return;

		if ( Input.Pressed( "Crouch" ) && !IsCrouching )
		{
			IsCrouching = true;
			characterController.Height /= 2f;
		}

		if ( Input.Released( "Crouch" ) && IsCrouching )
		{
			var crouchTrace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Up * 64f )
				.WithoutTags( "player", "trigger", "pickup" )
				.Run();

			if ( crouchTrace.Hit == false )
			{
				IsCrouching = false;
				characterController.Height *= 2f;
			}
		}
	}
}
