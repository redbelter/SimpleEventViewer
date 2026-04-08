using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace EventLogReader
{
    public class MainForm : Form
    {
        private ComboBox _logNameCombo = new();
        private ListView _eventsListView = new();
        private TableLayoutPanel _filterPanel = new();

        private ComboBox _levelFilterCombo = new();
        private ComboBox _userFilterCombo = new();
        private ComboBox _providerFilterCombo = new();
        private DateTimePicker _startTimePicker = new();
        private DateTimePicker _endTimePicker = new();

        public MainForm()
        {
            InitializeComponent();
            LoadLogNames();
            _logNameCombo_SelectedIndexChanged(null, null);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            _filterPanel.AutoSize = true;
            _filterPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _logNameCombo = new ComboBox();
            _logNameCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _logNameCombo.Size = new Size(150, 25);
            _logNameCombo.SelectedIndexChanged += _logNameCombo_SelectedIndexChanged;

            Label levelLabel = new Label();
            levelLabel.Text = "Level:";
            levelLabel.AutoSize = true;

            _levelFilterCombo = new ComboBox();
            _levelFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _levelFilterCombo.Size = new Size(100, 25);
            _levelFilterCombo.Items.AddRange(new object[] { "All", "Error", "Warning", "Information" });
            _levelFilterCombo.SelectedIndex = 0;
            _levelFilterCombo.SelectedIndexChanged += Filter_Changed;

            Label userLabel = new Label();
            userLabel.Text = "User:";
            userLabel.AutoSize = true;

            _userFilterCombo = new ComboBox();
            _userFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _userFilterCombo.Size = new Size(120, 25);
            _userFilterCombo.Items.Add("All");
            _userFilterCombo.SelectedIndexChanged += Filter_Changed;

            Label providerLabel = new Label();
            providerLabel.Text = "Provider:";
            providerLabel.AutoSize = true;

            _providerFilterCombo = new ComboBox();
            _providerFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _providerFilterCombo.Size = new Size(120, 25);
            _providerFilterCombo.Items.Add("All");
            _providerFilterCombo.SelectedIndexChanged += Filter_Changed;

            Label timeLabel = new Label();
            timeLabel.Text = "Time Range:";
            timeLabel.AutoSize = true;

            _startTimePicker = new DateTimePicker();
            _startTimePicker.Size = new Size(130, 25);
            _startTimePicker.Value = DateTime.Now.AddDays(-7);
            _startTimePicker.ValueChanged += Filter_Changed;

            _endTimePicker = new DateTimePicker();
            _endTimePicker.Size = new Size(130, 25);
            _endTimePicker.Value = DateTime.Now;
            _endTimePicker.ValueChanged += Filter_Changed;

            _filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _filterPanel.RowStyles.Add(new RowStyle());
            _filterPanel.RowStyles.Add(new RowStyle());

            _filterPanel.Controls.Add(_logNameCombo);
            _filterPanel.SetColumn(_logNameCombo, 0);
            _filterPanel.SetRow(_logNameCombo, 0);

            _filterPanel.Controls.Add(levelLabel);
            _filterPanel.SetColumn(levelLabel, 1);
            _filterPanel.SetRow(levelLabel, 0);

            _filterPanel.Controls.Add(_levelFilterCombo);
            _filterPanel.SetColumn(_levelFilterCombo, 2);
            _filterPanel.SetRow(_levelFilterCombo, 0);

            _filterPanel.Controls.Add(userLabel);
            _filterPanel.SetColumn(userLabel, 3);
            _filterPanel.SetRow(userLabel, 0);

            _filterPanel.Controls.Add(_userFilterCombo);
            _filterPanel.SetColumn(_userFilterCombo, 4);
            _filterPanel.SetRow(_userFilterCombo, 0);

            _filterPanel.Controls.Add(providerLabel);
            _filterPanel.SetColumn(providerLabel, 5);
            _filterPanel.SetRow(providerLabel, 0);

            _filterPanel.Controls.Add(_providerFilterCombo);
            _filterPanel.SetColumn(_providerFilterCombo, 6);
            _filterPanel.SetRow(_providerFilterCombo, 0);

            _filterPanel.Controls.Add(timeLabel);
            _filterPanel.SetColumn(timeLabel, 7);
            _filterPanel.SetRow(timeLabel, 0);

            _filterPanel.Controls.Add(_startTimePicker);
            _filterPanel.SetColumn(_startTimePicker, 8);
            _filterPanel.SetRow(_startTimePicker, 0);

            _filterPanel.Controls.Add(_endTimePicker);
            _filterPanel.SetColumn(_endTimePicker, 9);
            _filterPanel.SetRow(_endTimePicker, 0);

            _eventsListView = new ListView();
            _eventsListView.Dock = DockStyle.Fill;
            _eventsListView.View = View.Details;
            _eventsListView.FullRowSelect = true;
            _eventsListView.GridLines = true;
            _eventsListView.VirtualMode = true;
            _eventsListView.RetrieveVirtualItem += ListView_RetrieveVirtualItem;
            _eventsListView.CacheVirtualItems += ListView_CacheVirtualItems;

            Controls.Add(_filterPanel);
            Controls.Add(_eventsListView);

            ClientSize = new Size(1000, 500);
            Text = "Event Log Viewer";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            Resize += MainForm_Resize;
            Shown += MainForm_Shown;

            ResumeLayout(false);
            PerformLayout();
            
            InitializeColumns();
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            AdjustColumnWidths();
        }

        private void ListView_CacheVirtualItems(object? sender, CacheVirtualItemsEventArgs e)
        {
        }

        private int[] _filteredIndices = new int[0];

        private void ListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex >= 0 && e.ItemIndex < _filteredIndices.Length)
            {
                int entryIndex = _filteredIndices[e.ItemIndex];
                EventLogEntry entry = _allEntries[entryIndex];

                ListViewItem item = new ListViewItem();
                item.Text = entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss");
                item.SubItems.Add(GetLevelText(entry.EntryType));
                
                string userName = GetUserName(entry);
                if (!_userFilterCombo.Items.Contains(userName))
                    _userFilterCombo.Items.Add(userName);
                item.SubItems.Add(userName);

                string providerName = entry.Source;
                if (!_providerFilterCombo.Items.Contains(providerName))
                    _providerFilterCombo.Items.Add(providerName);
                item.SubItems.Add(providerName);

                item.SubItems.Add(entry.InstanceId.ToString());
                item.SubItems.Add(entry.Message);

                switch (entry.EntryType)
                {
                    case EventLogEntryType.Error:
                        item.BackColor = Color.LightPink;
                        break;
                    case EventLogEntryType.Warning:
                        item.BackColor = Color.LightYellow;
                        break;
                    case EventLogEntryType.Information:
                        item.BackColor = Color.LightBlue;
                        break;
                }

                e.Item = item;
            }
        }

        private void InitializeColumns()
        {
            _eventsListView.Columns.Add("Time", 130);
            _eventsListView.Columns.Add("Type", 70);
            _eventsListView.Columns.Add("User", 120);
            _eventsListView.Columns.Add("Provider", 120);
            _eventsListView.Columns.Add("Event ID", 70);
            _eventsListView.Columns.Add("Message", 360);
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (Width > 800)
                _filterPanel.Width = Width - 24;
            if (Height > 500)
                _eventsListView.Height = Height - _filterPanel.Height - 32;
            
            AdjustColumnWidths();
        }

        private void LoadLogNames()
        {
            try
            {
                var logs = EventLog.GetEventLogs();
                foreach (var log in logs)
                {
                    _logNameCombo.Items.Add(log.Log);
                }
            }
            catch { }

            if (_logNameCombo.Items.Count > 0)
            {
                _logNameCombo.SelectedIndex = 0;
            }
        }

        private void Filter_Changed(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private List<EventLogEntry> _allEntries = new();

        private void RefreshEvents(string logName)
        {
            _userFilterCombo.Items.Clear();
            _providerFilterCombo.Items.Clear();
            _userFilterCombo.Items.Add("All");
            _providerFilterCombo.Items.Add("All");

            _allEntries.Clear();

            try
            {
                var logs = EventLog.GetEventLogs();
                var eventLog = logs.FirstOrDefault(l => l.Log == logName);
                if (eventLog != null)
                {
                    foreach (EventLogEntry entry in eventLog.Entries)
                    {
                        _allEntries.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ApplyFilters();
        }

        private void AdjustColumnWidths()
        {
            int availableWidth = _eventsListView.ClientSize.Width - 25;
            
            if (_eventsListView.Columns.Count >= 6)
            {
                _eventsListView.Columns[0].Width = (int)(availableWidth * 0.18);
                _eventsListView.Columns[1].Width = (int)(availableWidth * 0.10);
                _eventsListView.Columns[2].Width = (int)(availableWidth * 0.17);
                _eventsListView.Columns[3].Width = (int)(availableWidth * 0.17);
                _eventsListView.Columns[4].Width = (int)(availableWidth * 0.09);
                _eventsListView.Columns[5].Width = availableWidth - (int)(availableWidth * 0.18) - (int)(availableWidth * 0.10) - 
                    (int)(availableWidth * 0.17) - (int)(availableWidth * 0.17) - (int)(availableWidth * 0.09);
            }
        }

        private void ApplyFilters()
        {
            string levelFilter = _levelFilterCombo.Text;
            string userFilter = _userFilterCombo.Text;
            string providerFilter = _providerFilterCombo.Text;
            DateTime startTime = _startTimePicker.Value;
            DateTime endTime = _endTimePicker.Value;

            var filteredList = new List<int>();
            for (int i = 0; i < _allEntries.Count; i++)
            {
                if (MatchesFilters(_allEntries[i], levelFilter, userFilter, providerFilter, startTime, endTime))
                    filteredList.Add(i);
            }
            _filteredIndices = filteredList.ToArray();

            _eventsListView.VirtualListSize = _filteredIndices.Length;
        }

        private bool MatchesFilters(EventLogEntry entry, string levelFilter, string userFilter, string providerFilter, DateTime startTime, DateTime endTime)
        {
            if (!string.IsNullOrEmpty(levelFilter) && levelFilter != "All")
            {
                switch (levelFilter)
                {
                    case "Error":
                        if (entry.EntryType != EventLogEntryType.Error) return false;
                        break;
                    case "Warning":
                        if (entry.EntryType != EventLogEntryType.Warning) return false;
                        break;
                    case "Information":
                        if (entry.EntryType != EventLogEntryType.Information) return false;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(userFilter) && userFilter != "All")
            {
                string entryUser = GetUserName(entry);
                if (!entryUser.Contains(userFilter, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (!string.IsNullOrEmpty(providerFilter) && providerFilter != "All")
            {
                if (!entry.Source.Contains(providerFilter, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (entry.TimeGenerated < startTime || entry.TimeGenerated > endTime)
                return false;

            return true;
        }

        private string GetUserName(EventLogEntry entry)
        {
            try
            {
                if (!string.IsNullOrEmpty(entry.UserName))
                    return entry.UserName;
            }
            catch { }

            return "N/A";
        }

        private string GetLevelText(EventLogEntryType type)
        {
            return type switch
            {
                EventLogEntryType.Error => "Error",
                EventLogEntryType.Warning => "Warning",
                EventLogEntryType.Information => "Info",
                EventLogEntryType.SuccessAudit => "Success",
                EventLogEntryType.FailureAudit => "Failure",
                _ => "Unknown"
            };
        }

        private void _logNameCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            RefreshEvents(_logNameCombo.SelectedItem?.ToString() ?? "Application");
        }
    }
}
