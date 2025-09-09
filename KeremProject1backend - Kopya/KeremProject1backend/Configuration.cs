using KeremProject1backend.InternalServices;

namespace KeremProject1backend
{

    static public class Configuration
    {
        static ConfigurationManager configurationManager = new();

        static Configuration()
        {
            configurationManager.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), ""));
            configurationManager.AddJsonFile("appsettings.json");
        }
        static public string? ConnectionString(string dbName)
        {
            return configurationManager.GetConnectionString(dbName);
        }
        static public ServiceParameters? GetServiceParameters()
        {
            return configurationManager.GetSection("ServiceParameters").Get<ServiceParameters>();
        }
    }
}
