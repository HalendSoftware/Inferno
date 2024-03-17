using Sandbox;

public sealed class Rotata : Component
{
	protected override void OnFixedUpdate()
	{
		GameObject.Transform.Rotation *= new Angles( 0, 2, 0 );
	}
}
