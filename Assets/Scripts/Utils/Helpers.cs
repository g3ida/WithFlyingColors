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
            x = Math.Abs(circlePos.x - rectPos.x),
            y = Math.Abs(circlePos.y - rectPos.y)
        };

        if (circleDist.x > (rect.x / 2.0f + circleRadius)) return false;
        if (circleDist.y > (rect.y / 2.0f + circleRadius)) return false;

        if (circleDist.x <= (rect.x / 2.0f)) return true;
        if (circleDist.y <= (rect.y / 2.0f)) return true;

        float cornerDistanceSq = Mathf.Pow(circleDist.x - rect.x / 2.0f, 2) +
                                 Mathf.Pow(circleDist.y - rect.y / 2.0f, 2);
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
        if (stream is AudioStreamOGGVorbis oggStream)
        {
            oggStream.Loop = looping;
        }
        else if (stream is AudioStreamMP3 mp3Stream)
        {
            mp3Stream.Loop = looping;
        }
        else if (stream is AudioStreamSample sampleStream)
        {
            if (looping) {
                sampleStream.LoopMode = AudioStreamSample.LoopModeEnum.Forward;
            } else {
                sampleStream.LoopMode = AudioStreamSample.LoopModeEnum.Disabled;
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
