using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpEditor;

internal static class SaveHelper
{
    public static IEnumerable<(long Timestamp, string File)> GetSortedFiles(string directory)
    {
        string[] files;

        try
        {
            files = System.IO.Directory.GetFiles(directory, "*.cs");
        }
        catch
        {
            files = new string[0];
        }


        long result = -1;

        return
            from el in files let filename = System.IO.Path.GetFileNameWithoutExtension(el)
            where filename.StartsWith("autosave") || long.TryParse(filename, out result) let result2 = result
            orderby !filename.StartsWith("autosave") ? result2 : ((DateTimeOffset)new System.IO.FileInfo(el).LastWriteTimeUtc).ToUnixTimeSeconds() descending
            select !filename.StartsWith("autosave") ? (result2, el) : (((DateTimeOffset)new System.IO.FileInfo(el).LastWriteTimeUtc).ToUnixTimeSeconds(), el);
    }
}
