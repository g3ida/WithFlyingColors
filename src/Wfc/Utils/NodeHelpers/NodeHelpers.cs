
namespace Wfc.Utils;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Wfc.Core.Logger;
using Wfc.Utils.Attributes;

public static class NodeHelpers {
  public static void WireNodes(this Node node) {
    // Walked one declared level at a time: private [NodePath] fields on a base
    // type are invisible to a single GetFields call on the runtime type, and a
    // subclassed scene (TimingLazer : LazerBeam) still has to wire its base.
    for (var type = node.GetType(); type != null && type != typeof(Node); type = type.BaseType) {
      _wireDeclaredNodes(node, type);
    }
  }

  private static void _wireDeclaredNodes(Node node, System.Type type) {
    var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

    foreach (var field in fields) {
      var attribute = field.GetCustomAttribute<NodePathAttribute>();

      if (attribute != null) {
        var path = attribute.Path;
        var targetNode = node.GetNode(path);

        if (targetNode != null && field.FieldType.IsInstanceOfType(targetNode)) {
          field.SetValue(node, targetNode);
        }
        else {
          // Debug info
          // foreach (var n in node.GetChildrenRecursive()) {
          //   Log.Error(n.GetPath());
          // }
          Log.Error($"Unable to assign node at path '{path}' to field '{field.Name}'.");
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

  // Every node of the given type below this one, depth first.
  //
  // Menus each grew their own walk to find their transitions, hint cards, settings
  // rows and binding buttons. One of them stopped at three levels deep for a
  // "performance" that never mattered on a screen built once, which put anything
  // nested one step further out of reach without saying so.
  public static IEnumerable<T> FindDescendants<T>(this Node node) where T : class =>
    node.GetChildrenRecursive().OfType<T>();

  public static T InstantiateChildNode<T>(this Node parent) where T : Node {
    var node = SceneHelpers.InstantiateNode<T>();
    parent.AddChild(node);
    node.Owner = parent;
    return node;
  }
}
