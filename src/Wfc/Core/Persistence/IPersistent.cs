namespace Wfc.Core.Persistence;

using System.Collections.Generic;

/**
 * Interface for nodes that can be saved and loaded please
 * add nodes that you want to save to the "Persistent" group as well
 */
public interface IPersistent {
  public string GetSaveId(); // Unique identifier for locating this node during load
  public Dictionary<string, object> Save(); // Return data to be saved
  public void Load(Dictionary<string, object> data); // Load data into the node
  public const string PERSISTENT_GROUP_NAME = "persistent";
}
