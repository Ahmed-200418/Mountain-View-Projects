using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ProjectManagerDesigner
{
    public partial class Form1 : Form
    {
        // Data
        private Dictionary<string, Dictionary<string, string>> projectsByRegion = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Sidebar
        private bool sidebarExpanded = true;
        private const int SIDEBAR_EXPANDED_WIDTH = 220;
        private const int SIDEBAR_COLLAPSED_WIDTH = 60;
        private readonly System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private int targetWidth;
        private int animationStep = 15;

        // Selection state
        private string selectedContractName = string.Empty;           // Selected contract folder name
        private string selectedContractPath = string.Empty;           // Selected contract folder path
        private string currentFolderRoot = string.Empty;              // Root folder (DMC/CURVE)
        private string selectedIndependentItemName = string.Empty;    // Selected independent item name

        // Navigation state for proper back navigation
        private string currentRegion = string.Empty;                  // Current selected region
        private string currentProjectName = string.Empty;             // Current selected project name
        private string currentProjectPath = string.Empty;             // Current selected project path

        // UI (runtime)
        private Panel? pathsPanel;
        private readonly Button?[] pathButtons = new Button?[3]; // Changed from 4 to 3

        // Independent items panel (top beside contract box)
        private Panel? independentItemsPanel;

        // Contract and item display labels
        private Label? contractNameLabel;
        private Label? itemNameLabel;

        // Search and tooltips
        private readonly ToolTip sharedToolTip = new ToolTip();
        private TextBox? projectSearchTextBox;
        private readonly List<Button> currentProjectButtons = new List<Button>();

        // CSV cache
        private DateTime lastCsvWriteTime = DateTime.MinValue;
        private string lastLoadedCsvPath = string.Empty;

        // Settings persistence
        private const string SettingsFileName = "user_settings.json";
        private const string FavoritesFileName = "favorites.json";
        private UserSettings appSettings = new UserSettings();

        // Favorites functionality
        private HashSet<string> favoriteRegions = new HashSet<string>();
        private HashSet<string> favoriteProjects = new HashSet<string>();
        private HashSet<string> favoriteContracts = new HashSet<string>();

        // Constants for folder names under each contract
        private const string SoftCopyFolderName = "soft copy";
        private const string ScanFolderName = "scan";
        private const string LogsFolderName = "Logs";

        private class UserSettings
        {
            public bool SidebarExpanded { get; set; } = true;
        }

        private class FavoritesData
        {
            public HashSet<string> Regions { get; set; } = new HashSet<string>();
            public HashSet<string> Projects { get; set; } = new HashSet<string>();
            public HashSet<string> Contracts { get; set; } = new HashSet<string>();
        }

        public Form1()
        {
            InitializeComponent();

            // Enable scrolling for content pages
            contentPanel.AutoScroll = true;

            LoadUserSettings();
            LoadFavorites();
            sidebarExpanded = appSettings.SidebarExpanded;
            sidebarPanel.Width = sidebarExpanded ? SIDEBAR_EXPANDED_WIDTH : SIDEBAR_COLLAPSED_WIDTH;
            UpdateButtonLayout();
            UpdateSidebarTexts();

            // Position settings button at bottom with proper spacing
            PositionSettingsButton();

            LoadProjectsFromCSV();
            SetupEventHandlers();
            LoadLogo();
            AddModernEffects();

            animationTimer.Interval = 20; // ~50 FPS
            animationTimer.Tick += AnimationTimer_Tick;

            ShowWelcomeMessage();

            this.FormClosing += (s, e) =>
            {
                SaveUserSettings();
                SaveFavorites();
            };
        }

        private void PositionSettingsButton()
        {
            // Position settings button at bottom of sidebar with spacing
            int bottomMargin = 20; // Space from bottom
            int buttonHeight = 73; // Same as other buttons
            int buttonWidth = sidebarPanel.Width - 22; // Account for left/right margins (11 each side)

            settingsButton.Size = new Size(buttonWidth, buttonHeight);
            settingsButton.Location = new Point(11, sidebarPanel.Height - buttonHeight - bottomMargin);

            // Update position when sidebar resizes
            sidebarPanel.Resize += (s, e) => {
                if (settingsButton != null)
                {
                    int newButtonWidth = sidebarPanel.Width - 22;
                    settingsButton.Size = new Size(newButtonWidth, buttonHeight);
                    settingsButton.Location = new Point(11, sidebarPanel.Height - buttonHeight - bottomMargin);
                }
            };
        }

        // ------------------------- Animation -------------------------

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (sidebarPanel.Width == targetWidth) return;

            int diff = targetWidth - sidebarPanel.Width;
            int step = Math.Sign(diff) * Math.Min(Math.Abs(diff), animationStep);
            sidebarPanel.Width += step;

            if (sidebarPanel.Width == targetWidth)
            {
                animationTimer.Stop();
                UpdateButtonLayout();
            }
        }

        // ------------------------- Visuals -------------------------

        private void AddModernEffects()
        {
            sidebarPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, sidebarPanel.Width - 3, 0, 3, sidebarPanel.Height);
                }
            };
        }

        private void LoadLogo()
        {
            try
            {
                // Try multiple locations for logo.png
                string[] possiblePaths = {
                    Path.Combine(Application.StartupPath, "logo.png"),                    // bin\Debug\logo.png
                    Path.Combine(Directory.GetCurrentDirectory(), "logo.png"),           // Current directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png"),     // Base directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "logo.png"), // Project root (for Debug builds)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "logo.png")        // Alternative project root
                };

                string logoPath = null;
                foreach (string path in possiblePaths)
                {
                    try
                    {
                        string fullPath = Path.GetFullPath(path);
                        if (File.Exists(fullPath))
                        {
                            logoPath = fullPath;
                            break;
                        }
                    }
                    catch
                    {
                        // Continue to next path if this one fails
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(logoPath))
                {
                    // Load the custom logo
                    using (var fileStream = new FileStream(logoPath, FileMode.Open, FileAccess.Read))
                    {
                        Image originalImage = Image.FromStream(fileStream);
                        logoPictureBox.Image = new Bitmap(originalImage); // Create a copy to avoid file locking
                        logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                }
                else
                {
                    // Fallback to the default created icon
                    CreateIconLogo();
                }
            }
            catch
            {
                // If there's any error loading the custom logo, fallback to default
                try
                {
                    CreateIconLogo();
                }
                catch
                {
                    // If even the default logo creation fails, show a simple text
                    logoPictureBox.Image = null;
                }
            }
        }

        private void CreateIconLogo()
        {
            // Create a bigger bitmap for the larger logo
            Bitmap logoBitmap = new Bitmap(80, 80);
            using (Graphics g = Graphics.FromImage(logoBitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Scale up the building design
                using (var brush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    Point[] building = {
                        new Point(16, 64),  // Scaled up coordinates
                        new Point(16, 24),
                        new Point(40, 8),
                        new Point(64, 24),
                        new Point(64, 64)
                    };
                    g.FillPolygon(brush, building);
                }

                // Scale up the windows
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(whiteBrush, 28, 32, 6, 6);
                    g.FillRectangle(whiteBrush, 44, 32, 6, 6);
                    g.FillRectangle(whiteBrush, 28, 48, 6, 6);
                    g.FillRectangle(whiteBrush, 44, 48, 6, 6);
                }
            }
            logoPictureBox.Image = logoBitmap;
        }

        // ------------------------- Data -------------------------

        private void LoadProjectsFromCSV()
        {
            try
            {
                string csvPath = Path.Combine(Application.StartupPath, "Projects.csv");
                lastLoadedCsvPath = csvPath;

                if (!File.Exists(csvPath))
                {
                    MessageBox.Show($"Projects file not found:\n{csvPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(csvPath);
                if (writeTime == lastCsvWriteTime && projectsByRegion.Count > 0)
                {
                    return; // cached
                }

                string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var parts = ParseCsvLine(lines[i]);
                    if (parts.Count >= 3)
                    {
                        string region = parts[0].Trim();
                        string projectName = parts[1].Trim();
                        string projectPath = parts[2].Trim();

                        if (!dict.ContainsKey(region))
                        {
                            dict[region] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        dict[region][projectName] = projectPath;
                    }
                }

                projectsByRegion = dict;
                lastCsvWriteTime = writeTime;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading projects file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line)) return result;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            result.Add(sb.ToString());
            return result;
        }

        // ------------------------- Events -------------------------

        private void SetupEventHandlers()
        {
            homeButton.Click += (s, e) => ShowWelcomeMessage();
            projectsButton.Click += (s, e) => ShowRegionSelection();
            settingsButton.Click += (s, e) => ShowSettings();

            toggleButton.Click += (s, e) => ToggleSidebar();

            toggleButton.MouseEnter += (s, e) =>
            {
                toggleButton.BackColor = Color.FromArgb(255, 206, 84);
                toggleButton.Font = new Font("Arial", 18F, FontStyle.Bold);
            };
            toggleButton.MouseLeave += (s, e) =>
            {
                toggleButton.BackColor = Color.FromArgb(255, 193, 7);
                toggleButton.Font = new Font("Arial", 16F, FontStyle.Bold);
            };

            AddButtonHoverEffects(homeButton);
            AddButtonHoverEffects(projectsButton);
            AddButtonHoverEffects(settingsButton);
        }

        private void AddButtonHoverEffects(Button button)
        {
            Color baseColor = Color.FromArgb(35, 65, 119);
            Color hoverColor = Color.FromArgb(45, 75, 129);

            button.MouseEnter += (s, e) =>
            {
                button.BackColor = hoverColor;
                if (!sidebarExpanded)
                {
                    button.Font = new Font(button.Font.FontFamily, button.Font.Size + 2, button.Font.Style);
                }
            };
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = baseColor;
                if (!sidebarExpanded)
                {
                    button.Font = new Font(button.Font.FontFamily, button.Font.Size - 2, button.Font.Style);
                }
            };
        }

        // ------------------------- Sidebar -------------------------

        private void ToggleSidebar()
        {
            sidebarExpanded = !sidebarExpanded;
            appSettings.SidebarExpanded = sidebarExpanded;
            SaveUserSettings();

            targetWidth = sidebarExpanded ? SIDEBAR_EXPANDED_WIDTH : SIDEBAR_COLLAPSED_WIDTH;
            UpdateSidebarTexts();
            animationTimer.Start();
            UpdateButtonLayout();
        }

        private void UpdateSidebarTexts()
        {
            if (sidebarExpanded)
            {
                homeButton.Text = "🏠  Home";
                projectsButton.Text = "📁  Projects";
                settingsButton.Text = "⚙️  Settings";
                logoPanel.Visible = true;
            }
            else
            {
                homeButton.Text = "🏠";
                projectsButton.Text = "📁";
                settingsButton.Text = "⚙️";
                logoPanel.Visible = true;
            }
        }

        private void UpdateButtonLayout()
        {
            if (sidebarExpanded)
            {
                homeButton.Size = new Size(200, 55);
                projectsButton.Size = new Size(200, 55);
                settingsButton.Size = new Size(200, 55);
                homeButton.Location = new Point(10, 140); // Moved down to make space for bigger logo
                projectsButton.Location = new Point(10, 205);
                // Keep settings button at bottom with proper anchor
                settingsButton.Location = new Point(10, sidebarPanel.Height - 80);
                homeButton.TextAlign = ContentAlignment.MiddleLeft;
                projectsButton.TextAlign = ContentAlignment.MiddleLeft;
                settingsButton.TextAlign = ContentAlignment.MiddleLeft;

                // Bigger logo panel centered
                logoPanel.Location = new Point(10, 10);
                logoPanel.Size = new Size(200, 120); // Bigger panel
                logoPictureBox.Location = new Point((logoPanel.Width - 80) / 2, (logoPanel.Height - 80) / 2); // Centered
                logoPictureBox.Size = new Size(80, 80); // Bigger logo

                toggleButton.Location = new Point(175, 85);
                toggleButton.Size = new Size(35, 30);
            }
            else
            {
                homeButton.Size = new Size(50, 50);
                projectsButton.Size = new Size(50, 50);
                settingsButton.Size = new Size(50, 50);
                homeButton.Location = new Point(5, 140); // Moved down to make space for bigger logo
                projectsButton.Location = new Point(5, 200);
                // Keep settings button at bottom in collapsed mode too
                settingsButton.Location = new Point(5, sidebarPanel.Height - 80);
                homeButton.TextAlign = ContentAlignment.MiddleCenter;
                projectsButton.TextAlign = ContentAlignment.MiddleCenter;
                settingsButton.TextAlign = ContentAlignment.MiddleCenter;

                // Bigger logo even in collapsed mode
                logoPanel.Location = new Point(5, 10);
                logoPanel.Size = new Size(50, 120); // Taller panel
                logoPictureBox.Location = new Point((logoPanel.Width - 40) / 2, (logoPanel.Height - 40) / 2); // Centered
                logoPictureBox.Size = new Size(40, 40); // Bigger than before

                toggleButton.Location = new Point(12, 95);
                toggleButton.Size = new Size(35, 30);
            }
        }

        // ------------------------- Top Bar Management -------------------------

        private void ClearTopSearchPanel()
        {
            // Clear all controls from the top search panel
            topSearchPanel.Controls.Clear();
        }

        private void AddTopBarButtons(string projectName, string projectPath, string pageType, string region = "")
        {
            ClearTopSearchPanel();

            // Back button - far right with improved navigation logic
            Button backButton = new Button
            {
                Text = GetBackButtonText(pageType),
                Size = new Size(140, 35),
                Location = new Point(topSearchPanel.Width - 160, 22),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            backButton.MouseEnter += (s, e) => backButton.BackColor = Color.FromArgb(41, 128, 185);
            backButton.MouseLeave += (s, e) => backButton.BackColor = Color.FromArgb(52, 152, 219);

            // Set back button action based on page type
            SetBackButtonAction(backButton, pageType, projectName, projectPath, region);

            if (pageType == "project")
            {
                // Project folder button - center
                Button projectFolderButton = new Button
                {
                    Text = "Open Projects Folder",
                    Size = new Size(200, 35),
                    Location = new Point((topSearchPanel.Width - 200) / 2, 22),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top
                };
                projectFolderButton.MouseEnter += (s, e) => projectFolderButton.BackColor = Color.FromArgb(41, 128, 185);
                projectFolderButton.MouseLeave += (s, e) => projectFolderButton.BackColor = Color.FromArgb(52, 152, 219);
                projectFolderButton.Click += (s, e) => OpenPath(projectPath, "Projects Folder");
                topSearchPanel.Controls.Add(projectFolderButton);
            }
            else if (pageType == "region")
            {
                // Regions folder button - center
                Button regionsFolderButton = new Button
                {
                    Text = "Open Regions Folder",
                    Size = new Size(200, 35),
                    Location = new Point((topSearchPanel.Width - 400) / 2, 22),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top
                };
                regionsFolderButton.MouseEnter += (s, e) => regionsFolderButton.BackColor = Color.FromArgb(41, 128, 185);
                regionsFolderButton.MouseLeave += (s, e) => regionsFolderButton.BackColor = Color.FromArgb(52, 152, 219);
                regionsFolderButton.Click += (s, e) => {
                    // Open the parent directory of all projects in this region
                    if (projectsByRegion.ContainsKey(region) && projectsByRegion[region].Count > 0)
                    {
                        var firstProject = projectsByRegion[region].First().Value;
                        var parentDir = Directory.GetParent(firstProject)?.FullName;
                        if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                        {
                            OpenPath(parentDir, "Regions Folder");
                        }
                    }
                };
                topSearchPanel.Controls.Add(regionsFolderButton);

                // Project search box - left side (ONLY in projects page)
                TextBox searchBox = new TextBox
                {
                    PlaceholderText = "Search for project...",
                    Size = new Size(300, 30),
                    Location = new Point(20, 25), // Left side positioning
                    Font = new Font("Arial", 12),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };
                searchBox.TextChanged += (s, e) => FilterProjects(region, searchBox.Text);
                topSearchPanel.Controls.Add(searchBox);
                projectSearchTextBox = searchBox; // Store reference for filtering
            }
            else if (pageType == "subfolder")
            {
                // DMC/CURVE folder button - center (always show for contract pages)
                Button mainFolderButton = new Button
                {
                    Text = GetFolderButtonText(),
                    Size = new Size(200, 35),
                    Location = new Point((topSearchPanel.Width - 200) / 2, 22),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top,
                    MaximumSize = new Size(250, 40),
                    MinimumSize = new Size(150, 30)
                };

                // Update location when panel resizes
                topSearchPanel.Resize += (s, e) => {
                    mainFolderButton.Location = new Point((topSearchPanel.Width - mainFolderButton.Width) / 2, 22);
                };
                mainFolderButton.MouseEnter += (s, e) => mainFolderButton.BackColor = Color.FromArgb(41, 128, 185);
                mainFolderButton.MouseLeave += (s, e) => mainFolderButton.BackColor = Color.FromArgb(52, 152, 219);
                mainFolderButton.Click += (s, e) => OpenPath(currentFolderRoot, currentFolderRoot.Split('\\').Last());
                topSearchPanel.Controls.Add(mainFolderButton);
            }

            topSearchPanel.Controls.Add(backButton);
        }

        private string GetBackButtonText(string pageType)
        {
            switch (pageType)
            {
                case "project":
                    return "← Back to Regions";
                case "region":
                    return "← Back to Regions";
                case "subfolder":
                    return "← Back to Project";
                default:
                    return "← Back";
            }
        }

        private void SetBackButtonAction(Button backButton, string pageType, string projectName, string projectPath, string region)
        {
            switch (pageType)
            {
                case "project":
                    backButton.Click += (s, e) => ShowRegionSelection();
                    break;
                case "region":
                    backButton.Click += (s, e) => ShowRegionSelection();
                    break;
                case "subfolder":
                    backButton.Click += (s, e) => ShowProjectPaths(currentProjectName, currentProjectPath);
                    break;
            }
        }

        private string GetFolderButtonText()
        {
            if (string.IsNullOrEmpty(currentFolderRoot))
                return "Open Folder";

            // Get the folder name from the path
            string folderName = Path.GetFileName(currentFolderRoot);

            // Return specific text based on folder type
            switch (folderName.ToUpper())
            {
                case "DMC":
                    return "Open DMC Folder";
                case "CURVE":
                    return "Open Curve Folder";
                default:
                    return $"Open {folderName} Folder";
            }
        }



        // ------------------------- Pages -------------------------

        private void ShowWelcomeMessage()
        {
            ClearContentPanel();
            ClearTopSearchPanel(); // Clear any search bars from other pages

            Label welcomeLabel = new Label
            {
                Text = "Welcome to Mountain View Projects",
                Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 55, 109),
                Size = new Size(900, 60), // Increased width to fit full text
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((contentPanel.Width - 900) / 2, 30),
                Anchor = AnchorStyles.Top // Center horizontally
            };
            contentPanel.Controls.Add(welcomeLabel);

            // Update welcome label position when content panel resizes
            contentPanel.Resize += (s, e) => {
                if (welcomeLabel != null)
                {
                    welcomeLabel.Location = new Point((contentPanel.Width - 900) / 2, 30);
                }
            };

            // Favorites section
            Label favoritesTitle = new Label
            {
                Text = "★ Favorites",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 55, 109),
                Size = new Size(300, 50),
                Location = new Point(50, 80)
            };
            contentPanel.Controls.Add(favoritesTitle);

            // Create a main container for all favorites with fixed layout
            Panel favoritesContainer = new Panel
            {
                Location = new Point(30, 140),
                Size = new Size(1000, 800), // Fixed size instead of dynamic
                AutoScroll = true // Enable scrolling if needed
            };
            contentPanel.Controls.Add(favoritesContainer);

            int currentY = 0;
            int sectionSpacing = 30; // Reduced spacing
            int fixedPanelWidth = 950; // Fixed width for all panels

            // Favorite Regions Section
            if (favoriteRegions.Count > 0)
            {
                Label regionsLabel = new Label
                {
                    Text = "📍 Regions",
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(25, 55, 109),
                    Size = new Size(300, 35), // Increased width
                    Location = new Point(20, currentY),
                    BackColor = Color.Transparent // Ensure background is transparent
                };
                favoritesContainer.Controls.Add(regionsLabel);
                currentY += 45;

                // Create responsive FlowLayoutPanel for regions
                FlowLayoutPanel regionsFlow = new FlowLayoutPanel
                {
                    Location = new Point(20, currentY),
                    Size = new Size(fixedPanelWidth, 80),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoScroll = false,
                    Padding = new Padding(5, 5, 5, 5)
                };
                favoritesContainer.Controls.Add(regionsFlow);

                foreach (var region in favoriteRegions)
                {
                    Button regionBtn = new Button
                    {
                        Text = region,
                        Size = new Size(180, 60),
                        BackColor = Color.FromArgb(52, 152, 219), // Light blue for all
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Arial", 11, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(5, 5, 5, 5),
                        MinimumSize = new Size(150, 50),
                        MaximumSize = new Size(220, 70)
                    };
                    regionBtn.MouseEnter += (s, e) => regionBtn.BackColor = Color.FromArgb(41, 128, 185);
                    regionBtn.MouseLeave += (s, e) => regionBtn.BackColor = Color.FromArgb(52, 152, 219);
                    regionBtn.Click += (s, e) => ShowProjectsInRegion(region);
                    regionsFlow.Controls.Add(regionBtn);
                }

                currentY += 90 + sectionSpacing;
            }

            // Favorite Projects Section
            if (favoriteProjects.Count > 0)
            {
                Label projectsLabel = new Label
                {
                    Text = "📁 Projects",
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(25, 55, 109),
                    Size = new Size(300, 35), // Increased width
                    Location = new Point(20, currentY),
                    BackColor = Color.Transparent // Ensure background is transparent
                };
                favoritesContainer.Controls.Add(projectsLabel);
                currentY += 45;

                // Create responsive FlowLayoutPanel for projects
                FlowLayoutPanel projectsFlow = new FlowLayoutPanel
                {
                    Location = new Point(20, currentY),
                    Size = new Size(fixedPanelWidth, 80),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoScroll = false,
                    Padding = new Padding(5, 5, 5, 5)
                };
                favoritesContainer.Controls.Add(projectsFlow);

                foreach (var project in favoriteProjects)
                {
                    Button projectBtn = new Button
                    {
                        Text = project,
                        Size = new Size(180, 60),
                        BackColor = Color.FromArgb(52, 152, 219), // Light blue for all
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Arial", 11, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(5, 5, 5, 5),
                        MinimumSize = new Size(150, 50),
                        MaximumSize = new Size(220, 70)
                    };
                    projectBtn.MouseEnter += (s, e) => projectBtn.BackColor = Color.FromArgb(41, 128, 185);
                    projectBtn.MouseLeave += (s, e) => projectBtn.BackColor = Color.FromArgb(52, 152, 219);

                    // Find project path and show project paths
                    foreach (var regionKv in projectsByRegion)
                    {
                        if (regionKv.Value.ContainsKey(project))
                        {
                            string projectPath = regionKv.Value[project];
                            projectBtn.Click += (s, e) => ShowProjectPaths(project, projectPath);
                            break;
                        }
                    }
                    projectsFlow.Controls.Add(projectBtn);
                }

                currentY += 90 + sectionSpacing;
            }

            // Favorite Contracts Section
            if (favoriteContracts.Count > 0)
            {
                Label contractsLabel = new Label
                {
                    Text = "📋 Contracts",
                    Font = new Font("Arial", 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(25, 55, 109),
                    Size = new Size(300, 35), // Increased width
                    Location = new Point(20, currentY),
                    BackColor = Color.Transparent // Ensure background is transparent
                };
                favoritesContainer.Controls.Add(contractsLabel);
                currentY += 45;

                // Create responsive FlowLayoutPanel for contracts
                FlowLayoutPanel contractsFlow = new FlowLayoutPanel
                {
                    Location = new Point(20, currentY),
                    Size = new Size(fixedPanelWidth, 80),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoScroll = false,
                    Padding = new Padding(5, 5, 5, 5)
                };
                favoritesContainer.Controls.Add(contractsFlow);

                foreach (var contract in favoriteContracts)
                {
                    Button contractBtn = new Button
                    {
                        Text = contract,
                        Size = new Size(180, 60),
                        BackColor = Color.FromArgb(52, 152, 219), // Light blue for all
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Arial", 11, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(5, 5, 5, 5),
                        MinimumSize = new Size(150, 50),
                        MaximumSize = new Size(220, 70)
                    };
                    contractBtn.MouseEnter += (s, e) => contractBtn.BackColor = Color.FromArgb(41, 128, 185);
                    contractBtn.MouseLeave += (s, e) => contractBtn.BackColor = Color.FromArgb(52, 152, 219);

                    // Find contract path and navigate to it
                    bool contractFound = false;
                    foreach (var regionKv in projectsByRegion)
                    {
                        foreach (var projectKv in regionKv.Value)
                        {
                            string projectPath = projectKv.Value;
                            string dmcPath = Path.Combine(projectPath, "DMC", contract);
                            string curvePath = Path.Combine(projectPath, "CURVE", contract);

                            if (Directory.Exists(dmcPath))
                            {
                                contractBtn.Click += (s, e) => {
                                    ShowSubFolders(projectKv.Key, projectPath, "DMC");
                                    // Select the contract after showing the page
                                    SelectContract(dmcPath, contract);
                                };
                                contractFound = true;
                                break;
                            }
                            else if (Directory.Exists(curvePath))
                            {
                                contractBtn.Click += (s, e) => {
                                    ShowSubFolders(projectKv.Key, projectPath, "CURVE");
                                    // Select the contract after showing the page
                                    SelectContract(curvePath, contract);
                                };
                                contractFound = true;
                                break;
                            }
                        }
                        if (contractFound) break;
                    }

                    if (!contractFound)
                    {
                        // If contract not found, disable the button
                        contractBtn.Enabled = false;
                        contractBtn.BackColor = Color.FromArgb(128, 128, 128);
                        contractBtn.Text = contract + " (Not Found)";
                    }

                    contractsFlow.Controls.Add(contractBtn);
                }

                currentY += 90 + sectionSpacing;
            }

            // Show message if no favorites
            if (favoriteRegions.Count == 0 && favoriteProjects.Count == 0 && favoriteContracts.Count == 0)
            {
                Panel noFavoritesPanel = new Panel
                {
                    Location = new Point(20, currentY),
                    Size = new Size(favoritesContainer.Width - 40, 150),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle
                };
                favoritesContainer.Controls.Add(noFavoritesPanel);

                Label noFavoritesIcon = new Label
                {
                    Text = "⭐",
                    Font = new Font("Arial", 48, FontStyle.Regular),
                    ForeColor = Color.FromArgb(189, 195, 199),
                    Size = new Size(80, 80),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point((noFavoritesPanel.Width - 80) / 2, 20)
                };
                noFavoritesPanel.Controls.Add(noFavoritesIcon);

                Label noFavoritesLabel = new Label
                {
                    Text = "No favorites yet\nRight-click on regions, projects, or contracts to add them to favorites.",
                    Font = new Font("Arial", 14, FontStyle.Regular),
                    ForeColor = Color.FromArgb(127, 140, 141),
                    Size = new Size(noFavoritesPanel.Width - 40, 60),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(20, 100)
                };
                noFavoritesPanel.Controls.Add(noFavoritesLabel);
            }
        }

        private void ShowRegionSelection()
        {
            ClearContentPanel();
            ClearTopSearchPanel();

            // Add Open Regions Folder button in top bar
            Button openRegionsFolderButton = new Button
            {
                Text = "Open Regions Folder",
                Size = new Size(200, 35),
                Location = new Point((topSearchPanel.Width - 200) / 2, 22),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top,
                MaximumSize = new Size(250, 40),
                MinimumSize = new Size(150, 30)
            };

            // Update location when panel resizes
            topSearchPanel.Resize += (s, e) => {
                openRegionsFolderButton.Location = new Point((topSearchPanel.Width - openRegionsFolderButton.Width) / 2, 22);
            };
            openRegionsFolderButton.MouseEnter += (s, e) => openRegionsFolderButton.BackColor = Color.FromArgb(41, 128, 185);
            openRegionsFolderButton.MouseLeave += (s, e) => openRegionsFolderButton.BackColor = Color.FromArgb(52, 152, 219);
            openRegionsFolderButton.Click += (s, e) => {
                // Open the parent directory of all projects (regions folder)
                if (projectsByRegion.Count > 0)
                {
                    var firstRegion = projectsByRegion.First().Value;
                    if (firstRegion.Count > 0)
                    {
                        var firstProject = firstRegion.First().Value;
                        var parentDir = Directory.GetParent(firstProject)?.Parent?.FullName;
                        if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                        {
                            OpenPath(parentDir, "Regions Folder");
                        }
                    }
                }
            };
            topSearchPanel.Controls.Add(openRegionsFolderButton);

            Label titleLabel = new Label
            {
                Text = "Select Region:",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Size = new Size(300, 40),
                Location = new Point(20, 20)
            };
            contentPanel.Controls.Add(titleLabel);

            // Enable scroll for long lists
            // Create FlowLayoutPanel for responsive region buttons
            FlowLayoutPanel regionsFlow = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(contentPanel.Width - 40, contentPanel.Height - 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 10)
            };
            contentPanel.Controls.Add(regionsFlow);

            foreach (string region in projectsByRegion.Keys)
            {
                Button regionButton = CreateRegionButton(region, Point.Empty);
                regionButton.Margin = new Padding(0, 5, 0, 5);
                regionsFlow.Controls.Add(regionButton);
            }
        }

        private Button CreateRegionButton(string region, Point location)
        {
            // Add star if it's a favorite
            string displayText = favoriteRegions.Contains(region) ? $"⭐ {region}" : region;

            var btn = new Button
            {
                Text = displayText,
                Size = new Size(250, 70),
                BackColor = Color.FromArgb(25, 55, 109), // Same color for all
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 13, FontStyle.Bold),
                Cursor = Cursors.Hand,
                MinimumSize = new Size(200, 60),
                MaximumSize = new Size(300, 80)
            };

            // Only set location if it's not Point.Empty (for FlowLayoutPanel usage)
            if (location != Point.Empty)
            {
                btn.Location = location;
            }
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(35, 65, 119);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(25, 55, 109);
            btn.Click += (s, e) => ShowProjectsInRegion(region);

            // Add right-click context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem favoriteItem = new ToolStripMenuItem(
                favoriteRegions.Contains(region) ? "Remove from Favorites" : "Add to Favorites");
            favoriteItem.Click += (s, e) =>
            {
                ToggleFavorite("region", region);
                // Refresh the region selection page
                ShowRegionSelection();
            };
            contextMenu.Items.Add(favoriteItem);
            btn.ContextMenuStrip = contextMenu;

            return btn;
        }

        private void ShowProjectsInRegion(string region)
        {
            ClearContentPanel();
            currentProjectButtons.Clear();

            // Store current region for navigation
            currentRegion = region;

            // Add top bar with back button and search
            AddTopBarButtons("", "", "region", region);

            Label titleLabel = new Label
            {
                Text = $"Projects in {region}:",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Size = new Size(400, 40),
                Location = new Point(20, 20)
            };
            contentPanel.Controls.Add(titleLabel);

            // Create FlowLayoutPanel for responsive project buttons
            FlowLayoutPanel projectsFlow = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(contentPanel.Width - 40, contentPanel.Height - 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 0, 0, 0)
            };
            contentPanel.Controls.Add(projectsFlow);

            if (!projectsByRegion.ContainsKey(region)) return;

            var projectList = projectsByRegion[region].ToList();

            foreach (var project in projectList)
            {
                string projectName = project.Key;
                string projectPath = project.Value;

                Button projectButton = CreateProjectButton(projectName, projectPath, Point.Empty, new Size(280, 80));
                projectButton.Margin = new Padding(5, 5, 5, 5);
                projectButton.MaximumSize = new Size(300, 90);
                projectButton.MinimumSize = new Size(250, 70);
                projectsFlow.Controls.Add(projectButton);
                currentProjectButtons.Add(projectButton);
            }
        }

        private Button CreateProjectButton(string projectName, string projectPath, Point location, Size size)
        {
            // Add star if it's a favorite
            string displayText = favoriteProjects.Contains(projectName) ? $"⭐ {projectName}" : projectName;

            var btn = new Button
            {
                Text = displayText,
                Size = size,
                BackColor = Color.FromArgb(25, 55, 109), // Same color for all
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                MinimumSize = new Size(250, 60),
                MaximumSize = new Size(350, 100)
            };

            // Only set location if it's not Point.Empty (for FlowLayoutPanel usage)
            if (location != Point.Empty)
            {
                btn.Location = location;
            }
            sharedToolTip.SetToolTip(btn, projectPath);
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(35, 65, 119);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(25, 55, 109);
            btn.Click += (s, e) => ShowProjectPaths(projectName, projectPath);

            // Add right-click context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem favoriteItem = new ToolStripMenuItem(
                favoriteProjects.Contains(projectName) ? "Remove from Favorites" : "Add to Favorites");
            favoriteItem.Click += (s, e) =>
            {
                ToggleFavorite("project", projectName);
                // Refresh the current region page
                string currentRegion = "";
                foreach (var regionKv in projectsByRegion)
                {
                    if (regionKv.Value.ContainsKey(projectName))
                    {
                        currentRegion = regionKv.Key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(currentRegion))
                {
                    ShowProjectsInRegion(currentRegion);
                }
            };
            contextMenu.Items.Add(favoriteItem);
            btn.ContextMenuStrip = contextMenu;

            return btn;
        }

        private void FilterProjects(string region, string query)
        {
            if (currentProjectButtons.Count == 0) return;
            if (!projectsByRegion.ContainsKey(region)) return;

            query = (query ?? string.Empty).Trim();
            var list = projectsByRegion[region].ToList();

            var filtered = string.IsNullOrEmpty(query)
                ? list
                : list.Where(kv => kv.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Find the existing FlowLayoutPanel for projects
            FlowLayoutPanel? projectsFlow = null;
            foreach (Control control in contentPanel.Controls)
            {
                if (control is FlowLayoutPanel flow && flow.Name != "contractsFlow")
                {
                    projectsFlow = flow;
                    break;
                }
            }

            if (projectsFlow == null) return; // FlowLayoutPanel not found

            // Clear existing buttons from FlowLayoutPanel
            projectsFlow.Controls.Clear();
            currentProjectButtons.Clear();

            // Add filtered project buttons to FlowLayoutPanel
            foreach (var kv in filtered)
            {
                string projectName = kv.Key;
                string projectPath = kv.Value;

                Button projectButton = CreateProjectButton(projectName, projectPath, Point.Empty, new Size(280, 80));
                projectButton.Margin = new Padding(5, 5, 5, 5);
                projectButton.MaximumSize = new Size(300, 90);
                projectButton.MinimumSize = new Size(250, 70);

                // Update click handler for this specific button
                projectButton.Click += (s, e) => ShowProjectPaths(projectName, projectPath);

                projectsFlow.Controls.Add(projectButton);
                currentProjectButtons.Add(projectButton);
            }

            for (int i = filtered.Count; i < currentProjectButtons.Count; i++)
            {
                currentProjectButtons[i].Visible = false;
            }
        }

        private void ProjectButtonClickDummy(object? sender, EventArgs e)
        {
            if (sender is Button b && b.Tag is ValueTuple<string, string> t)
            {
                ShowProjectPaths(t.Item1, t.Item2);
            }
        }

        private void ShowProjectPaths(string projectName, string projectPath)
        {
            ClearContentPanel();

            // Store current project info for navigation
            currentProjectName = projectName;
            currentProjectPath = projectPath;

            // Add buttons to top bar
            AddTopBarButtons(projectName, projectPath, "project");

            Label titleLabel = new Label
            {
                Text = $"Project Paths: {projectName}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Size = new Size(600, 40),
                Location = new Point(20, 20)
            };
            contentPanel.Controls.Add(titleLabel);

            // DMC and CURVE buttons positioned on the left
            Button dmcButton = new Button
            {
                Text = "DMC",
                Size = new Size(200, 80),
                Location = new Point(20, 80), // Left positioning
                BackColor = Color.FromArgb(52, 152, 219), // Updated to match color scheme
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 16, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            dmcButton.MouseEnter += (s, e) => dmcButton.BackColor = Color.FromArgb(41, 128, 185);
            dmcButton.MouseLeave += (s, e) => dmcButton.BackColor = Color.FromArgb(52, 152, 219);
            dmcButton.Click += (s, e) => ShowSubFolders(projectName, projectPath, "DMC");
            contentPanel.Controls.Add(dmcButton);

            Button curveButton = new Button
            {
                Text = "CURVE",
                Size = new Size(200, 80),
                Location = new Point(240, 80), // Left positioning, next to DMC
                BackColor = Color.FromArgb(52, 152, 219), // Updated to match color scheme
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 16, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            curveButton.MouseEnter += (s, e) => curveButton.BackColor = Color.FromArgb(41, 128, 185);
            curveButton.MouseLeave += (s, e) => curveButton.BackColor = Color.FromArgb(52, 152, 219);
            curveButton.Click += (s, e) => ShowSubFolders(projectName, projectPath, "CURVE");
            contentPanel.Controls.Add(curveButton);
        }

        private void ShowSubFolders(string projectName, string projectPath, string folderType)
        {
            ClearSelections(); // Clear any previous selection for details or items
            ClearContentPanel();

            // Update current folder root BEFORE adding top bar buttons
            currentFolderRoot = Path.Combine(projectPath, folderType);

            // Store current project info for navigation
            currentProjectName = projectName;
            currentProjectPath = projectPath;

            // Add buttons to top bar
            AddTopBarButtons(projectName, projectPath, "subfolder");

            Label titleLabel = new Label
            {
                Text = $"{folderType} - {projectName}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Size = new Size(Math.Min(350, contentPanel.Width - 420), 40), // Smaller responsive width
                Location = new Point(20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            contentPanel.Controls.Add(titleLabel);

            // Update title label size when content panel resizes
            contentPanel.Resize += (s, e) => {
                if (titleLabel != null)
                {
                    titleLabel.Size = new Size(Math.Min(350, contentPanel.Width - 420), 40);
                }
            };

            string folderPath = Path.Combine(projectPath, folderType);
            // currentFolderRoot already set above before AddTopBarButtons

            // Independent items panel at far right (no scroll) - wider for better buttons
            independentItemsPanel = new Panel
            {
                Location = new Point(contentPanel.Width - 400, 10), // Far right positioning with more width
                Size = new Size(380, 600), // Increased width and height for better button layout
                AutoScroll = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            contentPanel.Controls.Add(independentItemsPanel);
            CreateIndependentContractButtonsTop();

            // Contract and item name display moved higher with spacing - English with better styling
            contractNameLabel = new Label
            {
                Text = $"Contract: {(string.IsNullOrWhiteSpace(selectedContractName) ? "[Not Selected]" : selectedContractName)}",
                Font = new Font("Arial", 14, FontStyle.Bold), // Larger font
                ForeColor = Color.FromArgb(25, 55, 109), // Dark blue color
                Size = new Size(Math.Min(500, contentPanel.Width - 420), 30), // Responsive width
                Location = new Point(20, 70), // Moved higher
                BackColor = Color.FromArgb(240, 248, 255), // Light blue background
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            contentPanel.Controls.Add(contractNameLabel);

            itemNameLabel = new Label
            {
                Text = $"Item: {(string.IsNullOrWhiteSpace(selectedIndependentItemName) ? "[Not Selected]" : selectedIndependentItemName)}",
                Font = new Font("Arial", 14, FontStyle.Regular), // Larger font
                ForeColor = Color.FromArgb(25, 55, 109), // Dark blue color
                Size = new Size(Math.Min(500, contentPanel.Width - 420), 30), // Responsive width
                Location = new Point(20, 110), // Moved higher with spacing from contract label
                BackColor = Color.FromArgb(240, 248, 255), // Light blue background
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            contentPanel.Controls.Add(itemNameLabel);

            // 3 dynamic path buttons (blue background, white text, smaller height) - moved up below contract/item labels
            pathsPanel = new Panel
            {
                Location = new Point(20, 150), // Right below the contract/item labels
                Size = new Size(Math.Min(760, contentPanel.Width - 420), 64), // Responsive width
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            contentPanel.Controls.Add(pathsPanel);

            // Update label and panel sizes when content panel resizes
            contentPanel.Resize += (s, e) => {
                if (contractNameLabel != null)
                {
                    contractNameLabel.Size = new Size(Math.Min(500, contentPanel.Width - 420), 30);
                }
                if (itemNameLabel != null)
                {
                    itemNameLabel.Size = new Size(Math.Min(500, contentPanel.Width - 420), 30);
                }
                if (pathsPanel != null)
                {
                    pathsPanel.Size = new Size(Math.Min(760, contentPanel.Width - 420), 64);
                }
            };

            CreateThreePathButtons(); // creates and wires buttons, UpdateThreePathsUI() called inside

            // Contracts grid: squares, natural sort, positioned below the path buttons
            int listStartY = 230; // Positioned below the moved-up path buttons

            try
            {
                if (Directory.Exists(folderPath))
                {
                    var subFolders = Directory.GetDirectories(folderPath)
                                              .OrderBy(Path.GetFileName, new NaturalStringComparer())
                                              .ToArray();

                    if (subFolders.Length > 0)
                    {
                        Label subFoldersLabel = new Label
                        {
                            Text = "Contracts:",
                            Font = new Font("Arial", 14, FontStyle.Bold),
                            ForeColor = Color.FromArgb(40, 40, 40),
                            Size = new Size(300, 30),
                            Location = new Point(20, listStartY)
                        };
                        contentPanel.Controls.Add(subFoldersLabel);

                        // Create responsive FlowLayoutPanel for contracts
                        FlowLayoutPanel contractsFlow = new FlowLayoutPanel
                        {
                            Location = new Point(20, listStartY + 40),
                            Size = new Size(contentPanel.Width - 420, contentPanel.Height - listStartY - 60),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                            FlowDirection = FlowDirection.LeftToRight,
                            WrapContents = true,
                            AutoScroll = true,
                            Padding = new Padding(0, 0, 0, 0)
                        };
                        contentPanel.Controls.Add(contractsFlow);

                        // Calculate button size based on contract count for consistency
                        var gridConfig = CalculateSmartGrid(subFolders.Length, contentPanel.Width - 420);
                        int buttonSize = gridConfig.ButtonSize;
                        int fontSize = CalculateFontSize(buttonSize);

                        foreach (string subFolder in subFolders)
                        {
                            string folderName = Path.GetFileName(subFolder);

                            // Add star if it's a favorite
                            string displayText = favoriteContracts.Contains(folderName) ? $"⭐ {folderName}" : folderName;

                            Button subFolderButton = new Button
                            {
                                Text = displayText,
                                Size = new Size(buttonSize, buttonSize),
                                BackColor = Color.FromArgb(25, 55, 109), // Same color for all
                                ForeColor = Color.White,
                                FlatStyle = FlatStyle.Flat,
                                Font = new Font("Arial", fontSize, FontStyle.Bold),
                                Cursor = Cursors.Hand,
                                TextAlign = ContentAlignment.MiddleCenter,
                                Margin = new Padding(3, 3, 3, 3),
                                AutoSize = false
                            };

                            subFolderButton.MouseEnter += (s, e) => subFolderButton.BackColor = Color.FromArgb(35, 65, 119);
                            subFolderButton.MouseLeave += (s, e) => subFolderButton.BackColor = Color.FromArgb(25, 55, 109);

                            // Select contract (doesn't navigate)
                            subFolderButton.Click += (s, e) =>
                            {
                                SelectContract(subFolder, folderName);
                                selectedIndependentItemName = string.Empty;
                                UpdateThreePathsUI();
                            };

                            // Add right-click context menu for contracts
                            ContextMenuStrip contractContextMenu = new ContextMenuStrip();
                            ToolStripMenuItem contractFavoriteItem = new ToolStripMenuItem(
                                favoriteContracts.Contains(folderName) ? "Remove from Favorites" : "Add to Favorites");
                            contractFavoriteItem.Click += (s, e) =>
                            {
                                ToggleFavorite("contract", folderName);
                                // Refresh the current subfolder page
                                ShowSubFolders(projectName, projectPath, folderType);
                            };
                            contractContextMenu.Items.Add(contractFavoriteItem);
                            subFolderButton.ContextMenuStrip = contractContextMenu;

                            contractsFlow.Controls.Add(subFolderButton);
                        }
                    }
                    else
                    {
                        Label noSubFoldersLabel = new Label
                        {
                            Text = "No contracts found",
                            Font = new Font("Arial", 12),
                            ForeColor = Color.FromArgb(128, 128, 128),
                            Size = new Size(300, 30),
                            Location = new Point(20, listStartY + 40)
                        };
                        contentPanel.Controls.Add(noSubFoldersLabel);
                    }
                }
                else
                {
                    Label notFoundLabel = new Label
                    {
                        Text = $"{folderType} folder not found",
                        Font = new Font("Arial", 12),
                        ForeColor = Color.Red,
                        Size = new Size(300, 30),
                        Location = new Point(20, listStartY)
                    };
                    contentPanel.Controls.Add(notFoundLabel);
                }
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label
                {
                    Text = $"Error reading folder: {ex.Message}",
                    Font = new Font("Arial", 10),
                    ForeColor = Color.Red,
                    Size = new Size(500, 50),
                    Location = new Point(20, listStartY)
                };
                contentPanel.Controls.Add(errorLabel);
            }
        }

        // ------------------------- Selection helpers -------------------------

        private void ClearSelections()
        {
            selectedContractName = string.Empty;
            selectedContractPath = string.Empty;
            selectedIndependentItemName = string.Empty;
        }

        private void SelectContract(string contractFolderPath, string contractName)
        {
            selectedContractName = contractName;
            selectedContractPath = contractFolderPath;

            // Ensure required subfolders exist for this contract
            EnsureRequiredSubfoldersForContract(contractFolderPath);

            UpdateContractItemLabels();
            UpdateThreePathsUI();
        }

        private void SelectIndependentItem(string itemName)
        {
            selectedIndependentItemName = itemName;
            UpdateContractItemLabels();
            UpdateThreePathsUI();
        }

        private void OnIndependentItemClicked(string itemName)
        {
            if (string.IsNullOrWhiteSpace(selectedContractPath))
            {
                MessageBox.Show("Please select a contract first from the contracts list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectIndependentItem(itemName);
        }

        private void UpdateContractItemLabels()
        {
            if (contractNameLabel != null)
            {
                contractNameLabel.Text = $"Contract: {(string.IsNullOrWhiteSpace(selectedContractName) ? "[Not Selected]" : selectedContractName)}";
            }

            if (itemNameLabel != null)
            {
                itemNameLabel.Text = $"Item: {(string.IsNullOrWhiteSpace(selectedIndependentItemName) ? "[Not Selected]" : selectedIndependentItemName)}";
            }
        }



        // ------------------------- Paths UI -------------------------

        private void CreateThreePathButtons()
        {
            pathsPanel.Controls.Clear();

            // Create TableLayoutPanel for responsive path buttons
            TableLayoutPanel pathButtonsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };

            // Set equal column widths (33.33% each)
            pathButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pathButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pathButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            pathButtonsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pathsPanel.Controls.Add(pathButtonsLayout);

            // Removed "Contract" and reversed order: Log, Scan, Soft Copy
            string[] labels = { "Log", "Scan", "Soft Copy" };

            for (int i = 0; i < 3; i++)
            {
                var btn = new Button
                {
                    Text = labels[i],
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(52, 152, 219), // Blue
                    ForeColor = Color.White,                   // White text
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = string.Empty,
                    Margin = new Padding(5, 5, 5, 5)
                };

                int captured = i;
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(41, 128, 185);
                btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(52, 152, 219);
                btn.Click += (s, e) =>
                {
                    string pathOrFile = btn.Tag as string ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(pathOrFile))
                    {
                        // Check if this is the Log button and an item is selected
                        if (captured == 0 && !string.IsNullOrWhiteSpace(selectedContractName) && !string.IsNullOrWhiteSpace(selectedIndependentItemName))
                        {
                            string expectedFileName = $"{selectedIndependentItemName}-({selectedContractName}).xlsx";
                            MessageBox.Show($"Log file not found in the logs folder.\n\nExpected file name: {expectedFileName}",
                                          "Log File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (!string.IsNullOrWhiteSpace(selectedContractName))
                        {
                            string buttonName = labels[captured];
                            MessageBox.Show($"The {buttonName} folder/file is not available.",
                                          "Path Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Please select a contract first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                    OpenPath(pathOrFile, $"Path {captured + 1}");
                };

                pathButtonsLayout.Controls.Add(btn, i, 0);
                pathButtons[i] = btn;
            }

            UpdateThreePathsUI();
        }

        private void UpdateThreePathsUI()
        {
            var paths = ResolveActionPaths();

            for (int i = 0; i < 3; i++)
            {
                var (label, target) = paths[i];

                var btn = pathButtons[i];
                if (btn == null) continue;

                btn.Text = label;
                btn.Tag = target;

                bool exists = !string.IsNullOrWhiteSpace(target) && (Directory.Exists(target) || File.Exists(target));
                btn.Enabled = !string.IsNullOrWhiteSpace(selectedContractName); // Always needs a contract

                if (exists)
                {
                    btn.FlatAppearance.BorderSize = 0;
                }
                else
                {
                    btn.FlatAppearance.BorderSize = 2;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(255, 193, 7);
                }
            }
        }

        // Returns three actions based on current selection (reversed order):
        // 1) logs/<item file>.xlsx (or closest match)
        // 2) scan/<item>
        // 3) soft copy/<item>
        private (string label, string target)[] ResolveActionPaths()
        {
            if (string.IsNullOrWhiteSpace(selectedContractPath))
            {
                return new (string, string)[]
                {
                    ("Log", string.Empty),
                    ("Scan", string.Empty),
                    ("Soft Copy", string.Empty)
                };
            }

            string contractFolder = selectedContractPath;

            // If no independent item selected yet, still present actions but point to parent folders
            bool hasItem = !string.IsNullOrWhiteSpace(selectedIndependentItemName);
            string itemName = selectedIndependentItemName ?? string.Empty;

            string softCopyTarget = Path.Combine(contractFolder, SoftCopyFolderName);
            string scanTarget = Path.Combine(contractFolder, ScanFolderName);
            string logsFolder = Path.Combine(contractFolder, LogsFolderName);

            if (hasItem)
            {
                // Soft copy -> contract/soft copy/<item>
                softCopyTarget = Path.Combine(softCopyTarget, itemName);

                // Scan -> contract/scan/<item>
                scanTarget = Path.Combine(scanTarget, itemName);
            }

            // Logs: file inside logs that matches item name
            string logTarget = logsFolder; // Default to folder
            if (hasItem)
            {
                // If item is selected, only look for the specific file
                string bestFile = FindBestLogFileForItem(logsFolder, itemName);
                if (!string.IsNullOrWhiteSpace(bestFile))
                {
                    logTarget = bestFile; // Point to the specific file
                }
                else
                {
                    logTarget = string.Empty; // Don't point to folder if item selected but file not found
                }
            }
            // If no item selected, logTarget remains as logsFolder

            // Reversed order: Log, Scan, Soft Copy
            return new (string, string)[]
            {
                ("Log", logTarget),
                ("Scan", scanTarget),
                ("Soft Copy", softCopyTarget)
            };
        }

        private string FindBestLogFileForItem(string logsFolder, string itemName)
        {
            try
            {
                if (!Directory.Exists(logsFolder)) return string.Empty;
                if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(selectedContractName)) return string.Empty;

                // Expected format: "item name-(contract name).xlsx"
                string expectedFileName = $"{itemName}-({selectedContractName})";

                // Look for Excel files with the exact format first
                var exactMatches = Directory.EnumerateFiles(logsFolder)
                                           .Where(f =>
                                           {
                                               string fileName = Path.GetFileNameWithoutExtension(f);
                                               return fileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase);
                                           })
                                           .Where(f =>
                                               f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                                               f.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase) ||
                                               f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                                           .OrderBy(f => f)
                                           .ToList();

                // Return the first exact match if found
                if (exactMatches.Any())
                {
                    return exactMatches.First();
                }

                // If no exact match, look for files containing both item name and contract name
                var partialMatches = Directory.EnumerateFiles(logsFolder)
                                             .Where(f =>
                                             {
                                                 string fileName = Path.GetFileNameWithoutExtension(f);
                                                 return fileName.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                        fileName.IndexOf(selectedContractName, StringComparison.OrdinalIgnoreCase) >= 0;
                                             })
                                             .Where(f =>
                                                 f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                                                 f.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase) ||
                                                 f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                                             .OrderBy(f => f)
                                             .ToList();
                                             
                return partialMatches.FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // ------------------------- Smart Grid Calculation -------------------------

        private (int ButtonSize, int Columns, int SpacingX, int SpacingY) CalculateSmartGrid(int itemCount, int availableWidth)
        {
            int buttonSize, columns, spacingX, spacingY;

            if (itemCount >= 30)
            {
                // Many contracts: 8 columns, bigger size
                columns = 8;
                buttonSize = 110; // Increased from 90 to 110
                spacingX = 15;    // Increased spacing
                spacingY = 15;    // Increased spacing
            }
            else if (itemCount >= 20)
            {
                // Medium contracts: 6 columns, medium size
                columns = 6;
                buttonSize = 110;
                spacingX = 15;
                spacingY = 15;
            }
            else if (itemCount >= 10)
            {
                // Few contracts: 4 columns, larger size
                columns = 4;
                buttonSize = 130;
                spacingX = 18;
                spacingY = 18;
            }
            else if (itemCount >= 5)
            {
                // Very few contracts: 3 columns, much larger size
                columns = 3;
                buttonSize = 150;
                spacingX = 20;
                spacingY = 20;
            }
            else
            {
                // Minimal contracts: 2 columns, maximum size
                columns = 2;
                buttonSize = 180;
                spacingX = 25;
                spacingY = 25;
            }

            // Ensure the grid fits within available width
            int totalWidth = columns * buttonSize + (columns - 1) * spacingX;
            if (totalWidth > availableWidth && columns > 1)
            {
                // Reduce columns if needed
                columns = Math.Max(1, availableWidth / (buttonSize + spacingX));

                // Recalculate spacing to center the grid
                if (columns > 1)
                {
                    int remainingWidth = availableWidth - (columns * buttonSize);
                    spacingX = Math.Max(10, remainingWidth / (columns - 1));
                }
            }

            return (buttonSize, columns, spacingX, spacingY);
        }

        private int CalculateFontSize(int buttonSize)
        {
            // Scale font size based on button size
            if (buttonSize >= 180) return 12;      // Very large buttons
            else if (buttonSize >= 150) return 11; // Large buttons
            else if (buttonSize >= 130) return 10; // Medium-large buttons
            else if (buttonSize >= 110) return 9;  // Medium buttons (now includes 30+ contracts)
            else return 8;                         // Small buttons
        }

        // ------------------------- Independent items (top, beside contract box) -------------------------

        private void CreateIndependentContractButtonsTop()
        {
            independentItemsPanel.Controls.Clear();

            // First group
            var group1Items = new List<string>
            {
                "1-Shop dwg",
                "2-As Built",
				"3-Transmittal",
				"4-Letters",
				"5-Quantity Survey",
				"6-RFI",
				"7-RFA",
				"8-Material Submittal",
				"9-Document Submittal",
				"10-MOM",
				"11-CVI",
				"12-Variation Order",
				"13-Variation Instruction"
            };

            // Second group
            var group2Items = new List<string>
            {
                "14-Inspection Request",
                "15-MIR",
				"16-NCR",
				"17-CPR",
				"18-Start New Activity",
				"19-Site Instruction",
				"20-SWI",
				"21-Safety Violation",
				"22-Daily Report"
            };

            // Layout settings - wider buttons for better text fit
            int btnWidth = 120;  // Wider buttons for better text fit
            int btnHeight = 50;  // Taller buttons for better text fit
            int gapX = 5;        // Horizontal spacing between buttons
            int gapY = 5;        // Vertical spacing between rows
            int panelWidth = independentItemsPanel.Width;
            int columnsPerRow = 3; // 3 columns to fit in the wider panel

            // Create first group
            CreateItemGroup(group1Items, 0, btnWidth, btnHeight, gapX, gapY, columnsPerRow);

            // Calculate separator position
            int group1Rows = (int)Math.Ceiling((double)group1Items.Count / columnsPerRow);
            int separatorY = group1Rows * (btnHeight + gapY) + 10;

            // Add separator line
            Panel separator = new Panel
            {
                BackColor = Color.FromArgb(108, 117, 125),
                Size = new Size(panelWidth - 10, 2),
                Location = new Point(5, separatorY)
            };
            independentItemsPanel.Controls.Add(separator);

            // Create second group below separator
            int group2StartY = separatorY + 15;
            CreateItemGroup(group2Items, group2StartY, btnWidth, btnHeight, gapX, gapY, columnsPerRow);
        }

        private void CreateItemGroupFlow(List<string> items, TableLayoutPanel parentLayout, int row)
        {
            FlowLayoutPanel groupFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
                Margin = new Padding(5)
            };

            parentLayout.Controls.Add(groupFlow, 0, row);

            foreach (string itemName in items)
            {
                var itemButton = new Button
                {
                    Text = itemName,
                    Size = new Size(120, 50),
                    BackColor = Color.FromArgb(52, 152, 219), // Updated to match color scheme
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 9, FontStyle.Bold), // Larger font for better readability
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(3, 3, 3, 3),
                    AutoSize = false
                };

                itemButton.MouseEnter += (s, e) => itemButton.BackColor = Color.FromArgb(41, 128, 185);
                itemButton.MouseLeave += (s, e) => itemButton.BackColor = Color.FromArgb(52, 152, 219);
                itemButton.Click += (s, e) => OnIndependentItemClicked(itemName);

                groupFlow.Controls.Add(itemButton);
            }
        }

        private void CreateItemGroup(List<string> items, int startY, int btnWidth, int btnHeight, int gapX, int gapY, int columnsPerRow)
        {
            int x = 0;
            int y = startY;

            for (int i = 0; i < items.Count; i++)
            {
                string itemName = items[i];

                // Move to next row if needed
                if (i > 0 && i % columnsPerRow == 0)
                {
                    x = 0;
                    y += btnHeight + gapY;
                }

                var itemButton = new Button
                {
                    Text = itemName,
                    Size = new Size(btnWidth, btnHeight),
                    Location = new Point(x, y),
                    BackColor = Color.FromArgb(52, 152, 219), // Updated to match color scheme
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Arial", 9, FontStyle.Bold), // Larger font for better readability
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                itemButton.MouseEnter += (s, e) => itemButton.BackColor = Color.FromArgb(41, 128, 185);
                itemButton.MouseLeave += (s, e) => itemButton.BackColor = Color.FromArgb(52, 152, 219);
                itemButton.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(selectedContractPath))
                    {
                        MessageBox.Show("Please select a contract first from the contracts list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    SelectIndependentItem(itemName);
                };

                independentItemsPanel.Controls.Add(itemButton);

                x += btnWidth + gapX;
            }
        }

        // ------------------------- Settings -------------------------

        private void ShowSettings()
        {
            ClearContentPanel();

            Label titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Size = new Size(200, 40),
                Location = new Point(20, 20)
            };
            contentPanel.Controls.Add(titleLabel);

            Button reloadButton = new Button
            {
                Text = "Reload Projects",
                Size = new Size(220, 50),
                Location = new Point(20, 80),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            reloadButton.MouseEnter += (s, e) => reloadButton.BackColor = Color.FromArgb(41, 128, 185);
            reloadButton.MouseLeave += (s, e) => reloadButton.BackColor = Color.FromArgb(52, 152, 219);
            reloadButton.Click += (s, e) =>
            {
                LoadProjectsFromCSV();
                EnsureRequiredSubfoldersForAllContracts();
                MessageBox.Show("Projects reloaded and paths reviewed/created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            contentPanel.Controls.Add(reloadButton);

            Button openCsvButton = new Button
            {
                Text = "Open Projects File",
                Size = new Size(220, 50),
                Location = new Point(260, 80),
                BackColor = Color.FromArgb(25, 55, 109), // Changed to dark blue to match color scheme
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            openCsvButton.MouseEnter += (s, e) => openCsvButton.BackColor = Color.FromArgb(35, 65, 119);
            openCsvButton.MouseLeave += (s, e) => openCsvButton.BackColor = Color.FromArgb(25, 55, 109);
            openCsvButton.Click += (s, e) =>
            {
                string csvPath = Path.Combine(Application.StartupPath, "Projects.csv");
                OpenPath(csvPath, "Projects File");
            };
            contentPanel.Controls.Add(openCsvButton);

            Label instructionLabel = new Label
            {
                Text = "Reloads the projects list from CSV.\nAlso automatically creates missing contract folders: soft copy, scan, Logs.",
                Font = new Font("Arial", 11),
                ForeColor = Color.FromArgb(100, 100, 100),
                Size = new Size(520, 120),
                Location = new Point(20, 150)
            };
            contentPanel.Controls.Add(instructionLabel);
        }

        private void EnsureRequiredSubfoldersForAllContracts()
        {
            try
            {
                int createdFolders = 0;
                foreach (var regionKv in projectsByRegion)
                {
                    foreach (var projectKv in regionKv.Value)
                    {
                        string projectPath = projectKv.Value;

                        // Check both DMC and CURVE if exist
                        createdFolders += EnsureRequiredSubfoldersUnderRoot(Path.Combine(projectPath, "DMC"));
                        createdFolders += EnsureRequiredSubfoldersUnderRoot(Path.Combine(projectPath, "CURVE"));
                    }
                }

                if (createdFolders > 0)
                {
                    MessageBox.Show($"Created {createdFolders} missing folders successfully!", "Folders Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while creating paths: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int EnsureRequiredSubfoldersUnderRoot(string rootPath)
        {
            int createdCount = 0;
            try
            {
                if (!Directory.Exists(rootPath)) return 0;

                var contractDirs = Directory.GetDirectories(rootPath);
                foreach (var contractDir in contractDirs)
                {
                    string soft = Path.Combine(contractDir, SoftCopyFolderName);
                    string scan = Path.Combine(contractDir, ScanFolderName);
                    string logs = Path.Combine(contractDir, LogsFolderName);

                    if (!Directory.Exists(soft))
                    {
                        Directory.CreateDirectory(soft);
                        createdCount++;
                    }
                    if (!Directory.Exists(scan))
                    {
                        Directory.CreateDirectory(scan);
                        createdCount++;
                    }
                    if (!Directory.Exists(logs))
                    {
                        Directory.CreateDirectory(logs);
                        createdCount++;
                    }
                }
            }
            catch { }
            return createdCount;
        }

        // Method to ensure folders for a specific contract when it's added
        private void EnsureRequiredSubfoldersForContract(string contractPath)
        {
            try
            {
                if (!Directory.Exists(contractPath)) return;

                string soft = Path.Combine(contractPath, SoftCopyFolderName);
                string scan = Path.Combine(contractPath, ScanFolderName);
                string logs = Path.Combine(contractPath, LogsFolderName);

                if (!Directory.Exists(soft)) Directory.CreateDirectory(soft);
                if (!Directory.Exists(scan)) Directory.CreateDirectory(scan);
                if (!Directory.Exists(logs)) Directory.CreateDirectory(logs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folders for contract: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ------------------------- Utilities -------------------------

        private void OpenPath(string path, string pathType)
        {
            try
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (Directory.Exists(path))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = $"\"{path}\"",
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", $"\"{path}\"");
                    }
                    else
                    {
                        Process.Start("xdg-open", $"\"{path}\"");
                    }
                }
                else
                {
                    MessageBox.Show($"Path not found:\n{path}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot open {pathType} path:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearContentPanel()
        {
            contentPanel.Controls.Clear();
            // Reset scroll to top on every page open
            contentPanel.AutoScrollPosition = new Point(0, 0);
        }

        private string GetSettingsFilePath()
        {
            string dir = Application.UserAppDataPath;
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
            return Path.Combine(dir, SettingsFileName);
        }

        private void LoadUserSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    var loaded = JsonSerializer.Deserialize<UserSettings>(json);
                    if (loaded != null) appSettings = loaded;
                }
            }
            catch { }
        }

        private void SaveUserSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                var json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { }
        }

        // ------------------------- Favorites Management -------------------------

        private void LoadFavorites()
        {
            try
            {
                string path = GetFavoritesFilePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    var favData = JsonSerializer.Deserialize<FavoritesData>(json);
                    if (favData != null)
                    {
                        favoriteRegions = favData.Regions ?? new HashSet<string>();
                        favoriteProjects = favData.Projects ?? new HashSet<string>();
                        favoriteContracts = favData.Contracts ?? new HashSet<string>();
                    }
                }
            }
            catch { }
        }

        private void SaveFavorites()
        {
            try
            {
                string path = GetFavoritesFilePath();
                var favData = new FavoritesData
                {
                    Regions = favoriteRegions,
                    Projects = favoriteProjects,
                    Contracts = favoriteContracts
                };
                var json = JsonSerializer.Serialize(favData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { }
        }

        private string GetFavoritesFilePath()
        {
            string dir = Application.UserAppDataPath;
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
            return Path.Combine(dir, FavoritesFileName);
        }

        private void ToggleFavorite(string type, string item)
        {
            switch (type.ToLower())
            {
                case "region":
                    if (favoriteRegions.Contains(item))
                        favoriteRegions.Remove(item);
                    else
                        favoriteRegions.Add(item);
                    break;
                case "project":
                    if (favoriteProjects.Contains(item))
                        favoriteProjects.Remove(item);
                    else
                        favoriteProjects.Add(item);
                    break;
                case "contract":
                    if (favoriteContracts.Contains(item))
                        favoriteContracts.Remove(item);
                    else
                        favoriteContracts.Add(item);
                    break;
            }
            SaveFavorites();
        }

        // Natural sort comparer (numbers inside names sorted numerically)
        private class NaturalStringComparer : IComparer<string>
        {
            private static readonly Regex ChunkRegex = new Regex(@"\d+|\D+", RegexOptions.Compiled);

            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                var xChunks = ChunkRegex.Matches(x);
                var yChunks = ChunkRegex.Matches(y);
                int len = Math.Min(xChunks.Count, yChunks.Count);

                for (int i = 0; i < len; i++)
                {
                    string a = xChunks[i].Value;
                    string b = yChunks[i].Value;

                    bool aIsNum = int.TryParse(a, out int ai);
                    bool bIsNum = int.TryParse(b, out int bi);

                    if (aIsNum && bIsNum)
                    {
                        int cmp = ai.CompareTo(bi);
                        if (cmp != 0) return cmp;
                    }
                    else
                    {
                        int cmp = string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
                        if (cmp != 0) return cmp;
                    }
                }

                return x.Length.CompareTo(y.Length);
            }
        }
    }
}