using System.Drawing;
using System.Media;
using WindowTitleRenamer.Localization;
using WindowTitleRenamer.Settings;
using WindowTitleRenamer.UI.Controls;

namespace WindowTitleRenamer.UI;

internal sealed class MainForm : Form
{
    private const int WmSysCommand = 0x0112;
    private const int WsExComposited = 0x02000000;
    private const long ScMinimize = 0xF020;
    private const long SystemCommandMask = 0xFFF0;

    private static readonly Color BackgroundColor = Color.FromArgb(18, 21, 28);
    private static readonly Color CardColor = Color.FromArgb(27, 31, 40);
    private static readonly Color InputColor = Color.FromArgb(34, 39, 50);
    private static readonly Color BorderColor = Color.FromArgb(55, 62, 76);
    private static readonly Color PrimaryTextColor = Color.FromArgb(238, 241, 247);
    private static readonly Color SecondaryTextColor = Color.FromArgb(155, 164, 181);
    private static readonly Color AccentColor = Color.FromArgb(255, 173, 38);
    private static readonly Color AccentHoverColor = Color.FromArgb(255, 188, 67);
    private static readonly Color SuccessColor = Color.FromArgb(82, 201, 137);
    private static readonly Color ErrorColor = Color.FromArgb(255, 105, 120);
    private static readonly Color WarningColor = Color.FromArgb(255, 191, 76);

    private readonly WindowService _windowService;
    private readonly PersistentRenamer _persistentRenamer;
    private readonly AppSettings _settings;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly Icon? _applicationIcon;

    private readonly Label _headerTitle = new();
    private readonly Label _headerSubtitle = new();
    private readonly Label _languageLabel = new();
    private readonly ComboBox _languageSelector = new();
    private readonly ThemedButton _hideToTrayButton = new();
    private readonly Label _windowListTitle = new();
    private readonly TextBox _searchBox = new();
    private readonly ThemedButton _refreshButton = new();
    private readonly BufferedDataGridView _windowGrid = new();
    private readonly Label _windowLoadingLabel = new();
    private readonly Label _windowCountLabel = new();
    private readonly Label _renamePanelTitle = new();
    private readonly Label _selectedWindowLabel = new();
    private readonly Label _currentTitleLabel = new();
    private readonly TextBox _currentTitleBox = new();
    private readonly Label _newTitleLabel = new();
    private readonly TextBox _newTitleBox = new();
    private readonly CheckBox _keepTitleCheckBox = new();
    private readonly Label _keepDescriptionLabel = new();
    private readonly ThemedButton _applyButton = new();
    private readonly ThemedButton _stopKeepingButton = new();
    private readonly Label _selectionHintLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 15000,
        InitialDelay = 500,
        ReshowDelay = 100,
        ShowAlways = true,
    };

    private readonly ContextMenuStrip _trayMenu = new();
    private readonly ToolStripMenuItem _trayShowItem = new();
    private readonly ToolStripMenuItem _trayExitItem = new();
    private readonly NotifyIcon _notifyIcon = new();

    private List<WindowInfo> _allWindows = [];
    private WindowInfo? _selectedWindow;
    private bool _updatingGrid;
    private bool _applyingLanguage;
    private bool _refreshInProgress;
    private bool _hasLoadedWindowList;
    private bool _exitRequested;
    private bool _trayHintShown;

    private Strings L => Strings.Current;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExComposited;
            return parameters;
        }
    }

    public MainForm(
        WindowService windowService,
        PersistentRenamer persistentRenamer,
        AppSettings settings)
    {
        _windowService = windowService;
        _persistentRenamer = persistentRenamer;
        _settings = settings;

        _applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };

        SuspendLayout();
        try
        {
            ConfigureForm();
            BuildLayout();
            ConfigureTrayIcon();
            WireEvents();
            SelectConfiguredLanguage();
            ApplyLocalization();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void ConfigureForm()
    {
        Text = "Window Title Renamer";
        ClientSize = new Size(1180, 680);
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BackgroundColor;
        ForeColor = PrimaryTextColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        KeyPreview = true;

        if (_applicationIcon is not null)
        {
            Icon = _applicationIcon;
        }
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            BackColor = BackgroundColor,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 18, 24, 14),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildContent(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        TableLayoutPanel header = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        TableLayoutPanel titleLayout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
        };
        titleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _headerTitle.AutoSize = true;
        _headerTitle.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
        _headerTitle.ForeColor = PrimaryTextColor;
        _headerTitle.Margin = new Padding(0, 0, 0, 2);

        _headerSubtitle.AutoSize = true;
        _headerSubtitle.Font = new Font("Segoe UI", 10F);
        _headerSubtitle.ForeColor = SecondaryTextColor;
        _headerSubtitle.Margin = new Padding(2, 0, 0, 10);

        titleLayout.Controls.Add(_headerTitle, 0, 0);
        titleLayout.Controls.Add(_headerSubtitle, 0, 1);

        FlowLayoutPanel actions = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 13, 0, 0),
            Margin = Padding.Empty,
        };

        _languageLabel.AutoSize = true;
        _languageLabel.ForeColor = SecondaryTextColor;
        _languageLabel.Margin = new Padding(0, 8, 8, 0);

        _languageSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageSelector.Name = "LanguageSelector";
        _languageSelector.FlatStyle = FlatStyle.Flat;
        _languageSelector.BackColor = InputColor;
        _languageSelector.ForeColor = PrimaryTextColor;
        _languageSelector.Width = 140;
        _languageSelector.Margin = new Padding(0, 2, 12, 0);
        _languageSelector.Items.AddRange(["简体中文", "English"]);

        ConfigureSecondaryButton(_hideToTrayButton, 148);
        _hideToTrayButton.Name = "HideToTrayButton";
        _hideToTrayButton.Margin = new Padding(0, 0, 0, 0);

        actions.Controls.Add(_languageLabel);
        actions.Controls.Add(_languageSelector);
        actions.Controls.Add(_hideToTrayButton);

        header.Controls.Add(titleLayout, 0, 0);
        header.Controls.Add(actions, 1, 0);
        return header;
    }

    private Control BuildContent()
    {
        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12),
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));

        Control listCard = BuildWindowListCard();
        listCard.Margin = new Padding(0, 0, 9, 0);
        Control editorCard = BuildRenameCard();
        editorCard.Margin = new Padding(9, 0, 0, 0);

        content.Controls.Add(listCard, 0, 0);
        content.Controls.Add(editorCard, 1, 0);
        return content;
    }

    private Control BuildWindowListCard()
    {
        Panel card = CreateCard();

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20, 17, 20, 14),
            Margin = Padding.Empty,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _windowListTitle.AutoSize = true;
        _windowListTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        _windowListTitle.ForeColor = PrimaryTextColor;
        _windowListTitle.Margin = new Padding(0, 0, 0, 8);

        TableLayoutPanel searchLayout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        ConfigureInput(_searchBox);
        _searchBox.Name = "SearchBox";
        _searchBox.Margin = new Padding(0, 0, 10, 10);

        ConfigureSecondaryButton(_refreshButton, 98);
        _refreshButton.Name = "RefreshButton";
        _refreshButton.Dock = DockStyle.Fill;
        _refreshButton.Margin = new Padding(0, 0, 0, 10);

        searchLayout.Controls.Add(_searchBox, 0, 0);
        searchLayout.Controls.Add(_refreshButton, 1, 0);

        ConfigureWindowGrid();

        Panel gridHost = new()
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            Margin = Padding.Empty,
        };
        _windowLoadingLabel.Dock = DockStyle.Fill;
        _windowLoadingLabel.BackColor = CardColor;
        _windowLoadingLabel.ForeColor = SecondaryTextColor;
        _windowLoadingLabel.Font = new Font("Segoe UI", 10F);
        _windowLoadingLabel.TextAlign = ContentAlignment.MiddleCenter;
        gridHost.Controls.Add(_windowGrid);
        gridHost.Controls.Add(_windowLoadingLabel);
        _windowLoadingLabel.BringToFront();

        _windowCountLabel.AutoSize = true;
        _windowCountLabel.ForeColor = SecondaryTextColor;
        _windowCountLabel.MinimumSize = new Size(0, _windowCountLabel.Font.Height);
        _windowCountLabel.Margin = new Padding(2, 10, 0, 0);

        layout.Controls.Add(_windowListTitle, 0, 0);
        layout.Controls.Add(searchLayout, 0, 1);
        layout.Controls.Add(gridHost, 0, 2);
        layout.Controls.Add(_windowCountLabel, 0, 3);
        card.Controls.Add(layout);
        return card;
    }

    private void ConfigureWindowGrid()
    {
        _windowGrid.Dock = DockStyle.Fill;
        _windowGrid.Name = "WindowGrid";
        _windowGrid.Margin = Padding.Empty;
        _windowGrid.BackgroundColor = CardColor;
        _windowGrid.BorderStyle = BorderStyle.None;
        _windowGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _windowGrid.GridColor = BorderColor;
        _windowGrid.RowHeadersVisible = false;
        _windowGrid.ReadOnly = true;
        _windowGrid.MultiSelect = false;
        _windowGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _windowGrid.AllowUserToAddRows = false;
        _windowGrid.AllowUserToDeleteRows = false;
        _windowGrid.AllowUserToResizeRows = false;
        _windowGrid.AutoGenerateColumns = false;
        _windowGrid.EnableHeadersVisualStyles = false;
        _windowGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _windowGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _windowGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardColor,
            ForeColor = PrimaryTextColor,
            SelectionBackColor = Color.FromArgb(61, 55, 39),
            SelectionForeColor = PrimaryTextColor,
            Font = new Font("Segoe UI", 9.5F),
            Padding = new Padding(6, 0, 6, 0),
        };
        _windowGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InputColor,
            ForeColor = SecondaryTextColor,
            SelectionBackColor = InputColor,
            SelectionForeColor = SecondaryTextColor,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            Padding = new Padding(6, 0, 6, 0),
        };
        _windowGrid.ColumnHeadersHeight = _windowGrid.ColumnHeadersDefaultCellStyle.Font.Height + 18;
        _windowGrid.RowTemplate.Height = _windowGrid.DefaultCellStyle.Font.Height + 18;

        _windowGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TitleColumn",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        _windowGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StatusColumn",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 96,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            },
        });
        _windowGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "HandleColumn",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 170,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F),
            },
        });
    }

    private Control BuildRenameCard()
    {
        Panel card = CreateCard();

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 9,
            Padding = new Padding(24, 17, 24, 18),
            Margin = Padding.Empty,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _renamePanelTitle.AutoSize = true;
        _renamePanelTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        _renamePanelTitle.ForeColor = PrimaryTextColor;
        _renamePanelTitle.Margin = new Padding(0, 0, 0, 8);

        _selectedWindowLabel.Dock = DockStyle.Top;
        _selectedWindowLabel.AutoSize = false;
        _selectedWindowLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        _selectedWindowLabel.Height = _selectedWindowLabel.Font.Height + 8;
        _selectedWindowLabel.ForeColor = SecondaryTextColor;
        _selectedWindowLabel.AutoEllipsis = true;
        _selectedWindowLabel.TextAlign = ContentAlignment.TopLeft;
        _selectedWindowLabel.Margin = new Padding(0, 0, 0, 8);

        ConfigureFieldLabel(_currentTitleLabel);
        ConfigureReadOnlyInput(_currentTitleBox);
        _currentTitleBox.Name = "CurrentTitleBox";
        _currentTitleBox.Margin = new Padding(0, 0, 0, 12);

        ConfigureFieldLabel(_newTitleLabel);
        ConfigureInput(_newTitleBox);
        _newTitleBox.Name = "NewTitleBox";
        _newTitleBox.Margin = new Padding(0, 0, 0, 12);

        Control keepPanel = BuildKeepTitlePanel();
        Control buttonPanel = BuildEditorButtons();

        _selectionHintLabel.Dock = DockStyle.Top;
        _selectionHintLabel.AutoSize = false;
        _selectionHintLabel.Font = new Font("Segoe UI", 9.5F);
        _selectionHintLabel.Height = _selectionHintLabel.Font.Height * 2 + 8;
        _selectionHintLabel.ForeColor = SecondaryTextColor;
        _selectionHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _selectionHintLabel.Margin = new Padding(0, 12, 0, 0);

        layout.Controls.Add(_renamePanelTitle, 0, 0);
        layout.Controls.Add(_selectedWindowLabel, 0, 1);
        layout.Controls.Add(_currentTitleLabel, 0, 2);
        layout.Controls.Add(_currentTitleBox, 0, 3);
        layout.Controls.Add(_newTitleLabel, 0, 4);
        layout.Controls.Add(_newTitleBox, 0, 5);
        layout.Controls.Add(keepPanel, 0, 6);
        layout.Controls.Add(buttonPanel, 0, 7);
        layout.Controls.Add(_selectionHintLabel, 0, 8);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildKeepTitlePanel()
    {
        TableLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _keepTitleCheckBox.AutoSize = true;
        _keepTitleCheckBox.Name = "KeepTitleCheckBox";
        _keepTitleCheckBox.ForeColor = PrimaryTextColor;
        _keepTitleCheckBox.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        _keepTitleCheckBox.Margin = new Padding(0, 3, 0, 3);
        _keepTitleCheckBox.FlatStyle = FlatStyle.Flat;

        _keepDescriptionLabel.AutoSize = false;
        _keepDescriptionLabel.Dock = DockStyle.Top;
        _keepDescriptionLabel.Font = new Font("Segoe UI", 9.5F);
        _keepDescriptionLabel.Height = _keepDescriptionLabel.Font.Height * 2 + 6;
        _keepDescriptionLabel.ForeColor = SecondaryTextColor;
        _keepDescriptionLabel.TextAlign = ContentAlignment.TopLeft;
        _keepDescriptionLabel.Margin = new Padding(22, 0, 0, 8);

        panel.Controls.Add(_keepTitleCheckBox, 0, 0);
        panel.Controls.Add(_keepDescriptionLabel, 0, 1);
        return panel;
    }

    private Control BuildEditorButtons()
    {
        TableLayoutPanel buttons = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

        ConfigurePrimaryButton(_applyButton);
        _applyButton.Name = "ApplyButton";
        _applyButton.Dock = DockStyle.Fill;
        _applyButton.Margin = new Padding(0, 0, 6, 8);

        ConfigureSecondaryButton(_stopKeepingButton, 130);
        _stopKeepingButton.Name = "StopKeepingButton";
        _stopKeepingButton.Dock = DockStyle.Fill;
        _stopKeepingButton.Margin = new Padding(6, 0, 0, 8);

        buttons.Controls.Add(_applyButton, 0, 0);
        buttons.Controls.Add(_stopKeepingButton, 1, 0);
        return buttons;
    }

    private Control BuildStatusBar()
    {
        _statusLabel.AutoSize = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.BackColor = BackgroundColor;
        _statusLabel.ForeColor = SecondaryTextColor;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.Padding = new Padding(0, 8, 0, 2);
        _statusLabel.Margin = Padding.Empty;
        return _statusLabel;
    }

    private static Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }

    private static void ConfigureFieldLabel(Label label)
    {
        label.AutoSize = true;
        label.ForeColor = SecondaryTextColor;
        label.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        label.Margin = new Padding(0, 4, 0, 0);
    }

    private static void ConfigureInput(TextBox textBox)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.BackColor = InputColor;
        textBox.ForeColor = PrimaryTextColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 10.5F);
    }

    private static void ConfigureReadOnlyInput(TextBox textBox)
    {
        ConfigureInput(textBox);
        textBox.ReadOnly = true;
        textBox.TabStop = false;
    }

    private static void ConfigurePrimaryButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = AccentColor;
        button.ForeColor = Color.FromArgb(30, 25, 15);
        button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        button.MinimumSize = new Size(0, button.Font.Height + 18);
        button.Padding = new Padding(14, 5, 14, 5);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        if (button is ThemedButton themedButton)
        {
            themedButton.DisabledBackColor = Color.FromArgb(73, 65, 43);
            themedButton.DisabledForeColor = Color.FromArgb(142, 132, 105);
            themedButton.DisabledBorderColor = Color.FromArgb(73, 65, 43);
        }
        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = AccentHoverColor;
            }
        };
        button.MouseLeave += (_, _) => button.BackColor = AccentColor;
    }

    private static void ConfigureSecondaryButton(Button button, int width)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        button.BackColor = InputColor;
        button.ForeColor = PrimaryTextColor;
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        button.MinimumSize = new Size(width, button.Font.Height + 16);
        button.Padding = new Padding(12, 4, 12, 4);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        if (button is ThemedButton themedButton)
        {
            themedButton.DisabledBackColor = Color.FromArgb(31, 35, 44);
            themedButton.DisabledForeColor = Color.FromArgb(94, 101, 115);
            themedButton.DisabledBorderColor = Color.FromArgb(47, 52, 64);
        }
        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = BorderColor;
            }
        };
        button.MouseLeave += (_, _) => button.BackColor = InputColor;
    }

    private void ConfigureTrayIcon()
    {
        _trayMenu.BackColor = CardColor;
        _trayMenu.ForeColor = PrimaryTextColor;
        _trayMenu.ShowImageMargin = false;
        _trayMenu.Padding = new Padding(4);
        _trayMenu.Items.AddRange([_trayShowItem, new ToolStripSeparator(), _trayExitItem]);

        _notifyIcon.Icon = _applicationIcon ?? SystemIcons.Application;
        _notifyIcon.Text = "Window Title Renamer";
        _notifyIcon.ContextMenuStrip = _trayMenu;
        _notifyIcon.Visible = true;
    }

    private void WireEvents()
    {
        Load += (_, _) => FitToWorkingArea();
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        Resize += MainForm_Resize;

        _refreshTimer.Tick += async (_, _) => await RefreshWindowListAsync(false);
        _refreshButton.Click += async (_, _) => await RefreshWindowListAsync(true);
        _searchBox.TextChanged += (_, _) => ApplyWindowFilter(_selectedWindow?.Hwnd);
        _windowGrid.SelectionChanged += (_, _) => HandleGridSelectionChanged();
        _windowGrid.CellDoubleClick += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex >= 0 && _newTitleBox.Enabled)
            {
                _newTitleBox.Focus();
                _newTitleBox.SelectAll();
            }
        };
        _newTitleBox.TextChanged += (_, _) => UpdateEditorButtons();
        _newTitleBox.KeyDown += NewTitleBox_KeyDown;
        _applyButton.Click += async (_, _) => await ApplyRenameAsync();
        _stopKeepingButton.Click += (_, _) => StopKeepingSelectedWindow();
        _hideToTrayButton.Click += (_, _) => HideToTray();
        _languageSelector.SelectedIndexChanged += (_, _) => ChangeLanguage();

        _trayShowItem.Click += async (_, _) => await RestoreFromTrayAsync();
        _trayExitItem.Click += (_, _) => ExitApplication();
        _notifyIcon.DoubleClick += async (_, _) => await RestoreFromTrayAsync();
    }

    private void FitToWorkingArea()
    {
        Rectangle workingArea = Screen.FromHandle(Handle).WorkingArea;
        const int screenMargin = 16;
        int availableWidth = Math.Max(720, workingArea.Width - screenMargin * 2);
        int availableHeight = Math.Max(540, workingArea.Height - screenMargin * 2);

        MinimumSize = new Size(
            Math.Min(MinimumSize.Width, availableWidth),
            Math.Min(MinimumSize.Height, availableHeight));
        Size = new Size(
            Math.Min(Width, availableWidth),
            Math.Min(Height, availableHeight));
        Location = new Point(
            workingArea.Left + (workingArea.Width - Width) / 2,
            workingArea.Top + (workingArea.Height - Height) / 2);
    }

    private void SelectConfiguredLanguage()
    {
        _applyingLanguage = true;
        _languageSelector.SelectedIndex = _settings.Language.Equals(
            "zh-CN", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        _applyingLanguage = false;
    }

    private void ChangeLanguage()
    {
        if (_applyingLanguage || _languageSelector.SelectedIndex < 0)
        {
            return;
        }

        _settings.Language = _languageSelector.SelectedIndex == 0 ? "zh-CN" : "en";
        Strings.SetLanguage(_settings.Language);
        ApplyLocalization();

        try
        {
            SettingsManager.Save(_settings);
        }
        catch (Exception exception)
        {
            ShowStatus(string.Format(L.SettingsSaveFailed, exception.Message), StatusKind.Error);
        }
    }

    private void ApplyLocalization()
    {
        Text = L.AppTitle;
        _headerTitle.Text = L.AppTitle;
        _headerSubtitle.Text = L.AppSubtitle;
        _languageLabel.Text = L.LanguageLabel;
        _hideToTrayButton.Text = L.HideToTray;
        _windowListTitle.Text = L.WindowListTitle;
        _windowLoadingLabel.Text = L.LoadingWindows;
        _searchBox.PlaceholderText = L.SearchPlaceholder;
        _refreshButton.Text = L.Refresh;
        _windowGrid.Columns["TitleColumn"].HeaderText = L.ColumnTitle;
        _windowGrid.Columns["StatusColumn"].HeaderText = L.ColumnStatus;
        _windowGrid.Columns["HandleColumn"].HeaderText = L.ColumnHandle;
        _renamePanelTitle.Text = L.RenamePanelTitle;
        _currentTitleLabel.Text = L.CurrentTitle;
        _newTitleLabel.Text = L.NewTitle;
        _newTitleBox.PlaceholderText = L.NewTitlePlaceholder;
        _keepTitleCheckBox.Text = L.KeepTitle;
        _keepDescriptionLabel.Text = L.KeepDescription;
        _applyButton.Text = L.Apply;
        _stopKeepingButton.Text = L.StopKeeping;
        _selectionHintLabel.Text = L.SelectWindowHint;
        _trayShowItem.Text = L.TrayShow;
        _trayExitItem.Text = L.TrayExit;

        UpdateSelectedWindow(_selectedWindow, false);
        ApplyWindowFilter(_selectedWindow?.Hwnd);
        ShowStatus(
            _refreshInProgress && !_hasLoadedWindowList ? L.LoadingWindows : L.Ready,
            StatusKind.Neutral);
    }

    private async void MainForm_Shown(object? sender, EventArgs eventArgs)
    {
        ShowStatus(L.LoadingWindows, StatusKind.Neutral);
        Refresh();
        await RefreshWindowListAsync(true);

        if (!IsDisposed && !Disposing && Visible)
        {
            _refreshTimer.Start();
        }
    }

    private async Task RefreshWindowListAsync(bool announce)
    {
        if (_refreshInProgress || IsDisposed || Disposing)
        {
            return;
        }

        _refreshInProgress = true;
        _refreshButton.Enabled = false;

        if (!_hasLoadedWindowList)
        {
            _windowLoadingLabel.Visible = true;
            _windowLoadingLabel.BringToFront();
            ShowStatus(L.LoadingWindows, StatusKind.Neutral);
        }

        try
        {
            List<WindowInfo> windows = await Task.Run(
                () => _windowService.ListOpenWindows().ToList());

            if (IsDisposed || Disposing)
            {
                return;
            }

            _allWindows = windows;
            _hasLoadedWindowList = true;
            ApplyWindowFilter(_selectedWindow?.Hwnd);
            _windowLoadingLabel.Visible = false;

            if (announce)
            {
                int activeRules = _persistentRenamer.ListRules().Count;
                ShowStatus(
                    string.Format(L.LoadedWindows, _allWindows.Count, activeRules),
                    StatusKind.Neutral);
            }
        }
        catch (Exception exception)
        {
            if (!IsDisposed && !Disposing)
            {
                _windowLoadingLabel.Visible = false;
                ShowStatus(string.Format(L.RefreshFailedError, exception.Message), StatusKind.Error);
            }
        }
        finally
        {
            _refreshInProgress = false;
            if (!IsDisposed && !Disposing)
            {
                _refreshButton.Enabled = true;
            }
        }
    }

    private void ApplyWindowFilter(IntPtr? preferredHandle)
    {
        string searchText = _searchBox.Text.Trim();
        List<WindowInfo> filteredWindows = string.IsNullOrEmpty(searchText)
            ? [.. _allWindows]
            : _allWindows
                .Where(window => window.Title.Contains(
                    searchText, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        IReadOnlyDictionary<IntPtr, string> rules = _persistentRenamer.ListRules();

        _updatingGrid = true;
        _windowGrid.SuspendLayout();
        try
        {
            _windowGrid.Rows.Clear();
            DataGridViewRow? preferredRow = null;
            DataGridViewRow[] rows = new DataGridViewRow[filteredWindows.Count];

            for (int index = 0; index < filteredWindows.Count; index++)
            {
                WindowInfo window = filteredWindows[index];
                bool isKeeping = rules.ContainsKey(window.Hwnd);
                DataGridViewRow row = new();
                row.CreateCells(
                    _windowGrid,
                    window.Title,
                    isKeeping ? L.Keeping : string.Empty,
                    window.HandleText);
                row.Tag = window;
                row.Cells[0].ToolTipText = window.Title;

                if (isKeeping)
                {
                    row.Cells[1].Style.ForeColor = AccentColor;
                }

                if (preferredHandle.HasValue && window.Hwnd == preferredHandle.Value)
                {
                    preferredRow = row;
                }

                rows[index] = row;
            }

            if (rows.Length > 0)
            {
                _windowGrid.Rows.AddRange(rows);
            }

            _windowGrid.ClearSelection();
            DataGridViewRow? rowToSelect = preferredRow;
            if (rowToSelect is null && _windowGrid.Rows.Count > 0)
            {
                rowToSelect = _windowGrid.Rows[0];
            }

            if (rowToSelect is not null)
            {
                rowToSelect.Selected = true;
                _windowGrid.CurrentCell = rowToSelect.Cells[0];
                UpdateSelectedWindow((WindowInfo)rowToSelect.Tag!, false);
            }
            else
            {
                UpdateSelectedWindow(null, false);
            }
        }
        finally
        {
            _windowGrid.ResumeLayout();
            _updatingGrid = false;
        }

        _windowCountLabel.Text = !_hasLoadedWindowList
            ? string.Empty
            : string.IsNullOrEmpty(searchText)
                ? string.Format(L.WindowCount, _allWindows.Count)
                : string.Format(L.FilteredWindowCount, filteredWindows.Count, _allWindows.Count);

        if (_hasLoadedWindowList && filteredWindows.Count == 0)
        {
            ShowStatus(
                _allWindows.Count == 0 ? L.NoWindows : L.NoSearchResults,
                StatusKind.Warning);
        }
    }

    private void HandleGridSelectionChanged()
    {
        if (_updatingGrid)
        {
            return;
        }

        WindowInfo? selected = _windowGrid.SelectedRows.Count > 0
            ? _windowGrid.SelectedRows[0].Tag as WindowInfo
            : null;
        UpdateSelectedWindow(selected, false);
    }

    private void UpdateSelectedWindow(WindowInfo? window, bool forceEditorReset)
    {
        bool selectionChanged = _selectedWindow?.Hwnd != window?.Hwnd;
        _selectedWindow = window;

        if (window is null)
        {
            _selectedWindowLabel.Text = L.NoSelection;
            _selectedWindowLabel.ForeColor = SecondaryTextColor;
            _toolTip.SetToolTip(_selectedWindowLabel, string.Empty);
            _toolTip.SetToolTip(_currentTitleBox, string.Empty);
            _currentTitleBox.Clear();
            _newTitleBox.Clear();
            _newTitleBox.Enabled = false;
            _keepTitleCheckBox.Checked = false;
            _keepTitleCheckBox.Enabled = false;
            _applyButton.Enabled = false;
            _stopKeepingButton.Enabled = false;
            _selectionHintLabel.Visible = true;
            return;
        }

        IReadOnlyDictionary<IntPtr, string> rules = _persistentRenamer.ListRules();
        bool isKeeping = rules.TryGetValue(window.Hwnd, out string? keptTitle);

        _selectedWindowLabel.Text = window.Title;
        _selectedWindowLabel.ForeColor = PrimaryTextColor;
        _currentTitleBox.Text = window.Title;
        _toolTip.SetToolTip(_selectedWindowLabel, window.Title);
        _toolTip.SetToolTip(_currentTitleBox, window.Title);
        _newTitleBox.Enabled = true;
        _keepTitleCheckBox.Enabled = true;

        if (selectionChanged || forceEditorReset)
        {
            _newTitleBox.Text = isKeeping ? keptTitle : window.Title;
            _keepTitleCheckBox.Checked = isKeeping;
        }

        _stopKeepingButton.Enabled = isKeeping;
        _selectionHintLabel.Visible = false;
        UpdateEditorButtons();
    }

    private void UpdateEditorButtons()
    {
        _applyButton.Enabled = _selectedWindow is not null
            && _newTitleBox.Enabled
            && !string.IsNullOrWhiteSpace(_newTitleBox.Text);
    }

    private async Task ApplyRenameAsync()
    {
        if (_selectedWindow is null)
        {
            return;
        }

        string newTitle = _newTitleBox.Text;
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            ShowStatus(L.EmptyTitleError, StatusKind.Error);
            _newTitleBox.Focus();
            return;
        }

        WindowInfo selectedWindow = _selectedWindow;
        RenameResult result = WindowService.Rename(selectedWindow.Hwnd, newTitle);
        if (!result.Succeeded)
        {
            if (result.WindowNoLongerExists)
            {
                ShowStatus(L.WindowGoneError, StatusKind.Error);
                await RefreshWindowListAsync(false);
                if (IsDisposed || Disposing)
                {
                    return;
                }
            }
            else
            {
                ShowStatus(
                    string.Format(L.RenameFailedError, result.ErrorCode),
                    StatusKind.Error);
            }

            SystemSounds.Exclamation.Play();
            return;
        }

        bool wasKeeping = _persistentRenamer.ListRules().ContainsKey(selectedWindow.Hwnd);
        if (_keepTitleCheckBox.Checked)
        {
            _persistentRenamer.AddOrUpdate(selectedWindow.Hwnd, newTitle);
        }
        else
        {
            _persistentRenamer.Remove(selectedWindow.Hwnd);
        }

        _selectedWindow = new WindowInfo(selectedWindow.Hwnd, newTitle);
        _currentTitleBox.Text = newTitle;
        _stopKeepingButton.Enabled = _keepTitleCheckBox.Checked;

        if (!string.IsNullOrWhiteSpace(_searchBox.Text)
            && !newTitle.Contains(
                _searchBox.Text.Trim(),
                StringComparison.CurrentCultureIgnoreCase))
        {
            _searchBox.Clear();
        }

        string status = string.Format(L.RenameSucceeded, selectedWindow.Title, newTitle);
        if (_keepTitleCheckBox.Checked)
        {
            status = $"{status} {L.KeepEnabled}";
        }
        else if (wasKeeping)
        {
            status = $"{status} {L.KeepDisabled}";
        }

        await RefreshWindowListAsync(false);
        if (IsDisposed || Disposing)
        {
            return;
        }

        ShowStatus(status, StatusKind.Success);
    }

    private void StopKeepingSelectedWindow()
    {
        if (_selectedWindow is null)
        {
            return;
        }

        _persistentRenamer.Remove(_selectedWindow.Hwnd);
        _keepTitleCheckBox.Checked = false;
        _stopKeepingButton.Enabled = false;
        ApplyWindowFilter(_selectedWindow.Hwnd);
        ShowStatus(L.KeepDisabled, StatusKind.Success);
    }

    private async void NewTitleBox_KeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode == Keys.Enter && _applyButton.Enabled)
        {
            eventArgs.SuppressKeyPress = true;
            await ApplyRenameAsync();
        }
    }

    private void ShowStatus(string message, StatusKind kind)
    {
        _statusLabel.Text = $"●  {message}";
        _statusLabel.ForeColor = kind switch
        {
            StatusKind.Success => SuccessColor,
            StatusKind.Error => ErrorColor,
            StatusKind.Warning => WarningColor,
            _ => SecondaryTextColor,
        };
    }

    private void MainForm_Resize(object? sender, EventArgs eventArgs)
    {
        if (WindowState == FormWindowState.Minimized && Visible)
        {
            HideToTray();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (!_exitRequested && eventArgs.CloseReason == CloseReason.UserClosing)
        {
            eventArgs.Cancel = true;
            HideToTray();
        }
    }

    private void HideToTray()
    {
        if (!Visible)
        {
            return;
        }

        _refreshTimer.Stop();
        Hide();

        if (WindowState == FormWindowState.Minimized)
        {
            // Keep a programmatically minimized form in its normal state so
            // restoring it does not render the minimized window first.
            WindowState = FormWindowState.Normal;
        }

        if (!_trayHintShown)
        {
            _notifyIcon.BalloonTipTitle = L.TrayRunningTitle;
            _notifyIcon.BalloonTipText = L.TrayRunningMessage;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(2500);
            _trayHintShown = true;
        }
    }

    private async Task RestoreFromTrayAsync()
    {
        Show();
        Activate();
        BringToFront();
        await RefreshWindowListAsync(false);
        if (!IsDisposed && !Disposing && Visible)
        {
            _refreshTimer.Start();
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmSysCommand &&
            (message.WParam.ToInt64() & SystemCommandMask) == ScMinimize)
        {
            // Hide before Windows starts its minimize animation. Processing the
            // default command first makes the form flash once before it vanishes.
            HideToTray();
            return;
        }

        base.WndProc(ref message);
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        _refreshTimer.Stop();
        _notifyIcon.Visible = false;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            _toolTip.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayMenu.Dispose();
        }

        base.Dispose(disposing);

        if (disposing)
        {
            _applicationIcon?.Dispose();
        }
    }

    private enum StatusKind
    {
        Neutral,
        Success,
        Warning,
        Error,
    }
}
