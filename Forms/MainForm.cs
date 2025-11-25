using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using TriDownload.Models;
using TriDownload.Services;

namespace TriDownload.Forms;

public class MainForm : Form
{
    private readonly IFileSortingService _sortingService;
    
    private TextBox _folderPathTextBox = null!;
    private Button _browseFolderButton = null!;
    private Button _previewButton = null!;
    private ListView _previewListView = null!;
    private Button _applySortingButton = null!;
    private Button _undoButton = null!;
    private TabControl _mainTabControl = null!;
    private TabPage _sortingTab = null!, _settingsTab = null!;
    private DataGridView _rulesGrid = null!;
    private Button _saveRulesButton = null!, _resetRulesButton = null!;
    
    private List<(string CurrentPath, string OriginalPath)> _lastMovements = new();
    private List<FilePreviewInfo> _currentPreview = new();

    public MainForm() : this(new FileSortingService())
    {
    }

    public MainForm(IFileSortingService sortingService)
    {
        _sortingService = sortingService;
        InitializeComponents();
        _sortingService.LoadRules();
        LoadRulesIntoGrid();
    }

    private void InitializeComponents()
    {
        Text = "Trieur de Fichiers";
        ClientSize = new Size(900, 535);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterScreen;

        _mainTabControl = new TabControl { Dock = DockStyle.Fill };
        _sortingTab = new TabPage("Tri");
        _settingsTab = new TabPage("Paramètres");
        _mainTabControl.TabPages.Add(_sortingTab);
        _mainTabControl.TabPages.Add(_settingsTab);
        _mainTabControl.Selecting += OnTabSelecting;
        Controls.Add(_mainTabControl);

        InitializeSortingTab();
        InitializeSettingsTab();
    }

    private void InitializeSortingTab()
    {
        _previewListView = new ListView 
        { 
            Dock = DockStyle.Fill,
            View = View.Details, 
            FullRowSelect = true, 
            GridLines = true
        };
        _previewListView.Columns.Add("Fichier", 400);
        _previewListView.Columns.Add("Extension", 120);
        _previewListView.Columns.Add("Destination (proposée)", 200);
        _previewListView.Columns.Add("État", 80);
        _sortingTab.Controls.Add(_previewListView);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 95 };
        
        var folderLabel = new Label { Text = "Dossier", Left = 20, Top = 20, Width = 60 };
        topPanel.Controls.Add(folderLabel);
        
        _browseFolderButton = new Button { Text = "Parcourir...", Width = 130, Height = 26, Top = 16, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _browseFolderButton.Click += OnBrowseFolderClick;
        topPanel.Controls.Add(_browseFolderButton);
        
        _folderPathTextBox = new TextBox { Left = 90, Top = 18, Height = 26, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        topPanel.Controls.Add(_folderPathTextBox);
        
        topPanel.SizeChanged += (s, e) => {
            _browseFolderButton.Left = topPanel.Width - _browseFolderButton.Width - 20;
            _folderPathTextBox.Width = _browseFolderButton.Left - _folderPathTextBox.Left - 10;
        };

        _previewButton = new Button { Text = "Aperçu", Left = 90, Top = 55, Width = 120, Height = 28 };
        _previewButton.Click += OnPreviewClick;
        topPanel.Controls.Add(_previewButton);

        _applySortingButton = new Button { Text = "Appliquer", Left = 220, Top = 55, Width = 120, Height = 28, Enabled = false };
        _applySortingButton.Click += OnApplySortingClick;
        topPanel.Controls.Add(_applySortingButton);

        _undoButton = new Button { Text = "Annuler le dernier tri", Left = 350, Top = 55, Width = 180, Height = 28, Enabled = false };
        _undoButton.Click += OnUndoClick;
        topPanel.Controls.Add(_undoButton);
        
        _sortingTab.Controls.Add(topPanel);
    }

    private void InitializeSettingsTab()
    {
        _rulesGrid = new DataGridView 
        { 
            Dock = DockStyle.Fill,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false
        };
        
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            HeaderText = "Extensions (séparées par virgule)", 
            DataPropertyName = "Extensions", 
            Width = 400 
        });
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            HeaderText = "Dossier de destination", 
            DataPropertyName = "DestinationFolder", 
            Width = 400 
        });
        _settingsTab.Controls.Add(_rulesGrid);

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        _saveRulesButton = new Button 
        { 
            Text = "Sauvegarder les r\u00e8gles", 
            Left = 20, 
            Top = 10, 
            Width = 180, 
            Height = 30
        };
        _saveRulesButton.Click += OnSaveRulesClick;
        bottomPanel.Controls.Add(_saveRulesButton);

        _resetRulesButton = new Button 
        { 
            Text = "R\u00e9initialiser par d\u00e9faut", 
            Left = 210, 
            Top = 10, 
            Width = 180, 
            Height = 30
        };
        _resetRulesButton.Click += OnResetRulesClick;
        bottomPanel.Controls.Add(_resetRulesButton);
        _settingsTab.Controls.Add(bottomPanel);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
        var rulesLabel = new Label 
        { 
            Text = "R\u00e8gles de tri (extensions \u2192 dossier)", 
            Left = 20, 
            Top = 20, 
            Width = 300, 
            Height = 20 
        };
        topPanel.Controls.Add(rulesLabel);
        _settingsTab.Controls.Add(topPanel);
    }

    private void LoadRulesIntoGrid()
    {
        _rulesGrid.DataSource = new BindingList<SortingRule>(_sortingService.Rules.ToList());
    }

    private void OnBrowseFolderClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog 
        { 
            SelectedPath = Directory.Exists(_folderPathTextBox.Text) ? _folderPathTextBox.Text : string.Empty 
        };
        
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _folderPathTextBox.Text = dialog.SelectedPath;
    }

    private void OnPreviewClick(object? sender, EventArgs e)
    {
        _previewListView.Items.Clear();
        _currentPreview.Clear();

        var folderPath = _folderPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            MessageBox.Show(this, "Dossier invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentPreview = _sortingService.PreviewFiles(folderPath);

        foreach (var file in _currentPreview)
        {
            _previewListView.Items.Add(new ListViewItem(new[] 
            { 
                file.FileName, 
                file.Extension, 
                file.DestinationFolder, 
                file.Status 
            }) 
            { 
                Tag = file 
            });
        }

        _applySortingButton.Enabled = _previewListView.Items.Count > 0;
    }

    private void OnApplySortingClick(object? sender, EventArgs e)
    {
        var folderPath = _folderPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            MessageBox.Show(this, "Dossier invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _lastMovements.Clear();
        var result = _sortingService.ApplySorting(folderPath, _currentPreview);
        _lastMovements = result.Movements;

        foreach (ListViewItem item in _previewListView.Items)
        {
            if (item.Tag is FilePreviewInfo file && item.SubItems.Count >= 4)
            {
                item.SubItems[3].Text = file.Status;
            }
        }

        MessageBox.Show(this, 
            $"Déplacés : {result.MovedCount}\nErreurs : {result.FailedCount}", 
            "Tri terminé", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Information);
        
        _undoButton.Enabled = result.MovedCount > 0;
        OnPreviewClick(this, EventArgs.Empty);
        _applySortingButton.Enabled = false;
    }

    private void OnUndoClick(object? sender, EventArgs e)
    {
        if (_lastMovements.Count == 0)
        {
            MessageBox.Show(this, "Rien à annuler.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = _sortingService.UndoSorting(_lastMovements);
        _lastMovements.Clear();
        _undoButton.Enabled = false;

        MessageBox.Show(this, 
            $"Annulés : {result.RestoredCount}\nErreurs : {result.FailedCount}", 
            "Annulation terminée", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Information);
        
        _previewButton.PerformClick();
    }

    private void OnTabSelecting(object? sender, TabControlCancelEventArgs e)
    {
        if (e.TabPage == _settingsTab)
        {
            using var passwordDialog = new Form
            {
                Text = "Accès aux paramètres",
                ClientSize = new Size(320, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "Mot de passe :",
                Left = 20,
                Top = 20,
                Width = 100
            };

            var passwordBox = new TextBox
            {
                Left = 130,
                Top = 18,
                Width = 160,
                UseSystemPasswordChar = true
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 130,
                Top = 60,
                Width = 75
            };

            var cancelButton = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Left = 215,
                Top = 60,
                Width = 75
            };

            passwordDialog.Controls.AddRange(new Control[] { label, passwordBox, okButton, cancelButton });
            passwordDialog.AcceptButton = okButton;
            passwordDialog.CancelButton = cancelButton;

            if (passwordDialog.ShowDialog(this) != DialogResult.OK || passwordBox.Text != "admin")
            {
                e.Cancel = true;
                if (passwordDialog.DialogResult == DialogResult.OK)
                {
                    MessageBox.Show(this, "Mot de passe incorrect.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }

    private void OnSaveRulesClick(object? sender, EventArgs e)
    {
        try
        {
            if (_rulesGrid.DataSource is BindingList<SortingRule> bindingList)
            {
                _sortingService.SaveRules(bindingList.ToList());
                MessageBox.Show(this, "Règles sauvegardées avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnResetRulesClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, 
            "Réinitialiser les règles par défaut ?", 
            "Confirmation", 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _sortingService.InitializeDefaultRules();
            LoadRulesIntoGrid();
            OnSaveRulesClick(sender, e);
        }
    }
}
