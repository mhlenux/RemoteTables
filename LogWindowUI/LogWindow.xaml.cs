
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LogWindowUI
{
    //public delegate void ClosingEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        private Action<object, EventArgs> onProcessExit;

        /// <param name="onProcessExit">Callback to Program to cleany exit</param>
        public LogWindow(Action<object, EventArgs> onProcessExit)
        {
            this.onProcessExit = onProcessExit;
            InitializeComponent();
            DataContext = this;

            LogEntries = new ObservableCollection<LogEntry>();
            LogLevels = new ObservableCollection<LogLevel>();
            LogSystems = new ObservableCollection<LogSystem>();

            LogEntryList.ItemsSource = LogEntries;
            LogEntries.CollectionChanged += OnLogEntriesChangedScrollToBottom;

            Systems.ItemsSource = LogSystems;
            Levels.ItemsSource = LogLevels;

            FilteredLogEntries = CollectionViewSource.GetDefaultView(LogEntries);
            FilteredLogEntries.Filter = LogEntriesFilterPredicate;

            IsAutoScrollEnabled = true;
            CurrentLogLevelSeverity = 1;

            this.Closing += OnClosingEvent;
        }

        private void OnClosingEvent(object sender, CancelEventArgs e)
        {
            onProcessExit(sender, e);
        }

        private bool LogEntriesFilterPredicate(object item)
        {
            LogEntry entry = item as LogEntry;

            // filter out systems
            if (LogSystems.Any(s => s.Name == entry.System && !s.Enabled))
            {
                return false;
            }

            // filter out levels
            LogLevel level = LogLevels.First(l => l.Name == entry.Level);
            if (level != null && level.Severity < CurrentLogLevelSeverity)
            {
                return false;
            }

            return true;
        }

        private ICollectionView FilteredLogEntries;

        public ObservableCollection<LogEntry> LogEntries { get; private set; }
        public void AddLogEntry(float timestamp, string system, string message, string level)
        {
            LogEntries.Add(new LogEntry
            {
                Timestamp = timestamp,
                System = system,
                Message = message,
                Level = level
            });
        }

        public bool IsAutoScrollEnabled { get; set; }
        private void OnLogEntriesChangedScrollToBottom(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsAutoScrollEnabled)
            {
                return;
            }

            if (VisualTreeHelper.GetChildrenCount(LogEntryList) > 0)
            {
                Decorator border = VisualTreeHelper.GetChild(LogEntryList, 0) as Decorator;
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        public ObservableCollection<LogSystem> LogSystems { get; private set; }
        public void ConfigureSystems(List<string> systems)
        {
            systems.ForEach((system) =>
            {
                LogSystem entry = new LogSystem
                {
                    Name = system,
                    Enabled = true
                };

                entry.PropertyChanged += OnSystemEnableChanged;

                LogSystems.Add(entry);
            });
        }

        private void OnSystemEnableChanged(object sender, PropertyChangedEventArgs args)
        {
            FilteredLogEntries.Refresh();
        }

        public int CurrentLogLevelSeverity { get; set; }
        public ObservableCollection<LogLevel> LogLevels { get; private set; }
        public void ConfigureLevels(List<Tuple<string, string>> levels)
        {
            // create log levels
            for (int i = 0; i < levels.Count; ++i)
            {
                LogLevel entry = new LogLevel
                {
                    Severity = i,
                    Name = levels[i].Item1,
                    Color = (Brush)new BrushConverter().ConvertFromString(levels[i].Item2)
                };

                entry.PropertyChanged += OnLevelSelectedChanged;

                if (CurrentLogLevelSeverity == entry.Severity)
                {
                    entry.Selected = true;
                }

                LogLevels.Add(entry);
            }

            // style ListView based on the data from the log levels
            Style logListStyle = new Style();
            logListStyle.TargetType = typeof(ListViewItem);
            foreach (LogLevel level in LogLevels)
            {
                DataTrigger trigger = new DataTrigger
                {
                    Binding = new Binding("Level"),
                    Value = level.Name
                };
                trigger.Setters.Add(new Setter(ListViewItem.ForegroundProperty, level.Color));

                logListStyle.Triggers.Add(trigger);
            }

            LogEntryList.ItemContainerStyle = logListStyle;
        }
        private void OnLevelSelectedChanged(object sender, PropertyChangedEventArgs args)
        {
            LogLevel level = sender as LogLevel;

            if (level.Selected)
            {
                CurrentLogLevelSeverity = level.Severity;

                FilteredLogEntries.Refresh();
            }
        }
    }
}
