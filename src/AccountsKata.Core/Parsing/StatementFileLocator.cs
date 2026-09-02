namespace AccountsKata.Core.Parsing;

/// <summary>Finds the sample statement by walking up from the working directory.</summary>
public static class StatementFileLocator
{
    public const string DefaultRelativePath = "inputs/account_20230228.csv";

    public static string Resolve(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, DefaultRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return DefaultRelativePath;
    }
}
