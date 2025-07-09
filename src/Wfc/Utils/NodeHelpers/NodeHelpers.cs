
namespace Wfc.Utils;

using System.Collections.Generic;
using System.Reflection;
using Godot;
using Wfc.Utils.Attributes;

public static class NodeHelpers {
  public static void WireNodes(this Node node) {
    var fields = node.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

    foreach (var field in fields) {
      var attribute = field.GetCustomAttribute<NodePathAttribute>();

      if (attribute != null) {
        var path = attribute.Path;
        var targetNode = node.GetNode(path);

        if (targetNode != null && field.FieldType.IsInstanceOfType(targetNode)) {
          field.SetValue(node, targetNode);
        }
        else {
          GD.PrintErr($"Unable to assign node at path '{path}' to field '{field.Name}'.");
        }
      }
    }
  }

  public static IEnumerable<Node> GetChildrenRecursive(this Node node) {
    foreach (var child in node.GetChildren()) {
      yield return child;

      foreach (var descendant in child.GetChildrenRecursive()) {
        yield return descendant;
      }
    }
  }

  public static T InstantiateChildNode<T>(this Node parent) where T : Node {
    var node = SceneHelpers.InstantiateNode<T>();
    parent.AddChild(node);
    node.Owner = parent;
    return node;
  }
}
