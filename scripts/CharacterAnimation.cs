using System.Collections.Generic;
using Godot;

public partial class CharacterAnimation : TextureRect
{
    public enum CharacterState
    {
        Idle,
        Grooving,
        Hyped,
        Shameful,
        Angry,
    }

    // Godot signals must use Variant-compatible types (int, float, string, Vector2, ...)
    [Signal]
    public delegate void StateChangedEventHandler(int newState);

    public CharacterState CurrentState => _state;

    [Export] public string BaseDir = "res://assets/sprites/Characters/Cat/";
    [Export] public string FilePrefix = "T_Cat_";

    // Streak thresholds
    [Export] public int PerfectStreakForHyped = 3;
    [Export] public int MissStreakForAngry = 2;

    // Return to Idle after some time without inputs
    [Export] public float IdleTimeoutSeconds = 1.2f;

    // Enable verbose logs for debugging animation decisions
    [Export] public bool DebugLogs = false;

    private MusicData.PlayerRole _role;
    private CharacterState _state = CharacterState.Idle;

    private int _perfectStreak = 0;
    private int _missStreak = 0;

    // 1 or 2, toggled once per successful judgement (Great/Perfect)
    private int _stepIndex = 1;

    private readonly Dictionary<string, Texture2D> _cache = new();

    private bool _idleTimerActive = false;
    private double _idleRemaining = 0.0;

    public override void _Ready()
    {
        SetState(CharacterState.Idle);
    }

   public void SetRole(MusicData.PlayerRole role)
    {
        _role = role;

        if (Refs.Instance == null)
        {
            GD.PushWarning("CharacterAnimation: Refs.Instance is null (cannot resolve character directory).");
            return;
        }

        BaseDir = Refs.Instance.GetCharacterDirectoryForRole(role);
        FilePrefix = Refs.Instance.GetCharacterFilePrefixForRole(role);

        _cache.Clear();

        _perfectStreak = 0;
        _missStreak = 0;

        // Force apply Idle sprite immediately (important at startup)
        SetState(CharacterState.Idle, force: true);
    }

    public override void _Process(double delta)
    {
        if (!_idleTimerActive)
            return;

        _idleRemaining -= delta;
        if (_idleRemaining <= 0.0)
        {
            _idleTimerActive = false;
            DebugPrint("Idle timeout -> Idle");
            SetState(CharacterState.Idle);
        }
    }

    // Call this once per judged input (Perfect/Great/Miss)
    public void OnJudgement(Refs.Accuracy accuracy)
    {
        RestartIdleTimeout();

        DebugPrint(
            $"OnJudgement accuracy={accuracy} before state={_state} stepIndex={_stepIndex} perfectStreak={_perfectStreak} missStreak={_missStreak}"
        );

        switch (accuracy)
        {
            case Refs.Accuracy.Perfect:
                _perfectStreak++;
                _missStreak = 0;

                ToggleStepIndex();

                if (_perfectStreak >= PerfectStreakForHyped)
                {
                    SetState(CharacterState.Hyped);
                    ApplyStepSprite("Hyped");
                }
                else
                {
                    SetState(CharacterState.Grooving);
                    ApplyStepSprite("Grooving");
                }
                break;

            case Refs.Accuracy.Great:
                // Great continues grooving chain but does not increase perfect streak
                _perfectStreak = 0;
                _missStreak = 0;

                ToggleStepIndex();

                SetState(CharacterState.Grooving);
                ApplyStepSprite("Grooving");
                break;

            case Refs.Accuracy.Miss:
                _missStreak++;
                _perfectStreak = 0;

                if (_missStreak >= MissStreakForAngry)
                {
                    SetState(CharacterState.Angry);
                    SetSprite("Angry", null);
                }
                else
                {
                    SetState(CharacterState.Shameful);
                    SetSprite("Shameful", null);
                }
                break;
        }

        DebugPrint(
            $"After state={_state} stepIndex={_stepIndex} perfectStreak={_perfectStreak} missStreak={_missStreak}"
        );
    }

    private void RestartIdleTimeout()
    {
        if (IdleTimeoutSeconds <= 0f)
        {
            _idleTimerActive = false;
            return;
        }

        _idleTimerActive = true;
        _idleRemaining = IdleTimeoutSeconds;
    }

    private void SetState(CharacterState state, bool force = false)
    {
        if (!force && _state == state)
            return;

        _state = state;
        EmitSignal(SignalName.StateChanged, (int)_state);

        if (state == CharacterState.Idle)
            SetSprite("Idle", null);
    }

    private void ToggleStepIndex()
    {
        // Toggle 1 <-> 2
        _stepIndex = 3 - _stepIndex;
        DebugPrint($"ToggleStepIndex -> {_stepIndex}");
    }

    private void ApplyStepSprite(string suffix)
    {
        DebugPrint($"ApplyStepSprite suffix={suffix} idx={_stepIndex}");
        SetSprite(suffix, _stepIndex);
    }

    private void SetSprite(string suffix, int? number)
    {
        var path = BuildPath(suffix, number);

        var tex = LoadTextureCached(path);
        if (tex == null)
        {
            GD.PushWarning($"CharacterAnimation: Missing texture at {path}");
            Texture = null; // Important: do not keep previous character texture
            return;
        }

        Texture = tex;
    }

    private string BuildPath(string suffix, int? number)
    {
        var file = number.HasValue
            ? $"{FilePrefix}{suffix}{number.Value}.png"
            : $"{FilePrefix}{suffix}.png";

        return $"{BaseDir.TrimEnd('/')}/{file}";
    }

    private Texture2D LoadTextureCached(string path)
    {
        if (_cache.TryGetValue(path, out var tex))
            return tex;

        if (!ResourceLoader.Exists(path))
        {
            GD.PushWarning($"CharacterAnimation: Texture not found: {path}");
            return null;
        }

        tex = ResourceLoader.Load<Texture2D>(path);
        _cache[path] = tex;
        return tex;
    }

    private void DebugPrint(string msg)
    {
        if (!DebugLogs)
            return;

        GD.Print($"[CharacterAnimation:{Name}] {msg}");
    }
}
