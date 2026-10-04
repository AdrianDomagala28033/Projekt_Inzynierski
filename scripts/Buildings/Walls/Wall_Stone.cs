using Godot;
using System;

public partial class Wall_Stone : StaticBody2D
{
    [Export] public int MaxHealth = 200;
    private int _currentHealth;

    private AiGridManager _aiManager;
    private AnimatedSprite2D _animatedSprite;
    private CollisionShape2D _collisionShape;
    
    private bool _isDestroyed = false;
    private const int TotalFrames = 8;

    public override async void _Ready()
    {
        AddToGroup("walls");

        _currentHealth = MaxHealth;
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        _aiManager = GetNodeOrNull<AiGridManager>("../AiSystem/AiGridManager");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        UpdateSpriteFrame();
        UpdateGridPenalty();
    }

    public void TakeDamage(int damage)
    {
        if (_isDestroyed) return;

        _currentHealth -= damage;

        GD.Print($"[Mur] Otrzymano {damage} obrażeń! Pozostałe HP: {_currentHealth}/{MaxHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isDestroyed = true;
            
            if (_collisionShape != null)
            {
                _collisionShape.SetDeferred("disabled", true);
            }
        }

        UpdateSpriteFrame();
        UpdateGridPenalty();
    }

    private void UpdateGridPenalty()
    {
        if (_aiManager == null) return;

        Vector2I gridPos = new Vector2I(
            Mathf.FloorToInt(GlobalPosition.X / 16.0f), 
            Mathf.FloorToInt(GlobalPosition.Y / 16.0f)
        );

        _aiManager.SetObstaclePenalty(gridPos, _currentHealth);
    }

    private void UpdateSpriteFrame()
    {
        if (_animatedSprite == null) return;

        _animatedSprite.Stop();

        if (_isDestroyed)
        {
            _animatedSprite.Frame = TotalFrames - 1;
            return;
        }

        float healthPercent = (float)_currentHealth / MaxHealth;
        int currentFrame = Mathf.FloorToInt((1.0f - healthPercent) * (TotalFrames - 1));
        
        currentFrame = Mathf.Clamp(currentFrame, 0, TotalFrames - 1);
        _animatedSprite.Frame = currentFrame;
    }
}