using Sandbox;
using Sambit.Player.Health;
using Sambit;
using Sambit.Common;
using Sambit.Common.Interfaces;
using Sambit.Player;

public sealed class MapBoundary : Component, Component.ITriggerListener
{
	[Property] private Collider _collider { get; set; }

	public enum BoundaryType
	{
		TurnBack,
		PushBack,
		InstantDeath
	}

	[Property] private BoundaryType _boundaryType { get; set; }
	[Property] private float TurnBackTimer { get; set; }

	private bool IsTurningBack { get; set; }


	protected override void OnUpdate()
	{
		foreach ( var c in _collider.Touching )
		{
			if ( c.Tags.Has( "playercollider" ) )
			{
				c.Components.TryGet( out PlayerController playerController, FindMode.InParent );
				c.Components.TryGet( out PlayerHealth playerHealth, FindMode.InParent );

				if ( playerController.EscapingTime >= playerController.EscapingTimeLeft && playerController.IsEscaping )
				{
					playerHealth.Death();
				}
			}
		}
	}


	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "playercollider" ) )
		{
			// Log.Info( other );
			switch ( _boundaryType )
			{
				case BoundaryType.TurnBack:
					TurnBack( other );
					break;
				case BoundaryType.PushBack:
					// Perform actions specific to PushBack boundary type
					break;
				case BoundaryType.InstantDeath:
					InstantDeath( other );
					break;
				default:
					break;
			}
		}
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( other.Tags.Has( "playercollider" ) )
		{
			switch ( _boundaryType )
			{
				case BoundaryType.TurnBack:
					TurnBackExit( other );
					break;
				case BoundaryType.PushBack:
					break;
				case BoundaryType.InstantDeath:
					break;
				default:
					break;
			}
		}
	}

	void TurnBack( Collider player )
	{
		player.Components.TryGet( out PlayerController playerController, FindMode.InParent );
		playerController.EscapingTime = 0;
		playerController.IsEscaping = true;
		playerController.EscapingTimeLeft = TurnBackTimer;
	}

	void TurnBackExit( Collider player )
	{
		player.Components.TryGet( out PlayerController playerController, FindMode.InParent );
		playerController.IsEscaping = false;
	}

	void PushBack( Collider player )
	{
	}

	void InstantDeath( Collider player )
	{
		// Log.Info( player );

		player.Components.TryGet( out PlayerHealth playerHealth, FindMode.InParent );
		playerHealth.Death();
	}
}
