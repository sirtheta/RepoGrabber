using RepoGrabber.Model;
using System.Text;
using System.Text.RegularExpressions;
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
          if (ContainsOnlyNonsense(lines[i].ToString()))
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
    /// Reads a markdown file content to a string and ads the included images as base64 in html
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>String with file content</returns>
    public static string ReadMarkdownFileContent(string filePath)
    {
      try
      {
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
            string content = sr.ReadToEnd();

            string pattern = @"!\[([^\]]*)\]\(([^)\s]+)(?:\s*=\s*(\d+x?\d*))?\)";
            content = Regex.Replace(content, pattern, match =>
            {
              string altText = match.Groups[1].Value;
              string imagePath = match.Groups[2].Value;
              string dimensions = match.Groups[3].Value;

              string absoluteImagePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), imagePath));

              if (File.Exists(absoluteImagePath))
              {
                string base64Image = FileHelper.ImageToBase64(absoluteImagePath);
                string extension = Path.GetExtension(imagePath).TrimStart('.');
                string base64String = $"data:image/{extension};base64,{base64Image}";

                string width = "";
                string height = "";
                if (!string.IsNullOrEmpty(dimensions))
                {
                  string[] dims = dimensions.Split('x');
                  width = $" width=\"{dims[0]}\"";
                  height = dims.Length > 1 ? $" height=\"{dims[1]}\"" : "";
                }

                return $"<img src=\"{base64String}\" alt=\"{altText}\"{width}{height} />";
              }

              return match.Value;
            });

            return content;
          }
        }
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// Function to exclude certain unneccessary lines from the Database
    /// like lines containing only whitespace or filling comment code like //********
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static bool ContainsOnlyNonsense(string s)
    {
      if (string.IsNullOrWhiteSpace(s)) return false;

      string pattern = @"^\s*(?:[\*\/]|<!--|-->)+\s*$";
      return !Regex.IsMatch(s, pattern);
    }
  }
}
