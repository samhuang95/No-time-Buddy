using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Reflection;

namespace No_time_Buddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string dbPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
            LoadMissions();
        }

        private void InitializeDatabase()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbFolder = Path.Combine(baseDirectory, "db");

            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            dbPath = Path.Combine(dbFolder, "missions.db");

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);

                using (var db = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    db.Open();
                    string sql = @"CREATE TABLE Mission (
                                    MissionId INTEGER PRIMARY KEY AUTOINCREMENT,
                                    UserId INTEGER,
                                    MissionTitle TEXT,
                                    MissionDeadline DATETIME,
                                    IsDeleted BOOLEAN,
                                    CreateId INTEGER,
                                    CreateTime DATETIME,
                                    UpdateId INTEGER,
                                    UpdateTime DATETIME)";
                    using (var cmd = new SQLiteCommand(sql, db))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            this.dbPath = dbPath;
        }

        private async void saveButtonClick(object sender, RoutedEventArgs e)
        {
            string missionTitle = this.missionTitle.Text;
            DateTime? missionDeadline = this.CalendarDatePicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(missionTitle) || missionDeadline == null)
            {
                ShowCustomDialog("Failed", "Please enter Mission Title and Deadline.");
                return;
            }

            if (missionDeadline.Value.Date < DateTime.Today)
            {
                ShowCustomDialog("Failed", "Mission Deadline cannot be earlier than today.");
                return;
            }

            var mission = new Mission
            {
                UserId = 123,
                MissionTitle = missionTitle,
                MissionDeadline = missionDeadline.Value,
                IsDeleted = false,
                CreateId = 123,
                CreateTime = DateTime.Now,
                UpdateId = 123,
                UpdateTime = DateTime.Now
            };

            await SaveMission(mission);

            LoadMissions();
        }

        private async Task SaveMission(Mission mission)
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var db = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        db.Open();
                        string sql = @"INSERT INTO Mission (UserId, MissionTitle, MissionDeadline, IsDeleted, CreateId, CreateTime, UpdateId, UpdateTime)
                                       VALUES (@UserId, @MissionTitle, @MissionDeadline, @IsDeleted, @CreateId, @CreateTime, @UpdateId, @UpdateTime)";
                        using (var cmd = new SQLiteCommand(sql, db))
                        {
                            cmd.Parameters.AddWithValue("@UserId", mission.UserId);
                            cmd.Parameters.AddWithValue("@MissionTitle", mission.MissionTitle);
                            cmd.Parameters.AddWithValue("@MissionDeadline", mission.MissionDeadline);
                            cmd.Parameters.AddWithValue("@IsDeleted", mission.IsDeleted);
                            cmd.Parameters.AddWithValue("@CreateId", mission.CreateId);
                            cmd.Parameters.AddWithValue("@CreateTime", mission.CreateTime);
                            cmd.Parameters.AddWithValue("@UpdateId", mission.UpdateId);
                            cmd.Parameters.AddWithValue("@UpdateTime", mission.UpdateTime);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });

                ShowCustomDialog("Success", "Mission saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving mission: {ex.Message}");
                ShowCustomDialog("Failed", "Failed to save mission.");
            }
        }

        private void ShowCustomDialog(string title, string content)
        {
            MessageBox.Show(content, title, MessageBoxButton.OK);
        }

        private async void LoadMissions()
        {
            List<Mission> missions = await Task.Run(() =>
            {
                using (var db = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    db.Open();
                    string sql = @"SELECT * FROM Mission WHERE IsDeleted = 0 ORDER BY MissionDeadline";
                    using (var cmd = new SQLiteCommand(sql, db))
                    {
                        var reader = cmd.ExecuteReader();
                        var result = new List<Mission>();
                        while (reader.Read())
                        {
                            result.Add(new Mission
                            {
                                MissionId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                MissionTitle = reader.GetString(2),
                                MissionDeadline = reader.GetDateTime(3),
                                IsDeleted = reader.GetBoolean(4),
                                CreateId = reader.GetInt32(5),
                                CreateTime = reader.GetDateTime(6),
                                UpdateId = reader.GetInt32(7),
                                UpdateTime = reader.GetDateTime(8)
                            });
                        }
                        return result;
                    }
                }
            });

            MissionListView.ItemsSource = missions;
        }

        private async void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var missionId = button?.Tag as int?;

            if (missionId.HasValue)
            {
                await Task.Run(() =>
                {
                    using (var db = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        db.Open();
                        string sql = "UPDATE Mission SET IsDeleted = 1 WHERE MissionId = @MissionId";
                        using (var cmd = new SQLiteCommand(sql, db))
                        {
                            cmd.Parameters.AddWithValue("@MissionId", missionId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
                LoadMissions();
            }
        }
    }
    public class Mission
    {
        public int MissionId { get; set; }
        public int UserId { get; set; }
        public string MissionTitle { get; set; }
        public DateTime MissionDeadline { get; set; }
        public bool IsDeleted { get; set; }
        public int CreateId { get; set; }
        public DateTime CreateTime { get; set; }
        public int UpdateId { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    public class DaysLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime deadline)
            {
                var daysLeft = (deadline - DateTime.Now).Days;
                return $"{daysLeft} 天";
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}