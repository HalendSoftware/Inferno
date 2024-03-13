using System.Xml.XPath;
using Sambit.Player.Client;
using Sambit.Player.Health;

namespace Sambit.Capturable;

public abstract class BaseCapturable : Component, Component.ITriggerListener
{
	/// <summary>
	/// Get a list of all capturables in the game's active scene.
	/// </summary>
	public static IEnumerable<BaseCapturable> All => Game.ActiveScene.GetAllComponents<BaseCapturable>();

	private float _captureProgress = 0;

	[Sync, Property]
	public float CaptureProgress
	{
		get => _captureProgress;
		set => _captureProgress = Math.Clamp( value, 0, 100 );
	}

	[Sync, Property] public float CaptureRange { get; set; } = 250;

	[Sync, Property,
	 Description( "How long in seconds to capture this area (Not including multiplier and player count on area)" )]
	public float CaptureTime { get; set; } = 6.0f;

	[Sync, Property] public float CaptureSpeedMultiplier { get; set; } = 1;

	[Sync, Property] public string CaptureName { get; set; } = "Capture";

	[Sync, Property, Description( "This areas capture status" )]
	public CaptureState CaptureStatus { get; set; } = CaptureState.Uncaptured;

	public enum CaptureState
	{
		Uncaptured,
		Contested,
		Capturing,
		Captured
	}

	public enum Zone
	{
		A,
		B,
		C,
		D,
		E,
		F
	}

	[Sync, Property, Description( "This areas identifier" )]
	public Zone CaptureZone { get; set; } = Zone.A;

	[Sync, Property, Description( "The team that is capturing this area" )]
	public Team CapturingTeam { get; set; } = Team.None;

	[Sync, Property, Description( "The team that captured this area" )]
	public Team CapturedTeam { get; set; } = Team.None;

	[Sync, Property] public Team LastTeamToTouch { get; set; } = Team.None;

	[Sync] public NetDictionary<Team, HashSet<Client>> Occupants { get; set; } = new();

	private TimeSince timeSinceEmpty;
	private HudMarker HudMarker { get; set; }

	protected override void OnStart()
	{
		// Add tags to the gameobject just in case someone forgets to, wont add them if they already exist
		GameObject.Tags.Add( "trigger" );
		GameObject.Tags.Add( "capturable" );
		GameObject.Networked = true;

		//if (Client.Local.IsHost)
		//	Network.TakeOwnership();

		Occupants.Clear();

		// Initialize the dictionary's list values.
		foreach ( Team team in Enum.GetValues( typeof(Team) ) )
		{
			if ( team is Team.All or Team.None )
				continue;

			Occupants[team] = new();
		}

		var collider = Components.GetOrCreate<SphereCollider>();
		collider.Radius = CaptureRange;
		collider.IsTrigger = true;

		HudMarker = Components.GetInChildren<HudMarker>();
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Tags.Has( "player" ) )
		{
			var player = other.GameObject.Parent.Components.Get<Client>();
			var playerTeam = player.CurrentTeam();

			// Already in the list!
			if ( Occupants[playerTeam].Contains( player ) )
				return;

			Occupants[playerTeam].Add( player );
		}

		// Check which team currently has the most players on this capture area
		CapturingTeam = GetCapturingTeam();

		// Debug
		//foreach (var a in Players)
		//{
		//    Log.Info($"Enter: {a}");
		//}
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( other.GameObject.Tags.Has( "player" ) )
		{
			var player = other.GameObject.Parent.Components.Get<Client>();
			var playerTeam = player.CurrentTeam();

			// If the last player on the flag leaves, set the last team that touched to that players team
			if ( GetNumberOnArea() == 1 )
			{
				LastTeamToTouch = playerTeam;
			}

			if ( !Occupants.ContainsKey( playerTeam ) )
				return;

			if ( !Occupants[playerTeam].Contains( player ) )
				return;

			Occupants[playerTeam].Remove( player );
		}

		// Recheck which team currently has the most players on this capture area
		CapturingTeam = GetCapturingTeam();

		// Debug
		//foreach (var a in Occupants)
		//{
		//	Log.Info($"Leave: {a.Key} : {string.Join(" ", a.Value)}");
		//}
	}

	// Doing the logic in here since it updates based on scene frequency instead of FPS
	protected override void OnFixedUpdate()
	{
		CheckOccupants();

		if ( CapturingTeam == Team.None )
			return;

		// Only want the owner to be able to set values
		if ( IsProxy ) return;

		// Need to broadcast some stuff for markers, since values set here are only known to the owner
		UpdateMarkers( CaptureProgress, CapturingTeam );

		// If contested, dont do anything
		if ( Contested() )
		{
			if ( CaptureStatus != CaptureState.Captured )
				CaptureStatus = CaptureState.Contested;

			return;
		}

		// If theres progress, and the enemy team touches the flag while no friendly team are on, reset the progress back to 0
		if ( CapturingTeam != LastTeamToTouch && CapturingTeam != Team.None )
		{
			if ( CaptureStatus is CaptureState.Capturing or CaptureState.Contested )
			{
				CaptureProgress = 0f;
				LastTeamToTouch = CapturingTeam;
			}
		}

		// If there is a CapturedTeam then its obviously captured
		if ( CapturedTeam != Team.None && CaptureProgress == 0 )
			CaptureStatus = CaptureState.Captured;

		// Start capping (something my teammates don't know how to do)
		if ( Occupants[CapturingTeam].Count > 0 )
		{
			switch ( CaptureStatus )
			{
				// But dont cap if the capturing team already captured this
				case CaptureState.Captured when CapturedTeam == CapturingTeam:
					return;
				case CaptureState.Captured when CapturedTeam != CapturingTeam:
					CaptureProgress = 0;
					break;
			}

			CaptureStatus = CaptureState.Capturing;
		}

		// If there is no one on this capture area and it isnt captured, start to decay the progress
		if ( Occupants.Values.All( player => player.Count == 0 ) )
		{
			if ( timeSinceEmpty > 3 && CaptureStatus != CaptureState.Captured )
				CaptureProgress -= Time.Delta * 10;
		}
		else
			timeSinceEmpty = 0;


		// Only care about CaptureState.Capturing atm
		switch ( CaptureStatus )
		{
			case CaptureState.Uncaptured:
				break;

			case CaptureState.Contested:
				return;

			case CaptureState.Capturing:
				CaptureProgress += Time.Delta * Occupants[CapturingTeam].Count * (100f / CaptureTime);

				if ( CaptureProgress >= 100 )
					OnCapture();
				break;

			case CaptureState.Captured:
				break;
		}
	}

	private void CheckOccupants()
	{
		var players = Client.All;

		foreach ( var player in players )
		{
			if ( player.Components.Get<PlayerHealth>().LifeState != Player.LifeState.Alive )
			{
				if ( Occupants[player.CurrentTeam()].Contains( player ) )
					Occupants[player.CurrentTeam()].Remove( player );
			}
		}

		CapturingTeam = GetCapturingTeam();
	}

	protected override void OnUpdate()
	{
		base.DrawGizmos();
		Gizmo.Draw.LineSphere( Transform.Position, CaptureRange );
	}

	[Broadcast]
	protected virtual void OnCapture()
	{
		CaptureStatus = CaptureState.Captured;
		CapturedTeam = CapturingTeam;
	}

	public bool Contested()
	{
		// Can't be contested if theres no one on
		if ( Occupants[Team.Alpha].Count == 0 && Occupants[Team.Bravo].Count == 0 )
			return false;

		return Occupants[Team.Alpha].Count == Occupants[Team.Bravo].Count;
	}

	public int GetNumberOnArea()
	{
		return Occupants.Values.Sum( x => x.Count );
	}

	public Team GetCapturingTeam()
	{
		if ( Occupants[Team.Alpha].Count > Occupants[Team.Bravo].Count )
			return Team.Alpha;
		else if ( Occupants[Team.Alpha].Count < Occupants[Team.Bravo].Count )
			return Team.Bravo;
		else
			return CapturingTeam;
	}

	[Broadcast]
	public void UpdateMarkers( float capProgress, Team capturingTeam )
	{
		if ( HudMarker == null )
			return;

		var localTeam = Client.Local.CurrentTeam();

		HudMarker.SetClass( "friendly", CapturedTeam == localTeam && CapturedTeam != Team.None );
		HudMarker.SetClass( "enemy", CapturedTeam != localTeam && CapturedTeam != Team.None );

		// Capture progress bar on the marker
		HudMarker.SetClass( "capturingfriendly", capturingTeam == localTeam );
		HudMarker.SetClass( "capturingenemy", capturingTeam != localTeam );

		HudMarker.StringColor = CapturedTeam.GetOpposingColor();
		HudMarker.Progress = capProgress;
		HudMarker.ObjectiveLabel = CapturedTeam == localTeam && CapturedTeam != Team.None ? "Defend" : "Capture";
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmo.Draw.LineSphere( Transform.Position, CaptureRange );
	}
}
