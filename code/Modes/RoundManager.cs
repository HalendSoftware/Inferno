using Sandbox;

public sealed class RoundManager : Component
{
	[Property] public uint MaxKills { get; set; }
	[Property] public uint AlphaKills { get; set; }
	[Property] public uint BravoKills { get; set; }

	protected override void OnUpdate()
	{
	}
}
