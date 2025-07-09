namespace Wfc.Core.Persistence;

using System.Collections.Generic;
using Wfc.Core.Serialization;

/**
 * Interface for nodes that can be saved and loaded please
 * add nodes that you want to save to the "Persistent" group as well
 */
public interface IPersistent {
  public string GetSaveId(); // Unique identifier for locating this node during load
  public string Save(ISerializer serializer); // Return data to be saved
  public void Load(ISerializer serializer, string data); // Load data into the node
  public const string PERSISTENT_GROUP_NAME = "persistent";
}
