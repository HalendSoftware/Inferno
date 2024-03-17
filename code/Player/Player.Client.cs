using Sambit.Player.Health;

namespace Sambit.Player.Client;

/// <summary>
/// Placed on the player (or maybe just in loose space, we'll see), the client component holds network info about a player, and serves as an easy way to iterate through players in a game.
/// </summary>
public sealed class Client : Component
{
	/// <summary>
	/// Get a list of all clients in the game's active scene.
	/// </summary>
	public static IEnumerable<Client> All => Game.ActiveScene.GetAllComponents<Client>();

	public static Client Local => All.FirstOrDefault( x => x.IsMe );

	/// <summary>
	/// Are we connected to a server?
	/// </summary>
	public bool IsConnected { get; private set; } = false;

	/// <summary>
	/// Is this client me? (The local client)
	/// </summary>
	// [Property]
	public bool IsMe => Connection.Local.Id == ConnectionID;

	/// <summary>
	/// Is this client hosting the current game session?
	/// </summary>
	[Property]
	public bool IsHost { get; private set; } = false;

	/// <summary>
	/// The client's SteamId
	/// </summary>
	[Property]
	public ulong SteamId { get; private set; } = 0;

	/// <summary>
	/// The client's Controller
	/// </summary>
	[Property] public PlayerController PlayerControllerComponent;

	/// <summary>
	/// The client's Connection ID
	/// </summary>
	[Property]
	public Guid ConnectionID { get; private set; }

	/// <summary>
	/// The client's DisplayName
	/// </summary>
	[Property]
	public string DisplayName { get; private set; } = "User";

	/// <summary>
	/// The client's Current Team Component
	/// </summary>
	[Property]
	public PlayerTeam TeamComponent { get; private set; }

	public bool HasSelectedTeam { get; set; }

	public void Setup( Connection channel )
	{
		Log.Info(
			$"Setting up connection {channel.Id} for user {channel.DisplayName} ({channel.SteamId}) (local connection is {Connection.Local.Id})" );
		IsConnected = true;
		ConnectionID = channel.Id;

		SteamId = channel.SteamId;
		DisplayName = channel.DisplayName;
		IsHost = channel.IsHost;
		PlayerControllerComponent = Components.Get<PlayerController>();
		TeamComponent = Components.Get<PlayerTeam>();
		// TeamComponent.SetTeam(Team.Bravo); // Temp

		Log.Info( $"Setup: {SteamId}" );

		if ( SteamId == 76561198031113835 )
		{
			Log.Error( "Hi Carson, this game SUCKS, but can you find the secret?" );
			var Highlight = Components.Create<HighlightOutline>();
			Highlight.ObscuredColor = Color.Transparent;
		}
	}

	public override string ToString()
	{
		return $"({DisplayName} - SteamID: {SteamId} | Team: {TeamComponent.CurrentTeam})";
	}
}
