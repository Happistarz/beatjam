using System;
using System.Linq;
using Godot;

public partial class HitZoneController : TextureRect
{
	[Export] public Control NoteContainer;
	[Export] public Control SpawnPoint;

	[Export] public TrackController Track;
	[Export] public Refs.NoteType NoteType;

	[Export] public float MissOutFactor = 0.5f;

	[Export] public float ScaleOnHit = 1.15f;
	private const float ScaleReturnSpeed = 20f;

	[Signal] public delegate void NoteHitEventHandler(int noteType, int accuracy);

	private ObjectPool<NoteController> _notePool;
	private string inputAction;

	public override void _Ready()
	{
		InitializePool();
		CustomMinimumSize = new Vector2(Refs.Instance.MinimumNoteSize, Refs.Instance.MinimumNoteSize);
	}

    public void Initialize(MusicData.PlayerRole? roleOverride = null)
    {
        if (Track == null)
        {
            GD.PushError($"HitZoneController {Name}: Track is null during Initialize!");
            return;
        }

        var effectiveRole = roleOverride ?? Track.Role;
        inputAction = Refs.GetInputAction(effectiveRole, NoteType);
    }

	private void InitializePool()
	{
		if (_notePool != null)
			return;

		_notePool = new ObjectPool<NoteController>();
		AddChild(_notePool);
		_notePool.Initialize(Refs.Instance.NoteScene, initialSize: 10, maxSize: 50);
	}

	public override void _Process(double delta)
	{
		Scale = new Vector2(
			Mathf.Lerp(Scale.X, 1f, ScaleReturnSpeed * (float)delta),
			Mathf.Lerp(Scale.Y, 1f, ScaleReturnSpeed * (float)delta)
		);

		if (Input.IsActionJustPressed(inputAction))
		{
			Scale = new Vector2(ScaleOnHit, ScaleOnHit);

            if (BeatController.Instance != null && BeatController.Instance.InMetronomeMode)
                return;

            if (NoteContainer == null || NoteContainer.GetChildren().Count < 1)
                return;

			var note = GetClosestNote();
			if (note == null)
				return;

			var hitLineY = GetHitLineGlobalY();
			var distance = Math.Abs(note.GetCenterGlobalY() - hitLineY);
			var accuracy = Refs.Instance.GetNoteAccuracy(distance);

			// Visual feedback at the note position
			SpawnAccuracyFeedbackOnNote(note, accuracy);

			// Optional: print for now
			GD.Print($"Hit: role={Track.Role} lane={NoteType} accuracy={accuracy}");

			EmitSignal(SignalName.NoteHit, (int)NoteType, (int)accuracy);

			// Mark as pushed to show feedback and prevent re-hits, then return to pool after a short delay
			note.MarkPushed();
		}
	}

	private NoteController GetClosestNote()
	{
		var hitLineY = GetHitLineGlobalY();

		return NoteContainer
			.GetChildren()
			.OfType<NoteController>()
			.Where(n =>
				n.NoteType == NoteType &&
				n.PlayerRole == Track.Role &&
				!n.HasPassed &&
				n.IsTouchable
			)
			.OrderBy(n => Math.Abs(n.GetCenterGlobalY() - hitLineY))
			.FirstOrDefault();
	}

	public void SpawnNote(float speed = -1f)
	{
		if (NoteContainer == null)
		{
			GD.PushError("HitZoneController: NoteContainer not assigned.");
			return;
		}

		if (SpawnPoint == null)
		{
			GD.PushError("HitZoneController: SpawnPoint not assigned.");
			return;
		}

		if (speed < 0f)
			speed = Refs.Instance.NoteSpeed;

		var note = _notePool.Get();
		NoteContainer.AddChild(note);

		Vector2 spawnGlobal = SpawnPoint.GlobalPosition + SpawnPoint.Size * 0.5f;
		Vector2 spawnLocal = spawnGlobal - NoteContainer.GlobalPosition;

		float hitzoneBottomY = GlobalPosition.Y + Size.Y;
		float missThresholdCenterY = hitzoneBottomY + (Size.Y * MissOutFactor);

		note.Initialize(NoteType, Track.Role, spawnLocal, speed, _notePool, missThresholdCenterY);
	}

	private void SpawnAccuracyFeedbackOnNote(NoteController note, Refs.Accuracy accuracy)
	{
		if (Refs.Instance == null)
			return;

		var scene = Refs.Instance.GetFeedbackScene(accuracy);
		if (scene == null)
			return;

		if (NoteContainer == null)
			return;

		// Instantiate feedback
		var feedback = scene.Instantiate<Control>();
		NoteContainer.AddChild(feedback);

		// Ensure it draws above notes
		feedback.ZAsRelative = false;
		feedback.ZIndex = 1000;

		// Determine feedback size (Controls may have Size = 0 on first frame)
		Vector2 size = feedback.Size;
		if (size.X <= 0.1f || size.Y <= 0.1f)
			size = feedback.CustomMinimumSize;

		// Get note center in NoteContainer local space
		var noteCenterGlobal = note.GlobalPosition + note.Size * 0.5f;
		var noteCenterLocal = noteCenterGlobal - NoteContainer.GlobalPosition;

		// Read optional offset from AccuracyFeedback script
		Vector2 offset = Vector2.Zero;
		if (feedback is AccuracyFeedback accuracyFeedback)
			offset = accuracyFeedback.PositionOffset;

		// Final position
		feedback.Position = noteCenterLocal - size * 0.5f + offset;
	}

	private float GetHitLineGlobalY()
	{
		return GlobalPosition.Y + Size.Y * 0.5f;
	}

	public override void _ExitTree()
	{
		_notePool?.ReturnAll();
	}
}
