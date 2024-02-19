using Sambit.Player.Client;

public class PlayerTeam : Component
{
    [Sync, Property] public Team CurrentTeam { get; set; } = Team.None;

    public void SetTeam(Team team)
    {
        if (IsProxy)
            return;

        Log.Info($"{Components.Get<Client>().DisplayName} Changed from {CurrentTeam} to {team}");
        CurrentTeam = team;
        Log.Info($"Team is now {CurrentTeam}");
    }

    public void ChangeTeam()
    {
        if (IsProxy)
            return;

        Log.Info($"{Components.Get<Client>().DisplayName} Changed from {CurrentTeam} to {this.GetEnemyTeam()}");
        CurrentTeam = this.GetEnemyTeam();
        Log.Info($"Team is now {CurrentTeam}");
    }
}