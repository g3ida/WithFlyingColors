namespace Wfc.Utils;

using Godot;

public static class Constants {
  public const float TETRIS_BLOCK_SIZE = 72.0f;

  // The size the block art is actually drawn at, bevel and frame included. It is wider than a
  // cell, so anything laying blocks on the grid has to scale it down to one: left alone, every
  // block paints over its right and bottom neighbours, and which of the two wins comes down to
  // the order they happen to be drawn in.
  public const float TETRIS_BLOCK_ART_SIZE = 74.0f;
  public const int TETRIS_POOL_WIDTH = 10;
  public const int TETRIS_POOL_HEIGHT = 18;
  public const int TETRIS_SPAWN_I = 5;
  public const int TETRIS_SPAWN_J = 2;

  // The escape wall stands in the grid one column past the playfield, where no piece can be
  // placed: it takes no part in completing a line, but a line that clears takes its brick with
  // it and the bricks above collapse onto the gap.
  public const int TETRIS_ESCAPE_WALL_I = TETRIS_POOL_WIDTH;
  public const int TETRIS_GRID_WIDTH = TETRIS_POOL_WIDTH + 1;
  public static readonly float[] TETRIS_SPEEDS = { 0.3f, 0.23f, 0.17f, 0.11f, 0.07f };
  public const int TETRIS_MAX_LEVELS = 4;

  // Pieces and cleared rows slide down at this rate instead of jumping a whole cell in a
  // single frame. Block bodies are moved by transform, so a jump that size drops one deep
  // inside whoever is standing there with no contact generated on the way in.
  public const float TETRIS_MAX_FALL_SPEED = 720.0f;

  public const float DEFAULT_DRAG_MARGIN_LR = 0.27f;
  public const float DEFAULT_DRAG_MARGIN_TB = 0.05f;
  public const int DEFAULT_CAMERA_LIMIT_LEFT = -100000;
  public const int DEFAULT_CAMERA_LIMIT_RIGHT = 100000;
  public const int DEFAULT_CAMERA_LIMIT_TOP = -100000;
  public const int DEFAULT_CAMERA_LIMIT_BOTTOM = 100000;

  public const int WORLD_TO_SCREEN = 100;
}
