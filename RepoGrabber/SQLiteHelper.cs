using RepoGrabber.Model;
using System.Data.SQLite;

namespace RepoGrabber
{
  internal class SQLiteHelper
  {
    private string _dbFilePath;

    /// <summary>
    /// Ctor to create the SQLiteHelper object
    /// </summary>
    /// <param name="dbFilePath"></param>
    public SQLiteHelper(string dbFilePath)
    {
      _dbFilePath = dbFilePath;
      InitializeDatabase();
    }

    /// <summary>
    /// Create the database
    /// </summary>
    private void InitializeDatabase()
    {
      if (!File.Exists(_dbFilePath))
      {
        SQLiteConnection.CreateFile(_dbFilePath);
      }

      using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
      {
        connection.Open();

        string createBranchContentTable = @"
                CREATE TABLE IF NOT EXISTS BranchContent (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HeadHash TEXT,
                    BranchName TEXT,
                    ReadmeContent TEXT
                )";

        string createFileLineTable = @"
                CREATE TABLE IF NOT EXISTS FileLine (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BranchContentId INTEGER,
                    RelativePath TEXT,
                    LineNumber INTEGER,
                    LineContent TEXT,
                    FOREIGN KEY(BranchContentId) REFERENCES BranchContent(Id)
                )";

        using (var command = new SQLiteCommand(createBranchContentTable, connection))
        {
          command.ExecuteNonQuery();
        }

        using (var command = new SQLiteCommand(createFileLineTable, connection))
        {
          command.ExecuteNonQuery();
        }
      }
    }

    /// <summary>
    /// Insert the branches into the database
    /// </summary>
    /// <param name="branchContents"></param>
    internal void InsertBranchContents(List<BranchContent> branchContents)
    {
      using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
      {
        connection.Open();

        foreach (var branchContent in branchContents)
        {
          using (var transaction = connection.BeginTransaction())
          {
            string insertBranchContent = @"
                        INSERT INTO BranchContent (HeadHash, BranchName, ReadmeContent) 
                        VALUES (@HeadHash, @BranchName, @ReadmeContent);
                        SELECT last_insert_rowid()";

            long branchContentId;
            using (var command = new SQLiteCommand(insertBranchContent, connection))
            {
              command.Parameters.AddWithValue("@HeadHash", branchContent.HeadHash);
              command.Parameters.AddWithValue("@BranchName", branchContent.BranchName);
              command.Parameters.AddWithValue("@ReadmeContent", branchContent.ReadmeContent);
              branchContentId = (long)command.ExecuteScalar();
            }

            string insertFileLine = @"
                        INSERT INTO FileLine (BranchContentId, RelativePath, LineNumber, LineContent) 
                        VALUES (@BranchContentId, @RelativePath, @LineNumber, @LineContent)";

            foreach (var fileLine in branchContent.Files)
            {
              using (var command = new SQLiteCommand(insertFileLine, connection))
              {
                command.Parameters.AddWithValue("@BranchContentId", branchContentId);
                command.Parameters.AddWithValue("@RelativePath", fileLine.RelativePath);
                command.Parameters.AddWithValue("@LineNumber", fileLine.LineNumber);
                command.Parameters.AddWithValue("@LineContent", fileLine.LineContent);
                command.ExecuteNonQuery();
              }
            }

            transaction.Commit();
          }
        }
      }
    }

    /// <summary>
    /// Returns the existing branches on the database
    /// </summary>
    /// <returns>Existing branches</returns>
    internal List<BranchList> GetExistingBranchLists()
    {
      var branchLists = new List<BranchList>();

      using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
      {
        connection.Open();

        string selectBranchList = "SELECT HeadHash, BranchName FROM BranchContent";

        using (var command = new SQLiteCommand(selectBranchList, connection))
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            branchLists.Add(new BranchList
            {
              HeadHash = reader["HeadHash"].ToString(),
              BranchName = reader["BranchName"].ToString()
            });
          }
        }
      }

      return branchLists;
    }

    /// <summary>
    /// Delete a branch in the d
    /// </summary>
    /// <param name="hash"></param>
    private void DeleteBranchContentByHash(string hash)
    {
      using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
      {
        connection.Open();

        using (var transaction = connection.BeginTransaction())
        {
          // Delete from FileLine table
          string deleteFileLines = @"
                    DELETE FROM FileLine 
                    WHERE BranchContentId IN (SELECT Id FROM BranchContent WHERE HeadHash = @HeadHash)";

          using (var command = new SQLiteCommand(deleteFileLines, connection))
          {
            command.Parameters.AddWithValue("@HeadHash", hash);
            command.ExecuteNonQuery();
          }

          // Delete from BranchContent table
          string deleteBranchContent = "DELETE FROM BranchContent WHERE HeadHash = @HeadHash";

          using (var command = new SQLiteCommand(deleteBranchContent, connection))
          {
            command.Parameters.AddWithValue("@HeadHash", hash);
            command.ExecuteNonQuery();
          }

          transaction.Commit();
        }
      }
    }

    /// <summary>
    /// Adds the content of every branch to the database
    /// </summary>
    /// <param name="branchContents"></param>
    internal void UpdateBranchContent(List<BranchContent> branchContents)
    {
      foreach (var branchContent in branchContents)
      {
        // Insert new branch content
        InsertBranchContents(new List<BranchContent> { branchContent });
      }
    }

    /// <summary>
    /// Removes an esixsting branch in the database if the source is of the branches is different
    /// </summary>
    /// <param name="existingBranches"></param>
    /// <param name="newPulledBranches"></param>
    /// <param name="onMatchAction"></param>
    internal void RemoveMatchingBranches(List<BranchList> existingBranches, List<BranchList> newBranches, SQLiteHelper sqliteHelper)
    {
      foreach (var newBranch in newBranches.ToList())
      {
        var existingBranch = existingBranches.FirstOrDefault(b => b.BranchName == newBranch.BranchName);

        if (existingBranch != null)
        {
          if (existingBranch.HeadHash == newBranch.HeadHash)
          {
            // Remove branch from new list if it already exists with the same HeadHash
            newBranches.Remove(newBranch);
          }
          else
          {
            DeleteBranchContentByHash(existingBranch.HeadHash);
            Console.WriteLine($"Deleted existing branch: {existingBranch.BranchName} in Database. Source is different.");
          }
        }
      }
    }
    //
  }
}
