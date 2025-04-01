namespace UORenderer;

public class Project
{
    public Project(string name)
    {
        // TODO: Name of project loads a file from APPDATA or something
        // the file has all the info in it.
    }

    public string BasePath = @"Z:\Ultima Online Stygian Abyss";

    public string GetFullPath(string fileName)
    {
        return Path.Combine(BasePath, fileName);
    }
}