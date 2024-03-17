using Sambit.Player.Health;
using Sandbox;

public sealed class RoundManager : Component
{
	[Property] public uint MaxKills { get; set; }
	[Property] public uint AlphaKills { get; set; }
	[Property] public uint BravoKills { get; set; }

	[Property] public uint highestScore { get; set; }
	[Property] public SceneFile restartMatch { get; set; }

	protected override void OnFixedUpdate()
	{
		highestScore = Scene.GetAllComponents<PlayerHealth>()
			.Select( x => x.PlayerKills )
			.Max();

		if ( highestScore >= 50 )
		{
			var scores = Scene.GetAllComponents<PlayerHealth>();

			foreach ( PlayerHealth health in scores )
			{
				RespawnReset( health );
			}
		}
	}

	[Broadcast]
	void RespawnReset( PlayerHealth health )
	{
		health.Respawn();
		health.PlayerKills = 0;
	}
}
