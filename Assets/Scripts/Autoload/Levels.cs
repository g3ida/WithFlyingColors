using Godot;
using System.Collections.Generic;

public class Levels: Godot.Object
{
    private static readonly List<Level> LEVELS = new List<Level>
    {
        new Level { Id = 1, Name = "Tutorial", Scene = "res://Levels/TutorialLevel.tscn" },
        new Level { Id = 2, Name = "Dark Games", Scene = "res://Levels/Level1.tscn" },
        new Level { Id = 3, Name = "One More Level", Scene = "res://Levels/Level1.tscn" }
    };
    public class Level: Godot.Object
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Scene { get; set; }
    }
}
