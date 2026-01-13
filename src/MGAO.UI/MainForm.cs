using MGAO.Core.Interfaces;
using MGAO.Core.Services;
using MGAO.Google;
using MGAO.Outlook;

namespace MGAO.UI;

public partial class MainForm : Form
{
    private readonly ITokenStore _tokenStore;
    private readonly GoogleAuthService? _authService;
    private readonly GoogleCalendarClient? _googleClient;
    private readonly OutlookCalendarBridge _outlookBridge;
    private readonly StateStore _stateStore;
    private ISyncEngine? _syncEngine;

    private TabControl _tabs = null!;
    private ListView _accountsList = null!;
    private ListView _statusList = null!;
    private DataGridView _logsGrid = null!;
    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ProgressBar _progressBar = null!;

    public MainForm()
    {
        _tokenStore = new DpapiTokenStore();
        _stateStore = new StateStore();
        _outlookBridge = new OutlookCalendarBridge();

        var clientId = Environment.GetEnvironmentVariable("MGAO_CLIENT_ID") ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("MGAO_CLIENT_SECRET") ?? "";

        if (!string.IsNullOrEmpty(clientId))
        {
            _authService = new GoogleAuthService(clientId, clientSecret, _tokenStore);
            _googleClient = new GoogleCalendarClient(_authService);
            _syncEngine = new SyncEngine(_googleClient, _outlookBridge, _stateStore);
        }

        InitializeComponents();
        LoadAccounts();
    }

    private void InitializeComponents()
    {
        Text = "MGAO - Multiple Google Accounts on Outlook";
        Size = new Size(800, 600);
        MinimumSize = new Size(640, 480);

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Add Account", null, OnAddAccount));
        toolbar.Items.Add(new ToolStripButton("Remove", null, OnRemoveAccount));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripButton("Sync Now", null, OnSyncNow));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripButton("Export Logs", null, OnExportLogs));

        _tabs = new TabControl { Dock = DockStyle.Fill };

        var accountsTab = new TabPage("Accounts");
        _accountsList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true
        };
        _accountsList.Columns.Add("Email", 200);
        _accountsList.Columns.Add("Calendars", 80);
        _accountsList.Columns.Add("Last Sync", 150);
        _accountsList.Columns.Add("Status", 100);
        accountsTab.Controls.Add(_accountsList);

        var statusTab = new TabPage("Status");
        _statusList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true
        };
        _statusList.Columns.Add("Calendar", 200);
        _statusList.Columns.Add("Account", 150);
        _statusList.Columns.Add("Progress", 100);
        _statusList.Columns.Add("Status", 150);
        _progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 20 };
        statusTab.Controls.Add(_statusList);
        statusTab.Controls.Add(_progressBar);

        var logsTab = new TabPage("Logs");
        _logsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false
        };
        _logsGrid.Columns.Add("Date", "Date");
        _logsGrid.Columns.Add("Account", "Account");
        _logsGrid.Columns.Add("Action", "Action");
        _logsGrid.Columns.Add("Result", "Result");
        logsTab.Controls.Add(_logsGrid);

        _tabs.TabPages.AddRange(new[] { accountsTab, statusTab, logsTab });

        _statusBar = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        _statusBar.Items.Add(_statusLabel);

        Controls.Add(_tabs);
        Controls.Add(toolbar);
        Controls.Add(_statusBar);

        toolbar.Dock = DockStyle.Top;

        if (_syncEngine != null)
        {
            _syncEngine.ProgressChanged += OnSyncProgress;
        }
    }

    private async void LoadAccounts()
    {
        _accountsList.Items.Clear();
        var accounts = await _tokenStore.GetAllAccountIdsAsync();

        foreach (var accountId in accounts)
        {
            var item = new ListViewItem(accountId);
            item.SubItems.Add("-");
            item.SubItems.Add("Never");
            item.SubItems.Add("Ready");
            _accountsList.Items.Add(item);
        }
    }

    private async void OnAddAccount(object? sender, EventArgs e)
    {
        if (_authService == null)
        {
            MessageBox.Show("Please set MGAO_CLIENT_ID and MGAO_CLIENT_SECRET environment variables.",
                "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _statusLabel.Text = "Authenticating...";
            var tempId = Guid.NewGuid().ToString();
            var credential = await _authService.AuthorizeAsync(tempId);
            var email = await _authService.GetAccountEmailAsync(credential);

            await _tokenStore.SaveTokenAsync(email,
                credential.Token.AccessToken,
                credential.Token.RefreshToken,
                DateTime.UtcNow.AddSeconds(credential.Token.ExpiresInSeconds ?? 3600));

            await _tokenStore.DeleteTokenAsync(tempId);

            // Fetch calendars and show selection dialog
            _statusLabel.Text = "Fetching calendars...";
            var calendars = await _googleClient!.GetCalendarsAsync(email);

            using var selectionForm = new CalendarSelectionForm(calendars);
            if (selectionForm.ShowDialog(this) == DialogResult.OK)
            {
                // Create Outlook folders for selected calendars
                _outlookBridge.Initialize();
                foreach (var cal in selectionForm.SelectedCalendars)
                {
                    _outlookBridge.GetOrCreateFolder(email, cal.Id, cal.Name);
                    await _stateStore.SaveSyncTokenAsync(email, cal.Id, null);
                }

                LoadAccounts();
                AddLog(email, "Add Account", $"Success ({selectionForm.SelectedCalendars.Count} calendars)");
                _statusLabel.Text = $"Added account: {email}";
            }
            else
            {
                await _tokenStore.DeleteTokenAsync(email);
                _statusLabel.Text = "Account setup cancelled";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Authentication failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Authentication failed";
        }
    }

    private async void OnRemoveAccount(object? sender, EventArgs e)
    {
        if (_accountsList.SelectedItems.Count == 0) return;

        var accountId = _accountsList.SelectedItems[0].Text;
        var result = MessageBox.Show($"Remove account {accountId}?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            await _tokenStore.DeleteTokenAsync(accountId);
            LoadAccounts();
            AddLog(accountId, "Remove Account", "Success");
        }
    }

    private async void OnSyncNow(object? sender, EventArgs e)
    {
        if (_syncEngine == null) return;

        _statusLabel.Text = "Syncing...";
        _progressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            var result = await _syncEngine.SyncAllAsync();

            _statusLabel.Text = result.Success
                ? $"Sync complete: {result.Created} created, {result.Updated} updated"
                : $"Sync failed: {result.Error}";

            AddLog("All", "Sync", result.Success ? "Success" : $"Failed: {result.Error}");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Sync error: {ex.Message}";
        }

        _progressBar.Style = ProgressBarStyle.Blocks;
        _progressBar.Value = 0;
    }

    private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnSyncProgress(sender, e));
            return;
        }

        _statusLabel.Text = $"{e.AccountId}: {e.Status}";
        _progressBar.Value = Math.Min(e.PercentComplete, 100);
    }

    private void OnExportLogs(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV files|*.csv",
            FileName = $"mgao_logs_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            using var writer = new StreamWriter(dialog.FileName);
            writer.WriteLine("Date,Account,Action,Result");

            foreach (DataGridViewRow row in _logsGrid.Rows)
            {
                var line = string.Join(",", row.Cells.Cast<DataGridViewCell>()
                    .Select(c => $"\"{c.Value}\""));
                writer.WriteLine(line);
            }

            _statusLabel.Text = $"Logs exported to {dialog.FileName}";
        }
    }

    private void AddLog(string account, string action, string result)
    {
        _logsGrid.Rows.Insert(0, DateTime.Now.ToString("g"), account, action, result);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _outlookBridge.Dispose();
        _stateStore.Dispose();
        base.OnFormClosing(e);
    }
}
