using RepoGrabber.FileHandling;
using RepoGrabber.Model;

namespace RepoGrabber
{
  internal class Program
  {
    static void Main(string[] args)
    {
      if ((args.Length == 1 && HelpRequired(args[0])) || args.Length < 3)
      {
        DisplayHelp();
        return;
      }

      string repoUrl = args[0];
      string directory = args[1];
      string keyWord = args[2];
      string exclusionFile = args.Length > 3 ? args[3] : "./RepoGrabber/RepoGrabber/FileHandling/Exclusions.json";
      string inclusionFile = args.Length > 4 ? args[4] : "./RepoGrabber/RepoGrabber/FileHandling/Inclusions.json";
      string dbFilePath = args.Length > 5 ? args[5] : "./branchcontent.db";

      // Get all branches from the repo to a list and update it in the database
      List<BranchList> branches = GitHandler.GetAllBranchesFromRepo(directory, repoUrl, keyWord);

      var sqliteHelper = new SQLiteHelper(dbFilePath);

      List<BranchList> existingBranches = sqliteHelper.GetExistingBranchLists();

      if (existingBranches.Count > 0)
        sqliteHelper.RemoveMatchingBranches(existingBranches, branches, sqliteHelper);


      // Get exclusions from JSON
      Exclusions exclusions = FileHelper.ReadExclusionsFromFile(exclusionFile);
      Inclusions inclusions = FileHelper.ReadInclusionsFromFile(inclusionFile);
      Console.WriteLine("In- and Exclusionlist loaded");

      // Get the relevant content from each branch
      List<BranchContent> branchContents = new();
      foreach (var branch in branches)
      {
        GitHandler.SwitchToBranchAndPull(directory, branch.BranchName);

        Console.WriteLine($"Switched to {branch.BranchName}");
        var hash = GitHandler.GetLastCommitHashOnBranch(directory);

        // get readme file of the repo
        string readmePath = FileHelper.FindReadmeFile(directory);
        // add content of the branch to the list
        branchContents.Add(new BranchContent
        {
          HeadHash = hash,
          BranchName = branch.BranchName,
          Files = FileReader.ReadFiles(directory, exclusions, inclusions),
          ReadmeContent = FileReader.ReadFileContent(readmePath)
        });
      }

      sqliteHelper.UpdateBranchContent(branchContents);
    }

    private static bool HelpRequired(string param)
    {
      return param == "-h" || param == "--help" || param == "/?";
    }

    static void DisplayHelp()
    {
      Console.WriteLine("Usage: RepoGrabber.exe <repoUrl> <directory> <keyWord> [<exclusionFile>] [<inclusionFile>] [<dbFilePath>]");
      Console.WriteLine();
      Console.WriteLine("Arguments:");
      Console.WriteLine("  Repo Url        URL to the git repository");
      Console.WriteLine("  Directory       Directory for the repository");
      Console.WriteLine("  Key Word        Only branches which contain this keyword are added");
      Console.WriteLine("  Exclusion File  (Optional) Exclusionlist for full filenames and folders in JSON, Takes default if not provided");
      Console.WriteLine("  Inclusion File  (Optional) Inclusion list for filetype extensions in JSON, Takes default if not provided");
      Console.WriteLine("  DB File Path    (Optional) Path to the SQLite Database, Takes default if not provided");
    }
  }
}
