using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;

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

    /// <summary>
    /// Convert an image to base64
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ImageToBase64(string imagePath)
    {
      try
      {
        using (Image image = Image.FromFile(imagePath))
        {
          using (MemoryStream ms = new MemoryStream())
          {
            ImageFormat format = Path.GetExtension(imagePath).ToLower() switch
            {
              ".png" => ImageFormat.Png,
              ".jpg" => ImageFormat.Jpeg,
              ".jpeg" => ImageFormat.Jpeg,
              _ => throw new ArgumentException("Unsupported image format.")
            };

            image.Save(ms, format);
            byte[] imageBytes = ms.ToArray();
            return Convert.ToBase64String(imageBytes);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error converting image to base64: {ex.Message}");
        return null;
      }
    }
  }
}
