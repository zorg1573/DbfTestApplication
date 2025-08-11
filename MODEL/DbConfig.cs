using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestApp.DAL;

namespace TestApp.MODEL
{
    public class DbConfig
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        public string GetConnection()
        {
            return $"Server={Server};Database={Database};User Id={UserId};Password={Password};";
        }
        public string GetConnectionString()
        {
            DbConfig config = DbConfigHelper.LoadConfig();
            return $"Server={config.Server};Database={config.Database};User Id={config.UserId};Password={config.Password};";
        }
    }

}
