using Godot;
using System;

public class PlatformTileMap : TileMap
{
    [Export]
    public float splash_darkness { get; set; } = 0.78f;

    private float animationTimer = 10;
    private Vector2 contactPosition = new Vector2(0, 0);

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    public void OnPlayerLanded(Node area, Vector2 position)
    {
        if (area == GetNode<Area2D>("Area2D"))
        {
            animationTimer = 0;
            contactPosition = position;
        }
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
            return;

        animationTimer += delta;

        if (Material is ShaderMaterial shaderMaterial)
        {
            Vector2 resolution = GetViewport().GetSizeOverride();
            Camera2D cam = Global.Instance().Camera;

            if (cam != null)
            {
                Vector2 camPos = cam.GetCameraScreenCenter();
                Vector2 currentPos = new Vector2(
                    contactPosition.x + (resolution.x / 2) - camPos.x,
                    contactPosition.y + (resolution.y / 2) - camPos.y);
                Vector2 pos = new Vector2(currentPos.x / resolution.x, currentPos.y / resolution.y);
                Vector2 positionInShaderCoords = new Vector2(pos.x, 1 - pos.y);

                shaderMaterial.SetShaderParam("u_contact_pos", positionInShaderCoords);
                shaderMaterial.SetShaderParam("u_timer", animationTimer);
                shaderMaterial.SetShaderParam("u_aspect_ratio", resolution.y / resolution.x);
                shaderMaterial.SetShaderParam("darkness", splash_darkness);
            }
        }
    }

    private void ConnectSignals()
    {
        Event.GdInstance().Connect("player_landed", this, nameof(OnPlayerLanded));
    }

    private void DisconnectSignals()
    {
        Event.GdInstance().Disconnect("player_landed", this, nameof(OnPlayerLanded));
    }
}
