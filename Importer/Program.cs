using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Importer {
	class Program {
		static void Main(string[] args) {
			//ImportEntries();
			//ImportMetadata();
			ImportSpelling();

			Console.Write("All done.");
			Console.ReadLine();
		}

		static void ImportMetadata() {

			SqlConnection conn=new SqlConnection(@"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;");
			conn.Open();

			StreamReader reader=new StreamReader(@"C:\Deleteme\metadata.csv");
			while(reader.Peek() > -1){
				string[] cols=reader.ReadLine().Split('\t');
				if(cols.Length > 2) {
					int id=int.Parse(cols[0]);
					string type=cols[1];
					string json=cols[2].Replace("\"\"", "\"");
					json=Regex.Replace(json, "^\"", "");
					json=Regex.Replace(json, "\"$", "");
					Console.WriteLine(id+", "+type);

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
			conn.Close();
			reader.Close();
			Console.WriteLine("Metadata imported.");
		}

		static void ImportEntries() {

			SqlConnection conn=new SqlConnection(@"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;");
			conn.Open();
			int counter=0;
			StreamReader reader=new StreamReader(@"C:\Deleteme\temp\entries.txt");
			while(reader.Peek() > -1){
				string[] cols=reader.ReadLine().Split('\t');
				if(cols.Length > 1) {
					int id=int.Parse(cols[0]);
					string json=cols[1];
					Console.WriteLine(++counter);

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
			reader.Close();

			conn.Close();
			Console.WriteLine("Entries imported.");
		}

		static void ImportSpelling() {
			string[] abc=new string[]{"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

			SqlConnection conn=new SqlConnection(@"Server=localhost\sqlexpress;Database=tearma;Trusted_Connection=True;");
			conn.Open();
			int counter=0;

			StreamReader reader=new StreamReader(@"C:\Deleteme\temp\spelling.txt");
			while(reader.Peek() > -1){
				string[] cols=reader.ReadLine().Split('\t');
				if(cols.Length > 2) {
					int termID=int.Parse(cols[0]);
					string word=cols[1];
					int length=int.Parse(cols[2]);

					Console.WriteLine(++counter);

					SqlCommand command=new SqlCommand("insert into spelling(term_id, word, [A],[B],[C],[D],[E],[F],[G],[H],[I],[J],[K],[L],[M],[N],[O],[P],[Q],[R],[S],[T],[U],[V],[W],[X],[Y],[Z], [length]) values(@term_id, @word, @a, @b, @c, @d, @e, @f, @g, @h, @i, @j, @k, @l, @m, @n, @o, @p, @q, @r, @s, @t, @u, @v, @w, @x, @y, @z, @length)", conn);
					command.CommandType=CommandType.Text;
					SqlParameter param;

					param=new SqlParameter();
					param.ParameterName="@term_id";
					param.SqlDbType=SqlDbType.Int;
					param.Value=termID;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					param=new SqlParameter();
					param.ParameterName="@word";
					param.SqlDbType=SqlDbType.NVarChar;
					if(word.Length>255) word=word.Substring(0, 255);
					param.Value=word;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					param=new SqlParameter();
					param.ParameterName="@length";
					param.SqlDbType=SqlDbType.Int;
					param.Value=length;
					param.Direction=ParameterDirection.Input;
					command.Parameters.Add(param);

					for(int i=0; i<abc.Length; i++) {
						param=new SqlParameter();
						param.ParameterName="@"+abc[i];
						param.SqlDbType=SqlDbType.Int;
						param.Value=int.Parse(cols[i+3]);
						param.Direction=ParameterDirection.Input;
						command.Parameters.Add(param);
					}

					command.ExecuteNonQuery();
				}
			}
			reader.Close();
			conn.Close();
			Console.WriteLine("Spelling done.");

		}
	}
}
