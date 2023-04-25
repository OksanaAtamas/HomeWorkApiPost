using System.Text.Json;
using System.Data.SQLite;
using System.Diagnostics;
using System.Reflection;
using static System.IO.File;
using System.Diagnostics.Metrics;
using System.Text;

namespace testAsync2;
public class Root
{
    public string Title { get; set; }
    public string VideoID { get; set; }


    public Root(string Title, string VideoID)
    {
        this.Title = Title;
        this.VideoID = VideoID;

    }
}
class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static string url = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults=10&playlistId=PLSN-wzQDGl9Bi3W4LBSz3dqhmCc9cZHvI&key=AIzaSyDzAJFf6TiJH5ut8RhJehZaqsNBrO2gEO8";

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!1");
        getNames();
        getFromDBAndSend();
        GetFromDB();
        WriteToFile();
        using (var conn = CreateConnection())
        {
            var roots = ReadData(conn);
            foreach (var root in roots)
            {
                Console.WriteLine($"{root.Title} {root.VideoID}");
            }
        }
        Console.WriteLine("Hello, World!2");
        Console.ReadKey();
       
    }


    public static void GetFromDB()
    {
        SQLiteConnection sqlite_conn;
        sqlite_conn = CreateConnection();
        ReadData(sqlite_conn);

    }
    public static void getFromDBAndSend()
    {
        SQLiteConnection sqlite_conn;
        sqlite_conn = CreateConnection();
        List<Root> items = ReadData(sqlite_conn);
        string json = JsonSerializer.Serialize(items);
        sendNames(json);
    }
    public static async void sendNames(string json)
    {
        String response = "";
        await Task.Run(() =>
        {
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            response = client.PostAsync(url, content).Result.ToString();

        });
        Console.WriteLine(response);
    }

    static List<Root> ReadData(SQLiteConnection conn)
    {
        List<Root> list = new List<Root>();
        SQLiteDataReader sqlite_datareader;
        SQLiteCommand sqlite_cmd;
        sqlite_cmd = conn.CreateCommand();
        sqlite_cmd.CommandText = "SELECT * FROM HomeWork";
        sqlite_datareader = sqlite_cmd.ExecuteReader();
        while (sqlite_datareader.Read())
        {
            string Title = sqlite_datareader.GetString(0);
            string VideoId = sqlite_datareader.GetString(1);
          
            Root root = new Root(Title, VideoId);
            list.Add(root);
        }
        conn.Close();
        return list;
    }
    static SQLiteConnection CreateConnection()
    {

        SQLiteConnection sqlite_conn;
        sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; New = True; Compress = True; ");
        try
        {
            sqlite_conn.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return sqlite_conn;
    }
    public static void SaveToDB(Root root)
    {
        SQLiteConnection sqlite_conn;
        sqlite_conn = CreateConnection();
        CreateTable(sqlite_conn); 
        InsertData(sqlite_conn, root);
        WriteToFile();

    }

    static void CreateTable(SQLiteConnection conn)
    {

        SQLiteCommand sqlite_cmd;
    string Createsql = "CREATE TABLE IF NOT EXISTS HomeWork (Title VARCHAR(20), VideoId VARCHAR(20) PRIMARY KEY ON CONFLICT REPLACE)";

        sqlite_cmd = conn.CreateCommand();
        sqlite_cmd.CommandText = Createsql;
        sqlite_cmd.ExecuteNonQuery();

    }

    static void InsertData(SQLiteConnection conn, Root root)
    {
        SQLiteCommand sqlite_cmd;
        sqlite_cmd = conn.CreateCommand();
        sqlite_cmd.CommandText = $"INSERT INTO HomeWork (Title, VideoId)  VALUES('{root.Title}', '{root.VideoID}');";

        sqlite_cmd.ExecuteNonQuery();

    }


    static void WriteToFile()
    {
        SQLiteConnection sqlite_conn;
        sqlite_conn = CreateConnection();
        SQLiteDataReader sqlite_datareader;
        SQLiteCommand sqlite_cmd;
        sqlite_cmd = sqlite_conn.CreateCommand();
        sqlite_cmd.CommandText = "SELECT * FROM HomeWork";
        sqlite_datareader = sqlite_cmd.ExecuteReader();

        string output = "";
        while (sqlite_datareader.Read())
        {
            string Title = sqlite_datareader.GetString(0);
            string VideoId = sqlite_datareader.GetString(1);


            output += $"{Title} {VideoId}\n";
        }
        sqlite_conn.Close();

        File.WriteAllText("HomeWork.txt", output);
    }
    public static async void getPost(string json)
    {
        String response = "";
        await Task.Run(() =>
        {

            response = client.GetStringAsync(url).Result.ToString();
        });
        Console.WriteLine(response);


    }


    //public static async void getNames()
    //{
    //    String response = "";
    //    await Task.Run(() =>
    //    {
    //        response = client.GetStringAsync(url).Result.ToString();
    //    });
    //    Console.WriteLine(response);
    //    Root? root =
    //          JsonSerializer.Deserialize<Root>(response);
    //    Console.WriteLine(root.Title);
    //    SaveToDB(root);
    //}
    public static async void getNames()
    {
        String response = "";
        await Task.Run(() =>
        {
            response = client.GetStringAsync(url).Result.ToString();
        });
        Console.WriteLine(response);

        JsonDocument jsonDoc = JsonDocument.Parse(response);
        JsonElement rootElement = jsonDoc.RootElement;
        JsonElement items = rootElement.GetProperty("items");

        foreach (JsonElement item in items.EnumerateArray())
        {
            string title = item.GetProperty("snippet").GetProperty("title").GetString();
            string videoId = item.GetProperty("snippet").GetProperty("resourceId").GetProperty("videoId").GetString();

            Root root = new Root(title, videoId);
            Console.WriteLine(root.Title);
            
            SaveToDB(root);
        }
    }
}
