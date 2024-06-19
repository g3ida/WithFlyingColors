using Godot;
using System;

public partial class OpenSimplexCameraShake : Node2D
{
    [Export] public FastNoiseLite noise;
    [Export(PropertyHint.Range, "0,1")] public float trauma = 0.0f;

    [Export] public int max_x = 150;
    [Export] public int max_y = 150;
    [Export] public int max_r = 25;

    [Export] public int time_scale = 150;
    [Export(PropertyHint.Range, "0,1")] public float decay = 0.6f;

    private float time = 0;
    private Camera2D camera;

    public override void _Ready()
    {
        camera = GetParent<Camera2D>();
    }

    public void AddTrauma(float trauma_in)
    {
        trauma = Mathf.Clamp(trauma + trauma_in, 0, 1);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        float shake = Mathf.Pow(trauma, 2);
        camera.Offset = new Vector2(
            noise.GetNoise3D(time * time_scale, 0, 0) * max_x * shake,
            noise.GetNoise3D(0, time * time_scale, 0) * max_y * shake
        );

        // Need to activate camera rotation for this to function
        camera.RotationDegrees = noise.GetNoise3D(0, 0, time * time_scale) * max_r * shake;

        if (trauma > 0)
        {
            trauma = Mathf.Clamp(trauma - ((float)delta * decay), 0, 1);
        }
    }
}
