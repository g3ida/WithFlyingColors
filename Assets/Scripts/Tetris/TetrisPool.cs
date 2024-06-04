using Godot;
using System;
using System.Collections.Generic;

public partial class TetrisPool : Node2D
{
    [Signal]
    public delegate void lines_removedEventHandler(int count);

    [Signal]
    public delegate void game_overEventHandler();

    private static readonly PackedScene LevelUp = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/LevelUp.tscn");
    private static readonly PackedScene SBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/S_Block.tscn");
    private static readonly PackedScene ZBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/Z_Block.tscn");
    private static readonly PackedScene LBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/L_Block.tscn");
    private static readonly PackedScene JBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/J_Block.tscn");
    private static readonly PackedScene OBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/O_Block.tscn");
    private static readonly PackedScene TBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/T_Block.tscn");
    private static readonly PackedScene IBlock = (PackedScene)GD.Load("res://Assets/Scenes/Tetris/I_Block.tscn");

    private static readonly List<PackedScene> Tetrominos = new List<PackedScene> {
        SBlock, ZBlock, LBlock, JBlock, OBlock, TBlock, IBlock
    };

    private Godot.Collections.Array<PackedScene> randomBag = new Godot.Collections.Array<PackedScene>() ;
    private int score = 0;
    private int level = 1;
    private int highScore = 40;

    private bool isPaused = false;
    private bool haveActiveBlock = false;
    private int nbQueuedLinesToRemove = 0;
    private TetrisAI ai;
    private bool shapeIsInWaitTime = false;
    private Tetromino shape;
    private List<List<Block>> grid; // FIXME: convert this to godot navtive list
    private bool isVirgin = true;

    private Marker2D spawnPosNode;
    private ScoreBoard scoreBoardNode;
    private Timer shapeWaitTimerNode;
    private Timer removeLinesDurationTimerNode;
    private NextPiece nextPieceNode;
    private Marker2D levelUpPositionNode;
    private Node2D slidingFloorSliderNode; // FIXME: change type after c# migration
    private Area2D triggerEnterAreaNode;

    public override void _Ready()
    {
        spawnPosNode = GetNode<Marker2D>("SpawnPosition");
        scoreBoardNode = GetNode<ScoreBoard>("ScoreBoard");
        shapeWaitTimerNode = GetNode<Timer>("ShapeWaitTimer");
        removeLinesDurationTimerNode = GetNode<Timer>("RemoveLinesDurationTimer");
        nextPieceNode = GetNode<NextPiece>("NextPiece");
        levelUpPositionNode = GetNode<Marker2D>("LevelUpPosition");
        slidingFloorSliderNode = GetNode<Node2D>("SlidingFloor/SlidingPlatform");
        triggerEnterAreaNode = GetNode<Area2D>("TriggerEnterArea");

        GD.Randomize();
        InitGrid();
        ai = new TetrisAI();
        reset(true);
    }

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    private void ClearGrid()
    {
        if (grid != null)
        {
            for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++)
            {
                for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++)
                {
                    if (grid[i][j] != null)
                    {
                        grid[i][j].QueueFree();
                    }
                }
            }
            grid = null;
        }
    }

    private void InitGrid()
    {
        grid = new List<List<Block>>();
        for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++)
        {
            var list_i = new List<Block>();
            for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++)
            {
                list_i.Add(null);
            }
            grid.Add(list_i);
        }
    }

    private Dictionary<string, PackedScene> GetRandomTetrominoWithNext()
    {
        if (randomBag.Count > 1)
        {
            var current = randomBag[randomBag.Count - 1];
            randomBag.RemoveAt(randomBag.Count - 1);
            var next = randomBag[randomBag.Count - 1];
            return new Dictionary<string, PackedScene> { { "current", current }, { "next", next } };
        }
        else if (randomBag.Count == 0)
        {
            randomBag = new Godot.Collections.Array<PackedScene>(Tetrominos);
            randomBag.Shuffle();
            return GetRandomTetrominoWithNext();
        }
        else
        {
            var current = randomBag[randomBag.Count - 1];
            randomBag.RemoveAt(randomBag.Count - 1);
            randomBag = new Godot.Collections.Array<PackedScene>(Tetrominos);
            randomBag.Shuffle();
            randomBag.Add(current);
            return GetRandomTetrominoWithNext();
        }
    }

    private Tetromino AiSpawnBlock()
    {
        var pick = GetRandomTetrominoWithNext();
        var currentTetromino = pick["current"];
        nextPieceNode.SetNextPiece(pick["next"]);
        var best = ai.Best(grid, currentTetromino);
        var pos = (int)best["position"];
        var rot = (int)best["rotation"];
        shape = currentTetromino.Instantiate<Tetromino>();
        shape.SetGrid(grid);
        shape.MoveBy(pos, Constants.TETRIS_SPAWN_J);
        AddChild(shape);
        shape.Owner = this;
        for (int i = 0; i < rot; i++)
        {
            shape.RotateLeft();
        }
        shape.Position = spawnPosNode.Position + new Vector2(Constants.TETRIS_BLOCK_SIZE * (pos - Constants.TETRIS_SPAWN_I), 0);
        return shape;
    }

    private void GenerateBlocks()
    {
        haveActiveBlock = true;
        shape = AiSpawnBlock();

        if (!shape.CanMoveDown())
        {
            isPaused = true;
            EmitSignal(nameof(game_overEventHandler));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isPaused || nbQueuedLinesToRemove > 0) return;

        if (!haveActiveBlock)
        {
            GenerateBlocks();
        }

        if (shape != null && !shapeIsInWaitTime)
        {
            MoveShapeDown();
        }
    }

    private async void MoveShapeDown()
    {
        shapeIsInWaitTime = true;
        if (shape.MoveDownSafe())
        {
            shapeWaitTimerNode.Start();
            await ToSignal(shapeWaitTimerNode, "timeout");
        }
        else
        {
            shape.AddToGrid();
            RemoveLines();
            haveActiveBlock = false;
        }
        shapeIsInWaitTime = false;
    }

    private void RemoveLines()
    {
        var lines = DetectLines();
        if (lines.Count > 0)
        {
            EmitSignal(nameof(lines_removedEventHandler), lines.Count);
            Event.Instance().EmitTetrisLinesRemoved();
        }
        foreach (var line in lines)
        {
            RemoveLineCells(line);
        }
    }

    private async void RemoveLineCells(int line)
    {
        nbQueuedLinesToRemove += 1;
        removeLinesDurationTimerNode.WaitTime = Block.BLINK_ANIMATION_DURATION;
        for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++)
        {
            grid[i][line].Destroy();
            grid[i][line] = null;
        }
        removeLinesDurationTimerNode.Start();
        await ToSignal(removeLinesDurationTimerNode, "timeout");
        MoveDownLinesAbove(line);
        nbQueuedLinesToRemove -= 1;
    }

    private void MoveDownLinesAbove(int line)
    {
        for (int j = line - 1; j >= 0; j--)
        {
            for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++)
            {
                if (grid[i][j] != null)
                {
                    grid[i][j].J += 1;
                    grid[i][j].Position += new Vector2(0, Constants.TETRIS_BLOCK_SIZE);
                }
                if (grid[i][j + 1] != null)
                {
                    grid[i][j + 1].QueueFree();
                }
                grid[i][j + 1] = grid[i][j];
                grid[i][j] = null;
            }
        }
    }

    private List<int> DetectLines()
    {
        var linesToRemove = new List<int>();
        for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++)
        {
            bool completeLine = true;
            for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++)
            {
                if (grid[i][j] == null)
                {
                    completeLine = false;
                    break;
                }
            }
            if (completeLine)
            {
                linesToRemove.Add(j);
            }
        }
        return linesToRemove;
    }

    public void reset() { // FIXME: use optional params after c# migration.
        reset(false);
    }

    public void reset(bool firstTime)
    {
        if (isVirgin && !firstTime) return;

        isPaused = true;
        nbQueuedLinesToRemove = 0;
        score = 0;
        haveActiveBlock = false;
        shapeIsInWaitTime = false;
        shapeWaitTimerNode.WaitTime = Constants.TETRIS_SPEEDS[0];
        randomBag.Clear();
        shape?.QueueFree();
        shape = null;
        if (!firstTime)
        {
            ClearGrid();
            isPaused = false;
        }
        InitGrid();
        UpdateScoreboard();
    }

    private void UpdateScoreboard()
    {
        scoreBoardNode.SetHighScore(highScore);
        scoreBoardNode.SetScore(score);
        int oldLevel = level;
        level = score / 10 + 1;
        if (oldLevel != level)
        {
            scoreBoardNode.SetLevel(level);
            int speed = Math.Min(level, Constants.TETRIS_MAX_LEVELS);
            shapeWaitTimerNode.WaitTime = Constants.TETRIS_SPEEDS[speed];
            AudioManager.Instance().MusicTrackManager.SetPitchScale(1 + (speed - 1) * 0.1f);
            if (level > 1)
            {
                var levelUpNode = LevelUp.Instantiate<Node2D>();
                AddChild(levelUpNode);
                levelUpNode.Owner = this;
                levelUpNode.Position = levelUpPositionNode.Position;
            }
        }
    }

    private void _on_player_diying(Node area, Vector2 position, int entityType)
    {
        isPaused = true;
    }

    private void _on_TetrixPool_lines_removed(int count)
    {
        score += count;
        UpdateScoreboard();
    }

    private void _on_TetrixPool_game_over()
    {
        // Handle game over logic
    }

    private void _on_TriggerEnterArea_body_entered(Node body)
    {
        if (body != Global.Instance().Player) return;

        isPaused = false;
        slidingFloorSliderNode.Call("set_looping", false);
        slidingFloorSliderNode.Call("stop_slider", false);
        isVirgin = false;

        AudioManager.Instance().MusicTrackManager.LoadTrack("tetris");
        AudioManager.Instance().MusicTrackManager.PlayTrack("tetris");

        if (triggerEnterAreaNode != null)
        {
            triggerEnterAreaNode.QueueFree();
            triggerEnterAreaNode = null;
        }
    }

    private void ConnectSignals()
    {
        Event.Instance().Connect("player_diying", new Callable(this, nameof(_on_player_diying)));
        Event.Instance().Connect("checkpoint_loaded", new Callable(this, nameof(reset)));
        //Connect(nameof(lines_removed), this, nameof(_on_TetrixPool_lines_removed));
        //Connect(nameof(game_over), this, nameof(_on_TetrixPool_game_over));
    }

    private void DisconnectSignals()
    {
        Event.Instance().Disconnect("player_diying", new Callable(this, nameof(_on_player_diying)));
        Event.Instance().Disconnect("checkpoint_loaded", new Callable(this, nameof(reset)));
        //Disconnect(nameof(lines_removed), this, nameof(_on_TetrixPool_lines_removed));
        //Disconnect(nameof(game_over), this, nameof(_on_TetrixPool_game_over));
    }
}
