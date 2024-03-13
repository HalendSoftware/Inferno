using Sambit.Player.Client;

//Most of the Team code here is from Conquest, made by Devultj
//https://github.com/DevulTj/sbox-conquest

public enum Team
{
	None,
	Alpha,
	Bravo,
	All
}

public static class TeamExtensions
{
	public enum FriendlyStatus
	{
		Friendly,
		Hostile,
		Neutral
	}

	public static string GetHudClass( this Team team )
	{
		return team switch
		{
			Team.Bravo => "team_blue",
			Team.Alpha => "team_red",
			_ => "team_none"
		};
	}

	public static Color GetColor( this Team team )
	{
		return team switch
		{
			Team.Bravo => new Color( 0.29f, 0.54f, 0.94f, 0.80f ),
			Team.Alpha => new Color( 0.67f, 0.00f, 0.00f, 0.80f ),
			_ => new Color( 0.5f, 0.5f, 0.5f )
		};
	}

	public static string GetOpposingColor( this Team team )
	{
		var localTeam = Client.Local.TeamComponent.CurrentTeam;
		Color color;

		if ( localTeam != team )
			color = new Color( 0.67f, 0.00f, 0.00f, 0.80f );
		else
			color = new Color( 0.29f, 0.54f, 0.94f, 0.80f );

		if ( team == Team.None )
			color = new Color( 0.50f, 0.50f, 0.50f, 0.50f );

		return $"rgba({color.ToColor32().r}, {color.ToColor32().g}, {color.ToColor32().b}, {color.a})";
	}

	/// <summary>
	/// Gets a list of all players on both teams.
	/// </summary>
	public static IEnumerable<Client> GetAll()
	{
		return Game.ActiveScene.GetAllComponents<Client>()
			.Where( x => x.TeamComponent != null );
	}

	/// <summary>
	/// Gets a list of all players on a given team.
	/// </summary>
	public static IEnumerable<Client> GetAll( this Team team )
	{
		return Game.ActiveScene.GetAllComponents<Client>()
			.Where( e => e.TeamComponent.CurrentTeam == team );
	}


	/// <summary>
	/// Gets the player count of a given team.
	/// </summary>
	public static int GetCount( this Team team )
	{
		return Game.ActiveScene
			.GetAllComponents<Client>()
			.Select( x => x.TeamComponent ).Count( e => e.CurrentTeam == team );
	}

	/// <summary>
	/// Gets the team with the lowest player count.
	/// </summary>
	public static Team GetLowestCount( this Team team )
	{
		var alphaCount = GetCount( Team.Alpha );
		var bravoCount = GetCount( Team.Bravo );

		if ( bravoCount < alphaCount )
			return Team.Bravo;

		return Team.Alpha;
	}

	public static T ToEnum<T>( this string enumString )
	{
		return (T)Enum.Parse( typeof(T), enumString );
	}

	private static FriendlyStatus GetFriendState( Team one, Team two )
	{
		if ( one == Team.None || two == Team.None )
			return FriendlyStatus.Neutral;

		if ( one != two )
			return FriendlyStatus.Hostile;

		return FriendlyStatus.Friendly;
	}

	/// <summary>
	/// Is the given team friendly?
	/// </summary>
	public static bool IsFriendly( Team one, Team two )
	{
		return GetFriendState( one, two ) == FriendlyStatus.Friendly;
	}

	/// <summary>
	/// Is the given team hostile?
	/// </summary>
	public static bool IsHostile( Team one, Team two )
	{
		return GetFriendState( one, two ) == FriendlyStatus.Hostile;
	}

	/// <summary>
	/// Gets the enemy (opposite) team of a given team
	/// </summary>
	public static Team GetEnemyTeam( this PlayerTeam team )
	{
		return team.CurrentTeam switch
		{
			Team.Alpha => Team.Bravo,
			Team.Bravo => Team.Alpha,
			_ => Team.None
		};
	}

	/// <summary>
	/// Gets the team name in string form
	/// </summary>
	public static string GetName( this Team team )
	{
		return team switch
		{
			Team.Bravo => "Bravo",
			Team.Alpha => "Alpha",
			_ => "Neutral"
		};
	}

	/// <summary>
	/// Gets the team from a given client component (is this useful?)
	/// </summary>
	public static Team CurrentTeam( this Client cl )
	{
		return cl.TeamComponent?.CurrentTeam ?? Team.None;
	}

	/// <summary>
	/// Gets the team from a given gameobject/player
	/// </summary>
	public static Team CurrentTeam( this GameObject go )
	{
		return go.Components.Get<PlayerTeam>()?.CurrentTeam ?? Team.None;
	}

	public static bool CanInteract( this Team thisTeam, Team otherTeam )
	{
		var state = GetFriendState( thisTeam, otherTeam );
		return state is FriendlyStatus.Friendly or FriendlyStatus.Neutral;
	}
}
