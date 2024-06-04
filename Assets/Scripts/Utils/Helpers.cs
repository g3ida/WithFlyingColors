using Godot;
using System;
using System.Collections.Generic;

public static class Helpers
{
    public static float SignOf(float x)
    {
        return x < 0 ? -1.0f : 1.0f;
    }

    public static bool Intersects(Vector2 circlePos, float circleRadius, Vector2 rectPos, Vector2 rect)
    {
        Vector2 circleDist = new Vector2
        {
            X = Math.Abs(circlePos.X - rectPos.X),
            Y = Math.Abs(circlePos.Y - rectPos.Y)
        };

        if (circleDist.X > (rect.X / 2.0f + circleRadius)) return false;
        if (circleDist.Y > (rect.Y / 2.0f + circleRadius)) return false;

        if (circleDist.X <= (rect.X / 2.0f)) return true;
        if (circleDist.Y <= (rect.Y / 2.0f)) return true;

        float cornerDistanceSq = Mathf.Pow(circleDist.X - rect.X / 2.0f, 2) +
                                 Mathf.Pow(circleDist.Y - rect.Y / 2.0f, 2);
        return (cornerDistanceSq <= (circleRadius * circleRadius));
    }

    public static bool ArrayContainsSceneType<T>(List<T> array, T scene) where T : Node
    {
        foreach (var el in array)
        {
            if (scene == el)
            {
                return true;
            }
        }
        return false;
    }

    // FIXME: make this extension function
    public static void SetLooping(AudioStream stream, bool looping) {
        if (stream is AudioStreamOggVorbis oggStream)
        {
            oggStream.Loop = looping;
        }
        else if (stream is AudioStreamMP3 mp3Stream)
        {
            mp3Stream.Loop = looping;
        }
        else if (stream is AudioStreamWav sampleStream)
        {
            if (looping) {
                sampleStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            } else {
                sampleStream.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
            }
        }
    }

    // FIXME please find a better logic for saving data. this is due to inconsistancies
    // for godot and c#
    public static int ParseSaveDataInt(Dictionary<string, object> save_data, string key)
    {
        return Convert.ToInt32(save_data[key]);
    }

    public static float ParseSaveDataFloat(Dictionary<string, object> save_data, string key)
    {
        return Convert.ToSingle(save_data[key]);
    }

    public static NodePath ParseSaveDataNodePath(Dictionary<string, object> save_data, string key)
    {
        return (NodePath)Convert.ToString(save_data[key]);
    }

    public static void TriggerFunctionalCheckpoint()
    {
        var checkpoint = new CheckpointArea();
        checkpoint.color_group = "blue";
        checkpoint._on_CheckpointArea_body_entered(Global.Instance().Player);
    }
}
