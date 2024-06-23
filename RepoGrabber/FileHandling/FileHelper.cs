using Newtonsoft.Json;

namespace RepoGrabber.FileHandling
{
  internal class FileHelper
  {
    /// <summary>
    /// Read a exclusion list from  JSON file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    internal static Exclusions ReadExclusionsFromFile(string filePath)
    {
      if (File.Exists(filePath))
      {
        string json = File.ReadAllText(filePath);
        Exclusions exclusions = JsonConvert.DeserializeObject<Exclusions>(json);
        return exclusions;
      }
      else
      {
        throw new FileNotFoundException($"The file {filePath} was not found.");
      }
    }

    /// <summary>
    /// Returns a inclusion list from JSON file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    internal static Inclusions ReadInclusionsFromFile(string filePath)
    {
      if (File.Exists(filePath))
      {
        string json = File.ReadAllText(filePath);
        Inclusions inclusions = JsonConvert.DeserializeObject<Inclusions>(json);
        return inclusions;
      }
      else
      {
        throw new FileNotFoundException($"The file {filePath} was not found.");
      }
    }

    /// <summary>
    /// Searches for the readme.md file in the root directory path
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <returns></returns>
    internal static string FindReadmeFile(string directoryPath)
    {
      try
      {
        string filePath = Path.Combine(directoryPath, "README.md");
        if (File.Exists(filePath))
        {
          return filePath;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("Error on searching README.md file:");
        Console.WriteLine(e.Message);
      }
      return null;
    }
  }
}
