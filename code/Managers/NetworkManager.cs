using Sandbox.Network;
using Sambit;
using Sambit.Player;
using Sambit.Player.Client;

[Title( "S&mbit Network Helper" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class NetworkManager : Component, Component.INetworkListener
{
	public static NetworkManager Instance { get; private set; }

	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property]
	public bool StartServer { get; set; } = true;

	[Property] private bool hasSelectedTeam = false;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>

	[Property]
	public GameObject PlayerPrefab { get; set; }


	public List<Connection> Connections = new();
	public Connection Host = null;
	[Sync] public long HostSteamId { get; set; }

	public List<PlayerController> Players => Game.ActiveScene.Components
		.GetAll<PlayerController>( FindMode.EnabledInSelfAndDescendants ).ToList();

	protected override void OnAwake()
	{
		base.OnAwake();
		Instance = this;
	}

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}


	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		Game.ActiveScene.PhysicsWorld.SubSteps = 4;
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );


		if ( PlayerPrefab is null )
			return;

		var startLocation = Transform.World;
		startLocation.Scale = 1;

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation );

		var client = player.Components.Create<Client>();
		client.Setup( channel );
		client.HasSelectedTeam = false;

		player.Name = $"Player - {channel.DisplayName}";
		player.BreakFromPrefab();
		player.NetworkSpawn( channel );


		//
		// Find an intial spawn location for this player
		//
		var ForceSpawnPoints = Scene.GetAllComponents<SambitSpawnpoint>()
			.Where( x => x.ForceSpawn )
			.Where( x => x.Team == client.TeamComponent.CurrentTeam ).ToList();

		var SpawnPoints = Scene.GetAllComponents<SambitSpawnpoint>()
			.Where( x => x.Team == client.TeamComponent.CurrentTeam )
			.DefaultIfEmpty( Random.Shared.FromList( Scene.GetAllComponents<SambitSpawnpoint>().ToList(), default ) )
			.ToList();

		if ( ForceSpawnPoints.Any() )
			startLocation = Random.Shared.FromList( ForceSpawnPoints, default ).GameObject.Transform.World;
		// If no Force Spawnpoints, use any spawnpoint based on team then any if no team
		else if ( SpawnPoints.Any() )
			startLocation = Random.Shared.FromList( SpawnPoints, default ).GameObject.Transform.World;
		// If theres STILL no sambit spawnpoint, use the default spawnpoint
		else
		{
			var defaultSpawns = Scene.GetAllComponents<SpawnPoint>().ToList();
			startLocation = Random.Shared.FromList( defaultSpawns, default ).GameObject.Transform.World;
		}

		player.Transform.World = startLocation;
	}

	public void OnDisconnected( Connection channel )
	{
		foreach ( var player in Players )
		{
			if ( player.Network.OwnerId == channel.Id )
			{
				player.GameObject.Destroy();
			}
		}

		Connections.Remove( channel );
	}

	public void OnBecameHost( Connection previousHost )
	{
		foreach ( var player in Players )
		{
			if ( player.SteamId == (long)previousHost.SteamId )
			{
				player.GameObject.Destroy();
			}
		}

		Host = Connections.FirstOrDefault( x => x.SteamId == (ulong)Game.SteamId );
		HostSteamId = (long)Game.SteamId;

		Log.Info( "You are now the host!" );
	}
}
