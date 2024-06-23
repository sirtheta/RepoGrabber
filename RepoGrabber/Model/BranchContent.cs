namespace RepoGrabber.Model
{
  internal class BranchContent
  {
    public string HeadHash { get; set; }
    public string BranchName { get; set; }
    public List<FileLine> Files { get; set; }
    public string ReadmeContent { get; set; }
  }
}
