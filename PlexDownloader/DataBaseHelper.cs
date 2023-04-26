using PlexDownloader.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader
{
    public static class DataBaseHelper
    {
        private static SQLiteConnection _sqlite_conn;
        public static void Initialize()
        {
            if (_sqlite_conn == null)
            {
                _sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; Compress = True; ");
                _sqlite_conn.Open();
                DropTables();
                CreateTable();
            }
        }
        static void DropTables()
        {
            string dropTables = "";
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            using var reader = sqlite_cmd.ExecuteReader();
            while (reader.Read())
                if (reader.GetString(0) != "sqlite_sequence")
                    dropTables += "DROP TABLE IF EXISTS " + reader.GetString(0) + ";";
            using SQLiteCommand sqlite_cmd2 = _sqlite_conn.CreateCommand();
            sqlite_cmd2.CommandText = dropTables;
            sqlite_cmd2.ExecuteNonQuery();
        }
        static void CreateTable()
        {
            string CreateSources = @"CREATE TABLE IF NOT EXISTS Sources (
   ID INTEGER PRIMARY KEY AUTOINCREMENT,
  YouTubeURL  VARCHAR(512),
  SourceType INTEGER,
  LastScanned DATETIME,
TotalNumberOfVideos Integer,
NumberOfPendingVideos Integer,
NumberOfDownloadedVideos Integer,
Name varchar(512)
); ";
            string CreateWanted = @"CREATE TABLE IF NOT EXISTS Wanteds (
  ID varchar(512) ,
  VideoTitle VARCHAR(512),
  ChannelName VARCHAR(512),
  PlaylistName VARCHAR(512),
  IndexInPlaylist INTEGER,
  SourceID INTEGER,
  PRIMARY KEY (SourceID, ID),
    UNIQUE (SourceID, ID)
);";
            string CreateDetected = @"
CREATE TABLE IF NOT EXISTS Detecteds (
  SourceID INTEGER,
  ID varchar(512),
  VideoTitle varchar(512),
  ChannelName varchar(512),
  PlaylistName varchar(512),
  IndexInPlaylist INTEGER,
  PRIMARY KEY (SourceID, ID),
    UNIQUE (SourceID, ID)
)";
            string CreateDownloaded = @"CREATE TABLE IF NOT EXISTS Downloadeds (
  ID INTEGER ,
  VideoTitle VARCHAR(512),
  ChannelName VARCHAR(512),
  PlaylistName VARCHAR(512),
  IndexInPlaylist INTEGER,
  FilePath VARCHAR(512),
  SourceID INTEGER,
  PRIMARY KEY (SourceID, ID),
    UNIQUE (SourceID, ID)
);";
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = CreateSources;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = CreateWanted;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = CreateDownloaded;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = CreateDetected;
            sqlite_cmd.ExecuteNonQuery();

        }

        internal static List<Wanted> GetAllWanted()
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"Select * from Wanteds";
            using var reader = sqlite_cmd.ExecuteReader();
            DataReaderMapper<Wanted> mapper = new DataReaderMapper<Wanted>();
            return mapper.MapObjectsFromReader(reader);
        }

        internal static void AddWanteds(Detected[] toDownload)
        {
            using var command = new SQLiteCommand(_sqlite_conn);
            // Define the SQL statement with parameters
            command.CommandText = "INSERT INTO wanteds (ID, SourceID, VideoTitle, ChannelName, PlaylistName, IndexInPlaylist) VALUES (@ID, @SourceID, @VideoTitle, @ChannelName, @PlaylistName, @IndexInPlaylist)";

            // Add parameters for each field
            command.Parameters.Add("@ID", System.Data.DbType.String);
            command.Parameters.Add("@SourceID", System.Data.DbType.Int64);
            command.Parameters.Add("@VideoTitle", System.Data.DbType.String);
            command.Parameters.Add("@ChannelName", System.Data.DbType.String);
            command.Parameters.Add("@PlaylistName", System.Data.DbType.String);
            command.Parameters.Add("@IndexInPlaylist", System.Data.DbType.Int64);
            // Iterate through the detectedItems array and execute the command for each item
            foreach (var item in toDownload)
            {
                try
                {
                    command.Parameters["@ID"].Value = item.ID;
                    command.Parameters["@SourceID"].Value = item.SourceID;
                    command.Parameters["@VideoTitle"].Value = item.VideoTitle;
                    command.Parameters["@ChannelName"].Value = item.ChannelName;
                    command.Parameters["@PlaylistName"].Value = item.PlaylistName;
                    command.Parameters["@IndexInPlaylist"].Value = item.IndexInPlaylist;

                    // Execute the command
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }
            }
        }

        internal static int GetWantedCountBySourceID(long id)
        {
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM Wanteds WHERE SourceID = @SourceID", _sqlite_conn);
            command.Parameters.AddWithValue("@SourceID", id);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        internal static int GetDownloadedCountBySourceID(long id)
        {
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM Downloadeds WHERE SourceID = @SourceID", _sqlite_conn);
            command.Parameters.AddWithValue("@SourceID", id);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public static void UpdateDetectedForSource(long sourceId, Detected[] detected)
        {
            using var command = new SQLiteCommand(_sqlite_conn);

            // Delete rows with matching ID parameter
            command.CommandText = "DELETE FROM detecteds WHERE SourceID = @id";
            command.Parameters.AddWithValue("@id", sourceId);
            command.ExecuteNonQuery();
            var sql = @"
    INSERT INTO Detecteds (SourceID, ID, VideoTitle, ChannelName, PlaylistName, IndexInPlaylist)
    VALUES (@sourceID, @ID, @videoTitle, @channelName, @playlistName, @indexInPlaylist)
";
            using SQLiteCommand cmd = new SQLiteCommand(sql, _sqlite_conn);
            // Use a foreach loop to add each item as a parameter
            foreach (var item in detected)
            {
                cmd.Parameters.Clear(); // Clear parameters from previous iteration

                cmd.Parameters.Add("@sourceID", DbType.Int64).Value = item.SourceID;
                cmd.Parameters.Add("@ID", DbType.String).Value = item.ID;
                cmd.Parameters.Add("@videoTitle", DbType.String).Value = item.VideoTitle;
                cmd.Parameters.Add("@channelName", DbType.String).Value = item.ChannelName;
                cmd.Parameters.Add("@playlistName", DbType.String).Value = item.PlaylistName;
                cmd.Parameters.Add("@indexInPlaylist", DbType.Int64).Value = item.IndexInPlaylist;

                // Execute the query for each item
                cmd.ExecuteNonQuery();
            }
        }
        internal static Detected[] GetUndownloadedIds(long sourceID)
        {
            string query2 = @"SELECT *
FROM detecteds
WHERE detecteds.SourceID=" + sourceID + @" and NOT EXISTS (
  SELECT 1
  FROM wanteds
  WHERE wanteds.ID = detecteds.ID and detecteds.SourceID= wanteds.ID
) and NOT EXISTS (
  SELECT 1
  FROM Downloadeds
  WHERE downloadeds.ID = detecteds.ID and downloadeds.SourceID= detecteds.SourceID
)";
            using var command = new SQLiteCommand(_sqlite_conn);
            command.CommandText = query2;
            using var reader = command.ExecuteReader();
            DataReaderMapper<Detected> mapper = new DataReaderMapper<Detected>();
            List<Detected> toDownload = mapper.MapObjectsFromReader(reader);
            return toDownload.ToArray();
        }

        internal static Source GetSourceByID(long sourceID)
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"Select * from Sources where ID=@ID";
            sqlite_cmd.Parameters.AddWithValue("ID", sourceID);
            using var reader = sqlite_cmd.ExecuteReader();
            DataReaderMapper<Source> mapper = new DataReaderMapper<Source>();
            return mapper.MapObjectFromReader(reader);
        }

        public static List<Source> GetAllSources()
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"Select * from Sources";
            using var reader = sqlite_cmd.ExecuteReader();
            DataReaderMapper<Source> mapper = new DataReaderMapper<Source>();
            return mapper.MapObjectsFromReader(reader);
        }
        public static List<string> GetAllSourcesURLS()
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"Select YouTubeURL from Sources";
            using var reader = sqlite_cmd.ExecuteReader();
            List<string> urls = new List<string>();
            while (reader.Read())
            {
                urls.Add(reader.GetString(0));
            }
            return urls;
        }

        internal static void AddDownloaded(Wanted underlyingVideo, string downloadedPath)
        {
            using var command = new SQLiteCommand(_sqlite_conn);
            command.CommandText = @"INSERT INTO wanteds (ID, SourceID, VideoTitle, ChannelName, PlaylistName, IndexInPlaylist, FilePath)
                        VALUES (@ID, @SourceID, @VideoTitle, @ChannelName, @PlaylistName, @IndexInPlaylist, @FilePath)";

            // Bind the parameter values
            command.Parameters.AddWithValue("@ID", underlyingVideo.ID);
            command.Parameters.AddWithValue("@SourceID", underlyingVideo.SourceID);
            command.Parameters.AddWithValue("@VideoTitle", underlyingVideo.VideoTitle);
            command.Parameters.AddWithValue("@ChannelName", underlyingVideo.ChannelName);
            command.Parameters.AddWithValue("@PlaylistName", underlyingVideo.PlaylistName);
            command.Parameters.AddWithValue("@IndexInPlaylist", underlyingVideo.IndexInPlaylist);
            command.Parameters.AddWithValue("@FilePath", downloadedPath);

            command.ExecuteNonQuery();
        }

        internal static void DeleteWanted(Wanted underlyingVideo)
        {
            using var command = new SQLiteCommand(_sqlite_conn);

            // Delete rows with matching ID parameter
            command.CommandText = "DELETE FROM wanteds WHERE ID = @id and SourceID=@sid";
            command.Parameters.AddWithValue("@id", underlyingVideo.ID);
            command.Parameters.AddWithValue("@sid", underlyingVideo.SourceID);
            command.ExecuteNonQuery();
        }

        public static int AddSource(Source source)
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = @"INSERT INTO Sources (YouTubeURL, SourceType, LastScanned, TotalNumberOfVideos, NumberOfPendingVideos, NumberOfDownloadedVideos,Name)
                                                    VALUES (@youtube_url, @source_type, @last_scanned, @total_videos, @pending_videos, @downloaded_videos,@name);";
            sqlite_cmd.Parameters.AddWithValue("@youtube_url", source.YouTubeURL);
            sqlite_cmd.Parameters.AddWithValue("@source_type", source.SourceType);
            sqlite_cmd.Parameters.AddWithValue("@last_scanned", source.LastScanned);
            sqlite_cmd.Parameters.AddWithValue("@total_videos", source.TotalNumberOfVideos);
            sqlite_cmd.Parameters.AddWithValue("@pending_videos", source.NumberOfPendingVideos);
            sqlite_cmd.Parameters.AddWithValue("@downloaded_videos", source.NumberOfDownloadedVideos);
            sqlite_cmd.Parameters.AddWithValue("@name", source.Name);
            sqlite_cmd.ExecuteNonQuery();
            return (int)_sqlite_conn.LastInsertRowId;
        }
        public static void UpdateSource(Source source)
        {
            using SQLiteCommand sqlite_cmd = _sqlite_conn.CreateCommand();
            // Use a multiline string for the SQL statement
            sqlite_cmd.CommandText = @"
            UPDATE Sources
            SET YouTubeURL = @youtube_url,
                SourceType = @source_type,
                LastScanned = @last_scanned,
                TotalNumberOfVideos = @total_videos,
                NumberOfPendingVideos = @pending_videos,
                NumberOfDownloadedVideos = @downloaded_videos
            WHERE ID = @id";

            // Define named parameters and their values
            sqlite_cmd.Parameters.AddWithValue("@youtube_url", source.YouTubeURL);
            sqlite_cmd.Parameters.AddWithValue("@source_type", source.SourceType);
            sqlite_cmd.Parameters.AddWithValue("@last_scanned", source.LastScanned);
            sqlite_cmd.Parameters.AddWithValue("@total_videos", source.TotalNumberOfVideos);
            sqlite_cmd.Parameters.AddWithValue("@pending_videos", source.NumberOfPendingVideos);
            sqlite_cmd.Parameters.AddWithValue("@downloaded_videos", source.NumberOfDownloadedVideos);
            sqlite_cmd.Parameters.AddWithValue("@id", source.ID); // ID of the row to update

            sqlite_cmd.ExecuteScalar();
        }
        public static List<Source> GetSourcesThatNeedToBeRescanned(int minutesSinceLastScan)
        {
            // Get the current date and time, and subtract some time from it
            DateTime currentDate = DateTime.Now;
            DateTime pastDate = currentDate.AddMinutes(-1 * minutesSinceLastScan);
            using SQLiteCommand command = new SQLiteCommand(_sqlite_conn);
            // Use a multiline string for the SQL statement
            command.CommandText = @"
            SELECT *
            FROM Sources
            WHERE LastScanned <= @past_date";

            // Define named parameters and their values
            command.Parameters.AddWithValue("@past_date", pastDate);

            // Execute the SELECT command and get the results
            using SQLiteDataReader reader = command.ExecuteReader();
            DataReaderMapper<Source> mapper = new DataReaderMapper<Source>();
            return mapper.MapObjectsFromReader(reader);
        }
    }
}
