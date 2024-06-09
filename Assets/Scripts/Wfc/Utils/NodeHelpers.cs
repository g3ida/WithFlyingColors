using System.Reflection;
using Godot;

public static class NodePathHelper
{
  public static void WireNodes(this Node node)
  {
    var fields = node.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

    foreach (var field in fields)
    {
      var attribute = field.GetCustomAttribute<NodePathAttribute>();

      if (attribute != null)
      {
        var path = attribute.Path;
        var targetNode = node.GetNode(path);

        if (targetNode != null && field.FieldType.IsInstanceOfType(targetNode))
        {
          field.SetValue(node, targetNode);
        }
        else
        {
          GD.PrintErr($"Unable to assign node at path '{path}' to field '{field.Name}'.");
        }
      }
    }
  }
}
