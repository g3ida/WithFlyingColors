namespace Wfc.Core.Persistence;

using System.Collections.Generic;
using Wfc.Core.Serialization;

/**
 * Interface for nodes that can be saved and loaded. Add nodes that you want to save
 * to the PERSISTENT_GROUP_NAME group as well.
 */
public interface IPersistent {
  public string GetSaveId(); // Unique identifier for locating this node during load
  public string Save(ISerializer serializer); // Return data to be saved
  public void Load(ISerializer serializer, string data); // Load data into the node

  // "persist", not "persistent": the constant said the latter and was referenced by nothing,
  // while every scene and every lookup used the literal. Anything that had started trusting the
  // constant would have found an empty group.
  public const string PERSISTENT_GROUP_NAME = "persist";
}
