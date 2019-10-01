using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TataruBot {
    class DatabaseManager {
        public MySqlConnection SQLClient;
        public DatabaseManager(string userID, string pwd, string source, string database) {
            string line = $"Data Source={source};Database={database};User ID={userID};Password={pwd};";
            Console.WriteLine(line);
            SQLClient = new MySqlConnection(line);
        } 

        private async Task open_connection() {
            try {await SQLClient.OpenAsync();} 
            catch(Exception e) {Console.WriteLine(e.ToString());}
        }

        private void close_connection() {
            try {SQLClient.Close();}
            catch(Exception e) {e.ToString();} 
        }

        private async Task<List<Dictionary<string, string>>> send_command(string command, 
                                                                          string[] indexes) {
            // Opens connection to SQL database
            await open_connection();

            // Generate and execute command
            MySqlCommand myCommand = new MySqlCommand(command, SQLClient);
            var reader = await myCommand.ExecuteReaderAsync();

            // Generate response format
            var response = new List<Dictionary<string, string>>();
            while (reader.Read()){
                var current_entry = new Dictionary<string, string>();
                foreach(string index in indexes) {
                    current_entry.Add(index, reader[index].ToString());
                }
                response.Add(current_entry);
            }

            // Close connection to SQL database
            close_connection();

            // Return response
            return response;
        }

        public async Task test_connection() {
            // For debugging purposes
            string command = "SELECT * FROM test_table;";
            string[] indexes = new string[]{"ID", "Test"};
            var reader = await send_command(command, indexes);
            foreach (var read in reader){
                foreach (var index in indexes) {
                    Console.WriteLine(read[index]);
                }
            }
        }

        public async Task<List<Dictionary<string, string>>> get_data(
            string table, string[] columns, Dictionary<string, string> param) {
            // Create base command string and add required columms
            string command = $"SELECT {columns[0]}";
            for (int i = 1; i < columns.Length; i++) {
                command += $", {columns[i]}";
            } 

            // Identify table
            command += $" FROM {table} WHERE ";

            // Add conditionals 
            List<string> keys = new List<string>(param.Keys);
            command += $"{keys[0]} = \"{param[keys[0]]}\"";
            for (int i = 1; i < keys.Count; i++) {
                command += $" AND {keys[i]} = \"{param[keys[i]]}\"";
            } 

            // Send command and await result
            return await send_command(command, columns);
 
        }
    }
}
