using System.Collections.Generic;

public interface IPersistent {
    Dictionary<string, object> save();
    void load(Dictionary<string, object> save_data);
}