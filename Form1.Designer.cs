namespace ProjectManagerDesigner;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        sidebarPanel = new Panel();
        sidebarTableLayout = new TableLayoutPanel();
        settingsButton = new Button();
        projectsButton = new Button();
        homeButton = new Button();
        logoPanel = new Panel();
        logoPictureBox = new PictureBox();
        companyNameLabel = new Label();
        toggleButton = new Button();
        contentPanel = new Panel();
        welcomeLabel = new Label();
        topSearchPanel = new Panel();
        sidebarPanel.SuspendLayout();
        sidebarTableLayout.SuspendLayout();
        logoPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
        contentPanel.SuspendLayout();
        SuspendLayout();
        //
        // sidebarPanel
        //
        sidebarPanel.BackColor = Color.FromArgb(25, 55, 109);
        sidebarPanel.Controls.Add(sidebarTableLayout);
        sidebarPanel.Controls.Add(settingsButton); // Add settings button directly to sidebar panel
        sidebarPanel.Dock = DockStyle.Left;
        sidebarPanel.Location = new Point(0, 0);
        sidebarPanel.Margin = new Padding(3, 4, 3, 4);
        sidebarPanel.Name = "sidebarPanel";
        sidebarPanel.Size = new Size(251, 933);
        sidebarPanel.TabIndex = 0;
        //
        // sidebarTableLayout
        //
        sidebarTableLayout.BackColor = Color.Transparent;
        sidebarTableLayout.ColumnCount = 1;
        sidebarTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sidebarTableLayout.Controls.Add(logoPanel, 0, 0);
        sidebarTableLayout.Controls.Add(homeButton, 0, 1);
        sidebarTableLayout.Controls.Add(projectsButton, 0, 2);
        sidebarTableLayout.Controls.Add(toggleButton, 0, 3);
        sidebarTableLayout.Dock = DockStyle.Top;
        sidebarTableLayout.Location = new Point(0, 0);
        sidebarTableLayout.Name = "sidebarTableLayout";
        sidebarTableLayout.Padding = new Padding(11, 13, 11, 13);
        sidebarTableLayout.RowCount = 4;
        sidebarTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // Logo
        sidebarTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Home
        sidebarTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Projects
        sidebarTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Toggle
        sidebarTableLayout.Size = new Size(251, 360); // Fixed height to leave space for settings button
        sidebarTableLayout.TabIndex = 0;
        //
        // settingsButton
        //
        settingsButton.BackColor = Color.FromArgb(35, 65, 119);
        settingsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; // Anchor to bottom with margins
        settingsButton.FlatStyle = FlatStyle.Flat;
        settingsButton.Font = new Font("Arial", 11F, FontStyle.Bold);
        settingsButton.ForeColor = Color.White;
        settingsButton.Location = new Point(11, 840); // Will be adjusted in code
        settingsButton.Margin = new Padding(11, 4, 11, 20); // Bottom margin for spacing
        settingsButton.Size = new Size(229, 73); // Same size as other buttons
        settingsButton.Name = "settingsButton";
        settingsButton.TabIndex = 2;
        settingsButton.Text = "⚙️  Settings";
        settingsButton.TextAlign = ContentAlignment.MiddleLeft;
        settingsButton.UseVisualStyleBackColor = false;
        //
        // projectsButton
        //
        projectsButton.BackColor = Color.FromArgb(35, 65, 119);
        projectsButton.Dock = DockStyle.Fill;
        projectsButton.FlatStyle = FlatStyle.Flat;
        projectsButton.Font = new Font("Arial", 11F, FontStyle.Bold);
        projectsButton.ForeColor = Color.White;
        projectsButton.Margin = new Padding(3, 4, 3, 4);
        projectsButton.Name = "projectsButton";
        projectsButton.TabIndex = 1;
        projectsButton.Text = "📁  Projects";
        projectsButton.TextAlign = ContentAlignment.MiddleLeft;
        projectsButton.UseVisualStyleBackColor = false;
        //
        // homeButton
        //
        homeButton.BackColor = Color.FromArgb(35, 65, 119);
        homeButton.Dock = DockStyle.Fill;
        homeButton.FlatStyle = FlatStyle.Flat;
        homeButton.Font = new Font("Arial", 11F, FontStyle.Bold);
        homeButton.ForeColor = Color.White;
        homeButton.Margin = new Padding(3, 4, 3, 4);
        homeButton.Name = "homeButton";
        homeButton.TabIndex = 0;
        homeButton.Text = "🏠  Home";
        homeButton.TextAlign = ContentAlignment.MiddleLeft;
        homeButton.UseVisualStyleBackColor = false;
        //
        // logoPanel
        //
        logoPanel.BackColor = Color.Transparent;
        logoPanel.Controls.Add(logoPictureBox);
        logoPanel.Controls.Add(companyNameLabel);
        logoPanel.Dock = DockStyle.Fill;
        logoPanel.Margin = new Padding(3, 4, 3, 4);
        logoPanel.Name = "logoPanel";
        logoPanel.TabIndex = 5;
        // 
        // logoPictureBox
        // 
        logoPictureBox.BackColor = Color.Transparent;
        logoPictureBox.Location = new Point(10, 10);
        logoPictureBox.Margin = new Padding(3, 4, 3, 4);
        logoPictureBox.Name = "logoPictureBox";
        logoPictureBox.Size = new Size(200, 90); // Larger size to fill the logo panel
        logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        logoPictureBox.TabIndex = 0;
        logoPictureBox.TabStop = false;
        try
        {
            logoPictureBox.Image = Image.FromFile("logo.png");
        }
        catch
        {
            // If logo file not found, leave empty
        }
        //
        // companyNameLabel
        //
        companyNameLabel.Font = new Font("Arial", 12F, FontStyle.Bold);
        companyNameLabel.ForeColor = Color.White;
        companyNameLabel.Location = new Point(69, 7);
        companyNameLabel.Name = "companyNameLabel";
        companyNameLabel.Size = new Size(154, 67);
        companyNameLabel.TabIndex = 1;
        companyNameLabel.Text = "";
        companyNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        companyNameLabel.Visible = false; // Hide company name, show logo only
        //
        // toggleButton
        //
        toggleButton.BackColor = Color.FromArgb(255, 193, 7);
        toggleButton.Dock = DockStyle.Fill;
        toggleButton.FlatStyle = FlatStyle.Flat;
        toggleButton.Font = new Font("Arial", 16F, FontStyle.Bold);
        toggleButton.ForeColor = Color.White;
        toggleButton.Margin = new Padding(3, 4, 3, 4);
        toggleButton.Name = "toggleButton";
        toggleButton.TabIndex = 4;
        toggleButton.Text = "☰";
        toggleButton.UseVisualStyleBackColor = false;
        //
        // contentPanel
        //
        contentPanel.AutoScroll = true;
        contentPanel.BackColor = Color.White;
        contentPanel.Controls.Add(welcomeLabel);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(251, 80);
        contentPanel.Margin = new Padding(3, 4, 3, 4);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(23, 27, 23, 27);
        contentPanel.Size = new Size(1120, 853);
        contentPanel.TabIndex = 1;
        // 
        // welcomeLabel
        // 
        welcomeLabel.Dock = DockStyle.Fill;
        welcomeLabel.Font = new Font("Arial", 16F, FontStyle.Bold);
        welcomeLabel.ForeColor = Color.FromArgb(64, 64, 64);
        welcomeLabel.Location = new Point(23, 27);
        welcomeLabel.Name = "welcomeLabel";
        welcomeLabel.Size = new Size(1074, 799);
        welcomeLabel.TabIndex = 0;
        welcomeLabel.Text = "مرحباً بك في مدير المشاريع\r\n\r\nاختر 'Projects' من القائمة الجانبية للبدء";
        welcomeLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // topSearchPanel
        //
        topSearchPanel.BackColor = Color.FromArgb(25, 55, 109);
        topSearchPanel.Dock = DockStyle.Top;
        topSearchPanel.Location = new Point(251, 0);
        topSearchPanel.Margin = new Padding(3, 4, 3, 4);
        topSearchPanel.Name = "topSearchPanel";
        topSearchPanel.Size = new Size(1120, 80);
        topSearchPanel.TabIndex = 2;
        //
        // Form1
        //
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        ClientSize = new Size(1371, 933);
        Controls.Add(contentPanel);
        Controls.Add(topSearchPanel);
        Controls.Add(sidebarPanel);
        try
        {
            Icon = new Icon("logo.ico");
        }
        catch
        {
            // If logo.ico file not found, use default icon
        }
        Margin = new Padding(3, 4, 3, 4);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Mountain View Projects";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(800, 600);
        sidebarPanel.ResumeLayout(false);
        logoPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
        contentPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel sidebarPanel;
    private System.Windows.Forms.TableLayoutPanel sidebarTableLayout;
    private System.Windows.Forms.Button homeButton;
    private System.Windows.Forms.Button projectsButton;
    private System.Windows.Forms.Button settingsButton;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Label welcomeLabel;
    private System.Windows.Forms.Panel logoPanel;
    private System.Windows.Forms.PictureBox logoPictureBox;
    private System.Windows.Forms.Label companyNameLabel;
    private System.Windows.Forms.Button toggleButton;
    private System.Windows.Forms.Panel topSearchPanel;

    #endregion
}