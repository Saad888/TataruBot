using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace TataruBot
{

    class Program
    {
       
        public static void Main(string[] args){
            var tester = new Test();
            string jsons = JsonConvert.SerializeObject(tester).ToString();
            Console.WriteLine(jsons);
        }
    }

    class Test
    {
        public List<string> lolol {get; set;}

        public Test() {
            lolol = new List<string>();
            lolol.Add("Number 1");
            lolol.Add("Number 2");
            lolol.Add("Number 3");
            lolol.Add("Number 4");
            lolol.Add("Number 5");
        }
        public Test(List<string> input) {
            lolol = input;
        }
    }
} 

