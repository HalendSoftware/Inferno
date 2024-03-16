using Sambit.Player.Health;
using Sandbox;

public sealed class RoundManager : Component
{
	[Property] public uint MaxKills { get; set; }
	[Property] public uint AlphaKills { get; set; }
	[Property] public uint BravoKills { get; set; }

	[Property] public uint highestScore { get; set; }

	protected override void OnFixedUpdate()
	{
		highestScore = Scene.GetAllComponents<PlayerHealth>()
			.Select( x => x.PlayerKills )
			.Max();
	}
}
