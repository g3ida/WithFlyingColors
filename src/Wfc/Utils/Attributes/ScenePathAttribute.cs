using System;

namespace Wfc.Utils.Attributes
{
  [AttributeUsage(AttributeTargets.Class)]
  public class ScenePathAttribute : Attribute
  {
    public string Path { get; }

    public ScenePathAttribute(string path)
    {
      Path = path;
    }
  }
}
