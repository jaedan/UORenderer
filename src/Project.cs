namespace UORenderer;

public class Project
{
    public Project(string name)
    {
        // TODO: Name of project loads a file from APPDATA or something
        // the file has all the info in it.
    }

    public string GetFullPath(string fileName)
    {
        return Path.Combine(@"C:\Program Files (x86)\Ultima Online", fileName);
    }
}