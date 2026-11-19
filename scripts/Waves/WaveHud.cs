using Godot;

public partial class WaveHud : Label
{
    [Export] public WaveManager Manager;

    public override void _Ready()
    {
        if (Manager == null)
        {
            Manager = GetTree().GetFirstNodeInGroup("WaveManager") as WaveManager;
        }

        if (Manager != null)
        {
            Manager.CountdownUpdated += OnCountdownUpdated;
            Manager.WaveStarted += OnWaveStarted;
        }
    }

    public override void _ExitTree()
    {
        if (Manager != null)
        {
            Manager.CountdownUpdated -= OnCountdownUpdated;
            Manager.WaveStarted -= OnWaveStarted;
        }
    }

    private void OnCountdownUpdated(int nextWaveNumber, float timeRemaining)
    {
        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        Text = $"Fala {nextWaveNumber} za: {minutes:D2}:{seconds:D2}";
    }

    private void OnWaveStarted(int waveNumber, int totalEnemies)
    {
        Text = $"Fala {waveNumber} w toku! (Jednostek: {totalEnemies})";
    }
}