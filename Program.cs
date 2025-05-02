// Launcher.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace GameLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }

    public class LauncherForm : Form
    {
        private const string ServerUrl = "http://devilInfinite.com";
        // Remote endpoints
        private const string VersionUrl = $"{ServerUrl}/game/version.txt";
        private const string ManifestUrl = $"{ServerUrl}/game/manifest.json";
        private const string BannersUrl = $"{ServerUrl}/launcher/banners.json";
        private const string NewsUrl = $"{ServerUrl}/launcher/news.json";

        private readonly HttpClient httpClient = new HttpClient();
        private readonly string installPath;
        private readonly string localVersionPath;

        // UI elements
        private TableLayoutPanel mainLayout;
        private Panel navPanel;
        private Panel headerPanel;
        private Panel contentPanel;
        private Panel footerPanel;

        // Section panels
        private Panel homePanel;
        private Panel notificationsPanel;
        private Panel settingsPanel;
        private Panel helpPanel;

        // Home-specific elements
        private PictureBox bannerBox;
        private Button bannerPrev;
        private Button bannerNext;
        private FlowLayoutPanel newsFlow;
        private List<Image> banners = new List<Image>();
        private int currentBanner = 0;
        private Timer bannerTimer;
        private bool _isInstalled = false;
        // Footer elements
        private Label versionLabel;
        private Button playButton;

        public LauncherForm()
        {
            installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Devil Infinite");
            Directory.CreateDirectory(installPath);
            localVersionPath = Path.Combine(installPath, "version.txt");

            InitializeComponent();
            InitializeSections();
            ShowSection("Home");

            LoadDataAsync();


        }

        private async void LoadDataAsync()
        {
            while (true)
            {
                CheckForUpdatesAsync();
                LoadBannersAsync();
                LoadNewsAsync();
                //await Task.Delay(300);
                //if (!_isInstalled) {
                    
                //}
                await Task.Delay(15000);
            }
        }

        private void InitializeComponent()
        {
            Thread.Sleep(300);
            // Form setup
            Text = "Devil Infinite Launcher";
            ClientSize = new Size(1200, 600);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;

            // Main layout
            mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Margin = new Padding(0) };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(mainLayout);

            // Navigation panel
            navPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            mainLayout.Controls.Add(navPanel, 0, 0);
            AddNavButtons();

            // Right side layout
            var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, CellBorderStyle = TableLayoutPanelCellBorderStyle.None, Margin = new Padding(0) };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            mainLayout.Controls.Add(rightLayout, 1, 0);

            // Header
            headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 45), BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            rightLayout.Controls.Add(headerPanel, 0, 0);
            var headerLabel = new Label
            {
                Text = "ALL GAMES  |  MyGame",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Dock = DockStyle.Left,
                Padding = new Padding(10, 10, 0, 0)
            };
            headerPanel.Controls.Add(headerLabel);
            var exitBtn = new Button { Dock = DockStyle.Right, BackColor = Color.Transparent, Text = "X",ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat , Margin = new Padding(0)};
            exitBtn.Click += (s, e) => Close();
            headerPanel.Controls.Add(exitBtn);

            // Content placeholder
            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            rightLayout.Controls.Add(contentPanel, 0, 1);

            // Footer
            footerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(45, 45, 45), BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            rightLayout.Controls.Add(footerPanel, 0, 2);
            InitializeFooter();
        }

        private void AddNavButtons()
        {
            var sections = new[] { "Home", "Notifications", "Download", "Settings", "Help" };
            foreach (var name in sections)
            {
                var btn = new Button
                {
                    Text = name,
                    Dock = DockStyle.Top,
                    Height = 60,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(30, 30, 30),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => ShowSection(name);
                navPanel.Controls.Add(btn);
            }
        }

        private void InitializeSections()
        {
            // Home section
            homePanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            var homeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, CellBorderStyle = TableLayoutPanelCellBorderStyle.None };
            homeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            homeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            homePanel.Controls.Add(homeLayout);

            // Banner area
            var bannerContainer = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
            bannerBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            bannerPrev = new Button { Text = "<", Width = 30, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            bannerPrev.FlatAppearance.BorderSize = 0;
            bannerNext = new Button { Text = ">", Width = 30, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat };
            bannerNext.FlatAppearance.BorderSize = 0;
            bannerPrev.Click += (s, e) => ShowBanner((currentBanner - 1 + banners.Count) % banners.Count);
            bannerNext.Click += (s, e) => ShowBanner((currentBanner + 1) % banners.Count);
            bannerContainer.Controls.Add(bannerBox);
            bannerContainer.Controls.Add(bannerPrev);
            bannerContainer.Controls.Add(bannerNext);
            homeLayout.Controls.Add(bannerContainer, 0, 0);

            // News area
            newsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BorderStyle = BorderStyle.None, Margin = new Padding(0) };
            homeLayout.Controls.Add(newsFlow, 1, 0);

            // Notifications section
            notificationsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), Margin = new Padding(0) };
            var notLabel = new Label { Text = "No new notifications.", ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            notificationsPanel.Controls.Add(notLabel);

            // Settings section
            settingsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), Margin = new Padding(0) };
            var settingsLabel = new Label { Text = "Settings", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 12F), Dock = DockStyle.Top };
            var propGrid = new PropertyGrid { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            settingsPanel.Controls.Add(propGrid);
            settingsPanel.Controls.Add(settingsLabel);

            // Help section
            helpPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 60, 60), Margin = new Padding(0) };
            var helpText = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Text = "Help content: \r\n- Click PLAY to start.\r\n- Use Settings to customize.\r\n- Notifications will appear here."
            };
            helpPanel.Controls.Add(helpText);
        }

        private void ShowSection(string name)
        {
            contentPanel.Controls.Clear();
            switch (name)
            {
                case "Home": contentPanel.Controls.Add(homePanel); break;
                case "Notifications": contentPanel.Controls.Add(notificationsPanel); break;
                case "Settings": contentPanel.Controls.Add(settingsPanel); break;
                case "Help": contentPanel.Controls.Add(helpPanel); break;
                case "Download": // reuse Home or handle separately
                    contentPanel.Controls.Add(homePanel);
                    break;
                default:
                    contentPanel.Controls.Add(homePanel);
                    break;
            }
        }

        private async void LoadBannersAsync()
        {
            try
            {
                
                var json = await httpClient.GetStringAsync(BannersUrl);
                var urls = JsonSerializer.Deserialize<List<string>>(json);
                banners.Clear();
                foreach (var url in urls)
                {
                    var data = await httpClient.GetByteArrayAsync(url);
                    using var ms = new MemoryStream(data);
                   
                    banners.Add(Image.FromStream(ms));
                }
                if (banners.Count > 0)
                {
                    ShowBanner(0);
                    bannerTimer = new Timer { Interval = 5000 };
                    try
                    {
                        bannerTimer.Tick += (s, e) => ShowBanner((currentBanner + 1) % banners.Count);
                    }
                    catch { };
                    bannerTimer.Start();
                }
            }
            catch {
                LoadBannersAsync(); // Retry on failure
            }
        }

        private void ShowBanner(int index)
        {
            if (banners.Count == 0) return;
            currentBanner = index;
            bannerBox.Image = banners[index];
        }

        private async void LoadNewsAsync()
        {
            try
            {
                var json = await httpClient.GetStringAsync(NewsUrl);
                var items = JsonSerializer.Deserialize<List<NewsItem>>(json);
                newsFlow.Controls.Clear();
                foreach (var item in items)
                {
                    var panel = new Panel { Width = 300, Height = 80, Margin = new Padding(5), BackColor = Color.FromArgb(80, 80, 80) };
                    var title = new Label { Text = item.Title, ForeColor = Color.White, Dock = DockStyle.Top, Height = 40 };
                    var date = new Label { Text = item.Date.ToString("g"), ForeColor = Color.LightGray, Dock = DockStyle.Bottom, Height = 30 };
                    panel.Controls.Add(title);
                    panel.Controls.Add(date);
                    newsFlow.Controls.Add(panel);
                }
            }
            catch { }
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                var remoteVer = (await httpClient.GetStringAsync(VersionUrl)).Trim();
                var localVer = File.Exists(localVersionPath)
                    ? (await File.ReadAllTextAsync(localVersionPath)).Trim()
                    : "2504.0.0";

                if (string.Compare(remoteVer, localVer) > 0) {
                    versionLabel.Text = $"Update available: {remoteVer}";
                    playButton.Text = "DOWNLOAD";
                }
                else{
                    versionLabel.Text = $"Up to date: {localVer}";
                    playButton.Text = "PLAY";
                }
            }
            catch (Exception ex)
            {
                versionLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void InitializeFooter()
        {
            playButton = new Button
            {
                Text = "PLAY",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 120,
                BackColor = Color.Orange,
                FlatStyle = FlatStyle.Flat
            };
            playButton.FlatAppearance.BorderSize = 0;
            playButton.Click += (s, e) => LaunchGame();

            versionLabel = new Label
            {
                Text = "Fetching version...",
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };

            footerPanel.Controls.Add(versionLabel);
            footerPanel.Controls.Add(playButton);
        }

        private async void LaunchGame()
        {
            // if update is available (or first‐install), download first
            if (playButton.Text.Equals("DOWNLOAD"))
            {
                await DownloadGame();
                return;
            }

            // otherwise try to start
            var exe = Path.Combine(installPath, "DevilInfinite.exe");
            if (File.Exists(exe))
            {
                Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = installPath });
            }
            else
            {
                MessageBox.Show(
                    "Game executable missing. Re-downloading now…",
                    "Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                await DownloadGame();
            }
        }

        // 2) Revised DownloadGame with lock detection and FilePath usage
        private async Task DownloadGame()
        {
            playButton.Enabled = false;
            versionLabel.Text = "Downloading game...";

            try
            {
                var manifestJson = await httpClient.GetStringAsync(ManifestUrl);
                var manifest = JsonSerializer.Deserialize<List<ManifestEntry>>(manifestJson)
                               ?? throw new InvalidOperationException("Invalid manifest format.");

                int done = 0, total = manifest.Count;
                foreach (var entry in manifest)
                {
                    var remoteUrl = $"{ServerUrl}{entry.Url}";
                    var destPath = Path.Combine(installPath, entry.FileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    try
                    {
                        using var resp = await httpClient.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead);
                        resp.EnsureSuccessStatusCode();

                        await using var remoteStream = await resp.Content.ReadAsStreamAsync();
                        await using var fileStream = new FileStream(
                            destPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None
                        );
                        await remoteStream.CopyToAsync(fileStream);
                    }
                    catch (IOException ioEx) when (IsFileLocked(ioEx))
                    {
                        MessageBox.Show(
                            $"Cannot update “{entry.FileName}” because it is in use.\n" +
                            "Please close the game and try again.",
                            "Update Blocked",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    // checksum check
                    if (!string.IsNullOrEmpty(entry.Checksum))
                    {
                        var actual = BitConverter
                            .ToString(SHA256.Create().ComputeHash(File.ReadAllBytes(destPath)))
                            .Replace("-", "").ToLowerInvariant();

                        if (!actual.Equals(entry.Checksum, StringComparison.OrdinalIgnoreCase))
                            throw new IOException($"Checksum mismatch for {entry.FileName}");
                    }

                    done++;
                    versionLabel.Text = $"Downloading... ({done}/{total})";
                }

                // finalize
                var remoteVer = (await httpClient.GetStringAsync(VersionUrl)).Trim();
                await File.WriteAllTextAsync(localVersionPath, remoteVer);
                _isInstalled = true;
                playButton.Text = "PLAY";
                versionLabel.Text = $"Up to date: {remoteVer}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to download game:\n{ex.Message}",
                    "Download Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                versionLabel.Text = "Download error – see log";
            }
            finally
            {
                playButton.Enabled = true;
            }
        }

        // 3) Helper to detect a sharing‐violation on Windows
        private static bool IsFileLocked(IOException ex)
        {
            const int ERROR_SHARING_VIOLATION = 0x20;
            const int ERROR_LOCK_VIOLATION = 0x21;
            int hr = Marshal.GetHRForException(ex) & 0xFFFF;
            return hr == ERROR_SHARING_VIOLATION || hr == ERROR_LOCK_VIOLATION;
        }

        private class NewsItem
        {
            public string Title { get; set; }
            public DateTime Date { get; set; }
        }

        // Add this inner class somewhere in LauncherForm (e.g. next to NewsItem)
        private class ManifestEntry
        {
            public string FileName { get; set; }

            public string FilePath { get; set; }  
            public string Url { get; set; }
            public string Size { get; set; }  // optional: size in bytes
            public string Checksum { get; set; }  // optional: SHA256 or MD5
        }

    }
}