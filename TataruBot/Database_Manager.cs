using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TataruBot {
    class DatabaseManager {
        public SqlConnection SQLClient;
        public DatabaseManager(string userID, string pwd, string source, string database) {
            SQLClient = new SqlConnection($"Data Source={source};User ID={userID};Password={pwd};" +
                                          $"Initial Catalog=XaadDatabase;" +
                                          "connection timeout=15;");
        }

        private async Task open_connection() {
            Console.WriteLine("Open Connection");
            try {await SQLClient.OpenAsync();} 
            catch(Exception e) {Console.WriteLine(e.ToString());}
            Console.WriteLine("Opened Connection");
        }

        private void close_connection() {
            try {SQLClient.Close();}
            catch(Exception e) {e.ToString();} 
        }

        private async Task<SqlDataReader> send_command(string command) {
            await open_connection();
            SqlCommand myCommand = new SqlCommand(command, SQLClient);
            var reader = await myCommand.ExecuteReaderAsync();
            close_connection();
            return reader;
        }

        public async Task test_connection() {
            string command = "SELECT * FROM TataruBot.Test_Table;";
            var reader = await send_command(command);
            while (reader.Read()){
                Console.WriteLine(reader["ID"].ToString());
                Console.WriteLine(reader["Test"].ToString());
            }
        }
    }
}
