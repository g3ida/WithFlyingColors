using System.Collections.Generic;

public interface IPersistant
{
    Dictionary<string, object> save();
    void load(Dictionary<string, object> save_data);
}