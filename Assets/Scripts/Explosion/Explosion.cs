using Godot;
using System;
using System.Collections.Generic;


public partial class ExplosionInfo: GodotObject
{
    public Node2D BlocksContainer { get; set; }
    public bool CanDetonate { get; set; }
    public Vector2 CollisionExtents { get; set; }
    public Timer DebrisTimer { get; set; }
    public bool Detonate { get; set; }
    public bool HasDetonated { get; set; }
    public int Height { get; set; }
    public int HFrames { get; set; }
    public Vector2 Offset { get; set; }
    public int VFrames { get; set; }
    public int Width { get; set; }
    public ExplosionInfo()
    {
        BlocksContainer = new Node2D();
        CanDetonate = true;
        CollisionExtents = new Vector2();
        DebrisTimer = new Timer();
        Detonate = false;
        HasDetonated = false;
        Height = 0;
        HFrames = 1;
        Offset = new Vector2();
        VFrames = 1;
        Width = 0;
    }
}

public partial class Explosion : Node2D
{
    [Export]
    public Texture2D playerTexture;
    private const int BLOCKS_PER_SIDE = 6;
    private const int BLOCKS_IMPULSE = 400;
    private const int BLOCKS_GRAVITY_SCALE = 20;
    private const float DEBRIS_MAX_TIME = 1.5f;

    private PackedScene ExplosionElementScene = (PackedScene)ResourceLoader.Load("res://Assets/Scenes/Explosion/ExplosionElement.tscn");

    [Signal]
    public delegate void ObjectDetonatedEventHandler(Explosion self);

    private readonly bool _isRandomizeSeed = false;
    private ExplosionInfo _explosionInfo = null;

    public CharacterBody2D player;

    public override void _Ready() {
        player = GetParent<CharacterBody2D>();
    }

    public void FireExplosion() {
        if (_explosionInfo.CanDetonate) {
            _explosionInfo.Detonate = true;
            CallDeferred("_Explode");
        }
    }

    private Node2D InstanceExplosionElement(int n) {
        var explosionElement = ExplosionElementScene.Instantiate<ExplosionElement>();
        explosionElement.Name = Name + "_block_" + n;
        var shape = new RectangleShape2D
        {
            Size = _explosionInfo.CollisionExtents,
        };
        // explosionElement.Mode = RigidBody2D.ModeEnum.Static;
        SetRigidBodyMode(explosionElement, true);
        explosionElement.SetupSprite(playerTexture, _explosionInfo.VFrames, _explosionInfo.HFrames, n);
        explosionElement.SetColliderShape(shape);
        explosionElement.GetCollider().Disabled = true;
        explosionElement.Visible = false;
        return explosionElement;
    }

    private void SetRigidBodyMode(RigidBody2D element, bool isStatic) {
        if (isStatic) {
            element.Freeze = true;
            element.LockRotation = true;
        } else {
            element.Freeze = false;
            element.LockRotation = false;
        }
    }

    public void Setup() {
        _explosionInfo = new ExplosionInfo();
        if (_isRandomizeSeed) {
            GD.Randomize();
        }
        SetDebrisTimer();
        _explosionInfo.VFrames = BLOCKS_PER_SIDE;
        _explosionInfo.HFrames = BLOCKS_PER_SIDE;
        _explosionInfo.Width = playerTexture.GetWidth();
        _explosionInfo.Height = playerTexture.GetHeight();
        _explosionInfo.Offset = new Vector2(_explosionInfo.Width * 0.5f, _explosionInfo.Height * 0.5f);
        _explosionInfo.CollisionExtents = new Vector2(
          _explosionInfo.Width * 0.5f / _explosionInfo.HFrames,
          _explosionInfo.Height * 0.5f / _explosionInfo.VFrames);

        int idx = 0;
        var elems = new Godot.Collections.Array<Node2D>();

        for (int x = 0; x < _explosionInfo.HFrames; x++) {
            for (int y = 0; y < _explosionInfo.VFrames; y++) {
                var explosionElement = InstanceExplosionElement(idx);
                elems.Add(explosionElement);
                explosionElement.Position = new Vector2(
                    y * (_explosionInfo.Width / _explosionInfo.HFrames) - _explosionInfo.Offset.X + _explosionInfo.CollisionExtents.X + Position.Y,
                    x * (_explosionInfo.Height / _explosionInfo.VFrames) - _explosionInfo.Offset.Y + _explosionInfo.CollisionExtents.Y + Position.Y);
                idx++;
            }
        }
        CallDeferred("AddChildren", elems);
    }

    private void SetDebrisTimer() {
        _explosionInfo.DebrisTimer.Connect("timeout", new Callable(this, nameof(OnDebrisTimerTimeout)), (uint)ConnectFlags.OneShot);
        _explosionInfo.DebrisTimer.OneShot = true;
        _explosionInfo.DebrisTimer.WaitTime = DEBRIS_MAX_TIME;
        _explosionInfo.DebrisTimer.Name = "debris_timer";
        AddChild(_explosionInfo.DebrisTimer, true);
    }

    private void _Explode() {
        var container = _explosionInfo.BlocksContainer;
        for (int i = 0; i < container.GetChildCount(); i++) {
            var child = (ExplosionElement)container.GetChild(i);
            child.Detonate(BLOCKS_IMPULSE);
        }
    }

    public override void _PhysicsProcess(double delta) {
        if (_explosionInfo.CanDetonate && _explosionInfo.Detonate) {
            Detonate();
        }
    }

    private void AddChildren(Godot.Collections.Array<Node2D> elems) {
        foreach (var elem in elems) {
            _explosionInfo.BlocksContainer.AddChild(elem, true);
        }
        AddChild(_explosionInfo.BlocksContainer);
        _explosionInfo.BlocksContainer.Owner = this;
    }

    private void Detonate() {
        _explosionInfo.CanDetonate = false;
        _explosionInfo.HasDetonated = true;
        _explosionInfo.Detonate = false;
        var container = _explosionInfo.BlocksContainer;
        for (int i = 0; i < container.GetChildCount(); i++) {
            var child = (ExplosionElement)container.GetChild(i);
            child.GravityScale = BLOCKS_GRAVITY_SCALE;
            float childScale = (float)GD.RandRange(0.5, 1.5);
            child.Scale = new Vector2(childScale, childScale);
            child.Mass = childScale;
            child.CollisionLayer = GD.Randf() < 0.5f ? 0 : player.CollisionLayer;
            child.CollisionMask = GD.Randf() < 0.5f ? 0 : player.CollisionMask;
            child.ZIndex = GD.Randf() < 0.5f ? 0 : -1;
            // FIXME: Migration 4.0 - is this ok to comment ?
            // child.Mode = RigidBody2D.ModeEnum.Rigid;
            SetRigidBodyMode(child, false);

            child.GetCollider().Disabled = false;
            child.Visible = true;
        }
        _explosionInfo.DebrisTimer.Start();
    }

    private void OnDebrisTimerTimeout() {
        var container = _explosionInfo.BlocksContainer;
        for (int i = 0; i < container.GetChildCount(); i++) {
            var child = (RigidBody2D)container.GetChild(i);
            // FIXME: Migration 4.0 - I followed this
            // https://www.reddit.com/r/godot/comments/150z2se/do_rigidbody2d_modes_still_exist_in_version_41/
            //child.Mode = RigidBody2D.ModeEnum.Static;
            SetRigidBodyMode(child, true);
        }
        EmitSignal(nameof(ObjectDetonated), this);
    }
}
