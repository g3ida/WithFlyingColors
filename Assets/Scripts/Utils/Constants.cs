using Godot;

public static class Constants {
    public const float TETRIS_BLOCK_SIZE = 72.0f;
    public const int TETRIS_POOL_WIDTH = 10;
    public const int TETRIS_POOL_HEIGHT = 18;
    public const int TETRIS_SPAWN_I = 5;
    public const int TETRIS_SPAWN_J = 2;
    public static readonly float[] TETRIS_SPEEDS = { 0.3f, 0.23f, 0.17f, 0.11f, 0.07f };
    public const int TETRIS_MAX_LEVELS = 4;

    public const float EPSILON = 0.001f;
    public const float EPSILON2 = 0.0001f;

    public const float DEFAULT_DRAG_MARGIN_LR = 0.27f;
    public const float DEFAULT_DRAG_MARGIN_TB = 0.05f;
    public const float DEFAULT_CAMERA_LIMIT_LEFT = -100000;
    public const float DEFAULT_CAMERA_LIMIT_RIGHT = 100000;
    public const float DEFAULT_CAMERA_LIMIT_TOP = -100000;
    public const float DEFAULT_CAMERA_LIMIT_BOTTOM = 100000;

    public const float DEGREES_TO_RAD = Mathf.Pi / 180.0f;
    public const float RAD_TO_DEGREES = 180.0f / Mathf.Pi;
    public const float PI2 = Mathf.Pi * 0.5f;
    public const float PI3 = Mathf.Pi * 0.3333f;
    public const float PI4 = Mathf.Pi * 0.25f;
    public const float PI6 = Mathf.Pi * 0.166667f;
    public const float PI8 = Mathf.Pi * 0.125f;

    public const int WORLD_TO_SCREEN = 100;

    public static readonly string[] COLOR_GROUPS = { "blue", "pink", "purple", "yellow" };

    enum EntityType {PLATFORM, FALLZONE, LAZER, BULLET, BALL, BRICK_BREAKER}
}
