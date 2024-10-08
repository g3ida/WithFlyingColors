using Godot;
using System.Collections.Generic;

public partial class GemsHUDContainer : Panel
{
    private GemHUD blueGemNode;
    private GemHUD pinkGemNode;
    private GemHUD yellowGemNode;
    private GemHUD purpleGemNode;

    private Dictionary<string, GemHUD> gemsNodes;

    public override void _Ready()
    {
        blueGemNode = GetNode<GemHUD>("BlueGem");
        pinkGemNode = GetNode<GemHUD>("PinkGem");
        yellowGemNode = GetNode<GemHUD>("YellowGem");
        purpleGemNode = GetNode<GemHUD>("PurpleGem");

        gemsNodes = new Dictionary<string, GemHUD>
        {
            { "blue", blueGemNode },
            { "pink", pinkGemNode },
            { "yellow", yellowGemNode },
            { "purple", purpleGemNode }
        };

        Global.Instance().GemHUD = this;
    }

    public bool IsGemCollected(string colorGroup)
    {
        if (gemsNodes.ContainsKey(colorGroup))
        {
            var gemNode = gemsNodes[colorGroup];
            if (gemNode.currentState == GemHUD.State.COLLECTED)
            {
                return true;
            }
        }
        return false;
    }
}
