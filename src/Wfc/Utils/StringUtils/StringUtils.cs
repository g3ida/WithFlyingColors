namespace Wfc.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class StringUtils {
  public static string ToSnakeCase(string s) {
    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < s.Length; i++) {
      var c = s[i];
      if (i > 0 && char.IsUpper(c)) {
        sb.Append('_');
      }
      sb.Append(char.ToLowerInvariant(c));
    }
    return sb.ToString();
  }
}

