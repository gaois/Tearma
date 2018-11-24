using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Importer {
	class Program {
		static void Main(string[] args) {
			//ImportEntries();
			//ImportMetadata();

			Console.Write("All done.");
			Console.ReadLine();
		}

		static void ImportMetadata() {

			SqlConnection conn=new SqlConnection(@"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;");
			conn.Open();

			string dirpath=@"C:\MBM\gaois\Tearma\Migration\json\metadata";
			List<string> subdirnames=new List<string>(Directory.GetDirectories(dirpath));
			foreach(string subdirname in subdirnames) {
				string type=Path.GetFileName(subdirname);
				List<string> filenames=new List<string>(Directory.GetFiles(subdirname));
				foreach(string filename in filenames) {
					if(filename.EndsWith(".js")) {
						int id=int.Parse(Path.GetFileNameWithoutExtension(filename));
						string json=File.ReadAllText(Path.Combine(dirpath, filename));
						
					SqlCommand command=new SqlCommand("insert into metadata(id, type, json) values(@id, @type, @json)", conn);
					command.CommandType=CommandType.Text;
					SqlParameter param;

					param=new SqlParameter();
					param.ParameterName="@id";
					param.SqlDbType=SqlDbType.Int;
					param.Value=id;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					param=new SqlParameter();
					param.ParameterName="@type";
					param.SqlDbType=SqlDbType.NVarChar;
					param.Value=type;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					param =new SqlParameter();
					param.ParameterName="@json";
					param.SqlDbType=SqlDbType.NVarChar;
					param.Value=json;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					command.ExecuteNonQuery();
					}
				}
			}

			conn.Close();
			Console.WriteLine("Metadata imported.");
		}

		static void ImportEntries() {

			SqlConnection conn=new SqlConnection(@"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;");
			conn.Open();

			string dirpath=@"C:\MBM\gaois\Tearma\Migration\json\entry";
			List<string> filenames=new List<string>(Directory.GetFiles(dirpath));
			foreach(string filename in filenames) {
				if(filename.EndsWith(".js")) {
					int id=int.Parse(Path.GetFileNameWithoutExtension(filename));
					string json=File.ReadAllText(Path.Combine(dirpath, filename));

					SqlCommand command=new SqlCommand("insert into entries(id, json) values(@id, @json)", conn);
					command.CommandType=CommandType.Text;
					SqlParameter param;

					param=new SqlParameter();
					param.ParameterName="@id";
					param.SqlDbType=SqlDbType.Int;
					param.Value=id;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					param=new SqlParameter();
					param.ParameterName="@json";
					param.SqlDbType=SqlDbType.NVarChar;
					param.Value=json;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					command.ExecuteNonQuery();
				}
			}

			conn.Close();
			Console.WriteLine("Entries imported.");
		}
	}
}
