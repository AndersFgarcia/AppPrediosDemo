using System;
using System.IO;
using System.Configuration;

namespace AppPrediosDemo.Services
{
    public static class ConnectionStringService
    {
        private const string ConnectionStringFileName = "connectionstring.txt";

        public static string? GetConnectionString()
        {
            string? connectionString = null;

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnectionStringFileName);
            
            if (File.Exists(filePath))
            {
                try
                {
                    connectionString = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrWhiteSpace(connectionString))
                        return connectionString;
                }
                catch
                {
                }
            }

            try
            {
                var cs = ConfigurationManager.ConnectionStrings["ViabilidadJuridica"]?.ConnectionString;
                if (!string.IsNullOrWhiteSpace(cs))
                    return cs;
            }
            catch
            {
            }

            return null;
        }

        public static void SaveConnectionString(string connectionString)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnectionStringFileName);
                File.WriteAllText(filePath, connectionString);
            }
            catch
            {
            }
        }

        public static bool TestConnection(string? connectionString = null)
        {
            try
            {
                using var ctx = new Models.ViabilidadContext();
                return ctx.Database.CanConnect();
            }
            catch
            {
                return false;
            }
        }
    }
}

