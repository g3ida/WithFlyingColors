using Godot;
using System;

[Tool]
public class LazerBeam : Node2D
{
    private Line2D beamNode;
    private Line2D beamBgNode;
    private Position2D muzzleNode;
    private CPUParticles2D particlesNode;
    private Sprite baseNode;

    [Export]
    public string color_group { get; set; }

    public override void _Ready()
    {
        beamNode = GetNode<Line2D>("Line2D");
        beamBgNode = GetNode<Line2D>("Line2Dbackground");
        muzzleNode = GetNode<Position2D>("Muzzle");
        particlesNode = GetNode<CPUParticles2D>("Particles");
        baseNode = GetNode<Sprite>("Base");

        int colorIndex = ColorUtils.GetGroupColorIndex(color_group);
        Color color = ColorUtils.GetBasicColor(colorIndex);
        Color darkColor = ColorUtils.GetDarkColor(colorIndex);
        beamNode.DefaultColor = color;
        beamBgNode.DefaultColor = color;
        beamBgNode.DefaultColor = new Color(beamBgNode.DefaultColor, 0.63f);
        particlesNode.Color = darkColor;
        baseNode.Modulate = color;
    }

    public Physics2DDirectSpaceState SpaceState => GetWorld2d().DirectSpaceState;

    public Godot.Collections.Dictionary CastBeam()
    {
        var result = SpaceState.IntersectRay(
            muzzleNode.GlobalPosition,
            muzzleNode.GlobalPosition + Transform.x * 1000,
            new Godot.Collections.Array { this },
            2147483647, // collision mask
            true, // collide with bodies
            true // collide with areas
        );
        Vector2 rayCastPosition = (Vector2)result["position"];
        Vector2 pos = rayCastPosition * Transform;
        beamNode.SetPointPosition(1, pos);
        beamBgNode.SetPointPosition(1, pos);
        particlesNode.Position = pos;
        return result;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Engine.EditorHint)
        {
            return;
        }

        var castResult = CastBeam();
        var collider = castResult["collider"] as Node;

        if (collider != null && collider is BoxFace boxFace)
        {
            var groups = collider.GetGroups();
            if (groups.Count == 1 && groups[0] as string == color_group)
            {
                // Play some SFX maybe?
            }
            else
            {
                Event.Instance().EmitPlayerDiying(null, GlobalPosition, Constants.EntityType.LAZER);
            }
        }
    }
}
