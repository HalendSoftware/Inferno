namespace Sambit.Common.Interfaces;

public interface IDamagable
{
	void Damage( float damage, IDamageSource source, Guid AttackerId );
}

public interface IDamageSource
{
	public string GetSourceName() => "The Architects";
}
