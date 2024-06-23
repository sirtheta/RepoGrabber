using RepoGrabber.Model;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace RepoGrabber
{
  public class GitHandler
  {
    /// <summary>
    /// checks for a git repo under the given folder. Creates the folder if it does not exist
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="repoUrl"></param>
    /// <returns>bool</returns>
    static bool CheckForGitRepo(string directory)
    {
      bool isRepo = false;
      using (PowerShell powershell = PowerShell.Create())
      {
        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }
        else
        {
          powershell.AddScript($"Set-Location {directory}");
          powershell.AddScript($"git status");
          Collection<PSObject> results = powershell.Invoke();

          if (powershell.Streams.Error.Count == 0)
          {
            isRepo = true;
          }
        }
      }
      return isRepo;
    }

    /// <summary>
    /// returns a list of all branches from the repo containing the keyword, 
    /// if no keword is given, all branches are returned
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="repoUrl"></param>
    /// <param name="branches"></param>
    /// <param name="keyWord"></param>
    /// <returns>List<string></returns>
    public static List<BranchList> GetAllBranchesFromRepo(string directory, string repoUrl, string keyWord = "")
    {
      List<BranchList> branches = new();
      using (PowerShell powershell = PowerShell.Create())
      {
        // check if we need to clone the repo
        if (!CheckForGitRepo(directory))
        {
          powershell.AddScript($"git clone {repoUrl} {directory}");
        }
        else
        {
          powershell.AddScript($"Set-Location {directory}");
          powershell.AddScript($"git fetch --all");
        }
        // get every branch from repo including the hash
        powershell.AddScript("git branch -r | Select-String -Pattern \"origin/\" | ForEach-Object {\r\n" +
                              "$branch = $_.ToString().Trim().Replace(\"origin/\", \"\")\r\n" +
                              "$commitHash = git log -1 --format=\"%H\" origin/$branch\r\n" +
                              "[PSCustomObject]@" +
                              "{\r\n" +
                                "Branch = $branch\r\n" +
                                "CommitHash = $commitHash\r\n" +
                                "}" +
                              "}");

        Collection<PSObject> results = powershell.Invoke();

        foreach (var item in results)
        {
          var itemString = item.ToString();
          var match = Regex.Match(itemString, @"Branch=([^;]+); CommitHash=([^}]+)");
          if (match.Success)
          {
            string branchName = match.Groups[1].Value.Trim();
            string commitHash = match.Groups[2].Value.Trim();
            // only add branches with the keyword to the list
            if (branchName.Contains(keyWord))
              branches.Add(new BranchList
              {
                BranchName = branchName,
                HeadHash = commitHash
              });
          }
        }
      }
      return branches;
    }

    /// <summary>
    /// Switches to the given branch and pulls the branch
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="branch"></param>
    public static void SwitchToBranchAndPull(string directory, string branch)
    {
      using (PowerShell powershell = PowerShell.Create())
      {
        powershell.AddScript($"Set-Location {directory}");
        powershell.AddScript($"git checkout {branch}");
        powershell.AddScript($"git pull");

        Collection<PSObject> results = powershell.Invoke();
      }
    }

    /// <summary>
    /// Returns the last commit hash in this repo
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>string</returns>
    public static string GetLastCommitHashOnBranch(string directory)
    {
      string hash = string.Empty;
      using (PowerShell powershell = PowerShell.Create())
      {
        powershell.AddScript($"Set-Location {directory}");
        powershell.AddScript($"git rev-parse HEAD");

        Collection<PSObject> results = powershell.Invoke();

        if (results.Count > 0)
        {
          hash = results[0].ToString();
        }
      }
      return hash;
    }
  }
}
