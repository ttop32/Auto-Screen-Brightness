using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Auto_Screen_Brightness
{
    public class ScheduleEntry
    {
        public int Id { get; set; }
        public TimeSpan Time { get; set; }
        public int Brightness { get; set; }
        public int OverlayBrightness { get; set; }
        public bool IsEnabled { get; set; } = true;

        public override string ToString()
        {
            return $"{Time:hh\\:mm} - {Brightness}% {(IsEnabled ? "" : "(Disabled)")}";

        }
    }

    public class ScheduleManager
    {
        private static ScheduleManager _instance;
        private ObservableCollection<ScheduleEntry> _schedules = new();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _scheduleTask;
        private Action<int, int> _onScheduleTriggered;
        private int _nextId = 1;
        private string _schedulesFilePath;

        public ObservableCollection<ScheduleEntry> Schedules => _schedules;

        public static ScheduleManager Instance
        {
            get { return _instance ??= new ScheduleManager(); }
        }

        private ScheduleManager()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataFolder, "AutoScreenBrightness");
            Directory.CreateDirectory(appFolder);
            _schedulesFilePath = Path.Combine(appFolder, "schedules.json");
        }

        public void Initialize(Action<int, int> onScheduleTriggered)
        {
            _onScheduleTriggered = onScheduleTriggered;
            LoadSchedules();
            StartScheduleMonitoring();
        }

        public void AddSchedule(TimeSpan time, int brightness)
        {
            var entry = new ScheduleEntry
            {
                Id = _nextId++,
                Time = time,
                Brightness = brightness,
                IsEnabled = true
            };
            _schedules.Add(entry);
            SaveSchedules();
        }

        public bool CanAddSchedule(TimeSpan time)
        {
            return !_schedules.Any(s => s.Time == time);
        }

        public void AddScheduleWithOverlay(TimeSpan time, int brightness, int overlayBrightness)
        {
            var entry = new ScheduleEntry
            {
                Id = _nextId++,
                Time = time,
                Brightness = brightness,
                OverlayBrightness = overlayBrightness,
                IsEnabled = true
            };
            _schedules.Add(entry);
            SaveSchedules();
        }

        public void RemoveSchedule(ScheduleEntry entry)
        {
            _schedules.Remove(entry);
            SaveSchedules();
        }

        public void UpdateSchedule(ScheduleEntry entry, TimeSpan time, int brightness)
        {
            entry.Time = time;
            entry.Brightness = brightness;
            SaveSchedules();
        }

        public void ToggleSchedule(ScheduleEntry entry)
        {
            entry.IsEnabled = !entry.IsEnabled;
            SaveSchedules();
        }

        private void SaveSchedules()
        {
            try
            {
                var json = JsonSerializer.Serialize(_schedules.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_schedulesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save schedules: {ex.Message}");
            }
        }

        private void LoadSchedules()
        {
            try
            {
                if (File.Exists(_schedulesFilePath))
                {
                    var json = File.ReadAllText(_schedulesFilePath);
                    var loaded = JsonSerializer.Deserialize<List<ScheduleEntry>>(json);
                    if (loaded != null)
                    {
                        _schedules.Clear();
                        foreach (var entry in loaded)
                        {
                            _schedules.Add(entry);
                            _nextId = Math.Max(_nextId, entry.Id + 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load schedules: {ex.Message}");
            }
        }

        private void StartScheduleMonitoring()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _scheduleTask = Task.Run(() => MonitorSchedules(_cancellationTokenSource.Token));
        }

        private async Task MonitorSchedules(CancellationToken cancellationToken)
        {
            var lastTriggeredTime = DateTime.MinValue;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var currentTime = now.TimeOfDay;

                    var sortedSchedules = _schedules.Where(s => s.IsEnabled).OrderBy(s => s.Time).ToList();

                    foreach (var schedule in sortedSchedules)
                    {
                        // Check if we should trigger this schedule
                        var timeDiff = currentTime - schedule.Time;
                        if (timeDiff.TotalSeconds >= 0 && timeDiff.TotalSeconds < 30 && (now - lastTriggeredTime).TotalSeconds > 60)
                        {
                            _onScheduleTriggered?.Invoke(schedule.Brightness, schedule.OverlayBrightness);
                            lastTriggeredTime = now;
                            break;
                        }
                    }

                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _scheduleTask?.Wait(TimeSpan.FromSeconds(5));
        }
    }
}
