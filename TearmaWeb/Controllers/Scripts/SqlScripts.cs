using System.Reflection;

namespace TearmaWeb.Controllers.Scripts;

public class SqlScripts
{
    public static string Get(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.EndsWith(filename))
                continue;

            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
                continue;

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        throw new NullReferenceException("SQL script not found");
    }
}
