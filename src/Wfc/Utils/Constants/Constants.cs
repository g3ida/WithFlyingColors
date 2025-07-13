namespace Wfc.Utils;

using Godot;

public static class Constants {
  public const float TETRIS_BLOCK_SIZE = 72.0f;
  public const int TETRIS_POOL_WIDTH = 10;
  public const int TETRIS_POOL_HEIGHT = 18;
  public const int TETRIS_SPAWN_I = 5;
  public const int TETRIS_SPAWN_J = 2;
  public static readonly float[] TETRIS_SPEEDS = { 0.3f, 0.23f, 0.17f, 0.11f, 0.07f };
  public const int TETRIS_MAX_LEVELS = 4;

  public const float DEFAULT_DRAG_MARGIN_LR = 0.27f;
  public const float DEFAULT_DRAG_MARGIN_TB = 0.05f;
  public const int DEFAULT_CAMERA_LIMIT_LEFT = -100000;
  public const int DEFAULT_CAMERA_LIMIT_RIGHT = 100000;
  public const int DEFAULT_CAMERA_LIMIT_TOP = -100000;
  public const int DEFAULT_CAMERA_LIMIT_BOTTOM = 100000;

  public const int WORLD_TO_SCREEN = 100;
}
