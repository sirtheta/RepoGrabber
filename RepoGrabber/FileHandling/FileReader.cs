using RepoGrabber.Model;
using System.Text;
using Ude;

namespace RepoGrabber.FileHandling
{
  public class FileReader
  {
    /// <summary>
    /// Reads each file in the given folder and returns it as a List of File lines
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="exclusions"></param>
    /// <returns>List of File lines</returns>
    public static List<FileLine> ReadFiles(string folderPath, Exclusions exclusions, Inclusions inclusions)
    {
      List<FileLine> result = new();
      var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

      foreach (var file in allFiles)
      {
        string relativePath = Path.GetRelativePath(folderPath, file);
        string fileName = Path.GetFileName(file);
        string fileExtension = Path.GetExtension(file);
        string fileDirectory = Path.GetDirectoryName(relativePath);

        // Check for folder exclusions
        if (exclusions.FolderExclusions.Any(fe => fileDirectory.StartsWith(fe, StringComparison.OrdinalIgnoreCase)))
          continue;

        // Check for file exclusions and filetype inclusions
        if (exclusions.FileExclusions.Contains(fileName) || !inclusions.FileTypeInclusions.Contains(fileExtension))
          continue;

        // Read file and get encoding
        Encoding encoding = Encoding.UTF8;
        CharsetDetector cdet = new();
        using (FileStream fs = File.OpenRead(file))
        {
          cdet.Feed(fs);
          cdet.DataEnd();
        }

        if (cdet.Charset != null)
        {
          encoding = Encoding.GetEncoding(cdet.Charset);
        }

        // Add all lines of all files to the list
        string[] lines = File.ReadAllLines(file, encoding);
        for (int i = 0; i < lines.Length; i++)
        {
          if (!string.IsNullOrWhiteSpace(lines[i].ToString()))
          {
            result.Add(new FileLine
            {
              RelativePath = relativePath,
              LineNumber = i + 1,
              LineContent = lines[i]
            });
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Reads a file content to a string
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>String with file content</returns>
    public static string ReadFileContent(string filePath)
    {
      try
      {
        // Read file and get encoding
        using (FileStream fs = File.OpenRead(filePath))
        {
          CharsetDetector cdet = new();
          cdet.Feed(fs);
          cdet.DataEnd();

          Encoding encoding = Encoding.UTF8;
          if (cdet.Charset != null)
          {
            encoding = Encoding.GetEncoding(cdet.Charset);
          }


          fs.Position = 0;
          using (StreamReader sr = new(fs, encoding))
          {
            return sr.ReadToEnd();
          }
        }
      }
      catch
      {
        return null;
      }
    }
  }
}
