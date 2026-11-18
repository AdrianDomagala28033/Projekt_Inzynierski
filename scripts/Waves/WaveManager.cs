using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WaveManager : Node
{
    [Signal]
    public delegate void CountdownUpdatedEventHandler(int nextWaveNumber, float timeRemaining);

    [Signal]
    public delegate void WaveStartedEventHandler(int waveNumber, int totalEnemies);

    [Signal]
    public delegate void WaveSpawningFinishedEventHandler(int waveNumber);

    [Signal]
    public delegate void EnemySpawnedEventHandler(Node2D enemy);

    [ExportGroup("Wave Timers")]
    [Export] public float TimeBetweenWaves = 10.0f;
    [Export] public float SpawnInterval = 1.0f;
    [Export] public bool AutoStart = true;

    [ExportGroup("Difficulty Scaling")]
    [Export] public int BaseWaveBudget = 40;
    [Export] public int BudgetIncreasePerWave = 25;

    [ExportGroup("Enemies & Spawn Points")]
    [Export] public Godot.Collections.Array<EnemyConfig> EnemyPool = new();
    [Export] public Node2D[] SpawnPoints;

    public int CurrentWave { get; private set; } = 0;
    public bool IsSpawningWave { get; private set; } = false;

    private float _waveCountdownTimer = 0f;
    private float _spawnTimer = 0f;
    private Queue<EnemyConfig> _spawnQueue = new Queue<EnemyConfig>();

    public override void _Ready()
    {
        AddToGroup("WaveManager");

        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            var groupNodes = GetTree().GetNodesInGroup("EnemySpawn");
            SpawnPoints = groupNodes.OfType<Node2D>().ToArray();
        }

        if (AutoStart)
        {
            _waveCountdownTimer = TimeBetweenWaves;
        }
    }

    public override void _Process(double delta)
    {
        if (!AutoStart) return;

        if (IsSpawningWave)
        {
            ProcessWaveSpawning((float)delta);
        }
        else
        {
            _waveCountdownTimer -= (float)delta;
            EmitSignal(SignalName.CountdownUpdated, CurrentWave + 1, Mathf.Max(0f, _waveCountdownTimer));

            if (_waveCountdownTimer <= 0f)
            {
                StartNextWave();
            }
        }
    }

    public void StartNextWave()
    {
        CurrentWave++;
        List<EnemyConfig> waveUnits = GenerateWaveComposition(CurrentWave);

        _spawnQueue.Clear();
        foreach (var unit in waveUnits)
        {
            _spawnQueue.Enqueue(unit);
        }

        IsSpawningWave = true;
        _spawnTimer = 0f;

        GD.Print($"[WaveManager] Start Fali {CurrentWave}! Liczba przeciwników: {_spawnQueue.Count}");
        EmitSignal(SignalName.WaveStarted, CurrentWave, _spawnQueue.Count);
    }

    private void ProcessWaveSpawning(float delta)
    {
        _spawnTimer -= delta;

        if (_spawnTimer <= 0f)
        {
            if (_spawnQueue.Count > 0)
            {
                EnemyConfig nextEnemy = _spawnQueue.Dequeue();
                SpawnSingleEnemy(nextEnemy);
                _spawnTimer = SpawnInterval;
            }
            else
            {
                IsSpawningWave = false;
                _waveCountdownTimer = TimeBetweenWaves;
                EmitSignal(SignalName.WaveSpawningFinished, CurrentWave);
            }
        }
    }

    private void SpawnSingleEnemy(EnemyConfig config)
{
    if (config == null || config.EnemyScene == null) return;

    if (SpawnPoints == null || SpawnPoints.Length == 0)
    {
        GD.PushWarning("[WaveManager] Brak punktów spawnu! Dodaj Marker2D do grupy 'EnemySpawn'.");
        return;
    }

    Node2D chosenSpawn = SpawnPoints[GD.RandRange(0, SpawnPoints.Length - 1)];
    Node2D enemyInstance = config.EnemyScene.Instantiate<Node2D>();

    enemyInstance.GlobalPosition = chosenSpawn.GlobalPosition;

    Node parentContainer = GetTree().CurrentScene;
    parentContainer.AddChild(enemyInstance);

    EmitSignal(SignalName.EnemySpawned, enemyInstance);
}

    private List<EnemyConfig> GenerateWaveComposition(int waveNumber)
    {
        int totalBudget = BaseWaveBudget + (waveNumber * BudgetIncreasePerWave);

        float meleeRatio;
        float rangedRatio;
        float specialRatio;

        if (waveNumber <= 2)
        {
            meleeRatio = 1.0f;
            rangedRatio = 0.0f;
            specialRatio = 0.0f;
        }
        else if (waveNumber <= 5)
        {
            meleeRatio = 0.75f;
            rangedRatio = 0.25f;
            specialRatio = 0.0f;
        }
        else
        {
            meleeRatio = 0.55f;
            rangedRatio = 0.25f;
            specialRatio = 0.20f;
        }

        int specialBudget = Mathf.FloorToInt(totalBudget * specialRatio);
        int rangedBudget = Mathf.FloorToInt(totalBudget * rangedRatio);
        int meleeBudget = totalBudget - specialBudget - rangedBudget;

        List<EnemyConfig> specialUnits = FillRoleBudget(EnemyRole.Special, waveNumber, specialBudget, out int unspentSpecial);
        rangedBudget += unspentSpecial;

        List<EnemyConfig> rangedUnits = FillRoleBudget(EnemyRole.Ranged, waveNumber, rangedBudget, out int unspentRanged);
        meleeBudget += unspentRanged;

        List<EnemyConfig> meleeUnits = FillRoleBudget(EnemyRole.Melee, waveNumber, meleeBudget, out _);

        ShuffleList(meleeUnits);
        int frontlineCount = Mathf.CeilToInt(meleeUnits.Count / 2.0f);

        List<EnemyConfig> frontline = meleeUnits.Take(frontlineCount).ToList();
        List<EnemyConfig> rearguard = meleeUnits.Skip(frontlineCount).ToList();

        List<EnemyConfig> middleSupport = new List<EnemyConfig>();
        middleSupport.AddRange(rangedUnits);
        middleSupport.AddRange(specialUnits);
        ShuffleList(middleSupport);

        List<EnemyConfig> finalOrder = new List<EnemyConfig>();
        finalOrder.AddRange(frontline);
        finalOrder.AddRange(middleSupport);
        finalOrder.AddRange(rearguard);

        return finalOrder;
    }

    private List<EnemyConfig> FillRoleBudget(EnemyRole role, int waveNumber, int budget, out int remainingBudget)
{
    List<EnemyConfig> result = new List<EnemyConfig>();
    int currentBudget = budget;

    var candidates = EnemyPool
        .Where(e => e != null && e.EnemyScene != null && e.Role == role && e.MinWave <= waveNumber && e.SpawnCost > 0)
        .ToList();

    if (candidates.Count == 0)
    {
        remainingBudget = currentBudget;
        return result;
    }

    int minCost = candidates.Min(c => c.SpawnCost);

    while (currentBudget >= minCost)
    {
        var affordable = candidates.Where(c => c.SpawnCost <= currentBudget).ToList();
        if (affordable.Count == 0) break;

        EnemyConfig picked = affordable[GD.RandRange(0, affordable.Count - 1)];
        result.Add(picked);
        currentBudget -= picked.SpawnCost;
    }

    remainingBudget = currentBudget;
    return result;
}

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}