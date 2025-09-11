using System.Drawing;
using System.Windows.Forms;


public class MainForm : Form
{

    private TextBox txtFolder;
    private Button btnBrowse;
    private Button btnPreview;
    private ListView lvPreview;

    public MainForm()
    {
        // Init window
        Text = "Tri Download";
        ClientSize = new Size(900, 520);
        StartPosition = FormStartPosition.CenterScreen;

        // lblFolder and btnBrowse
        var lblFolder = new Label { Text = "Dossier", Left = 20, Top = 20, Width = 60 };
        txtFolder = new TextBox { Left = 90, Top = 18, Width = 640 };
        btnBrowse = new Button { Text = "Parcourir...", Left = 740, Top = 16, Width = 130, Height = 26 };
        Controls.Add(lblFolder);
        Controls.Add(txtFolder);
        Controls.Add(btnBrowse);

        // default path
        var downloads = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
        if (System.IO.Directory.Exists(downloads)) txtFolder.Text = downloads;

        //btnPreview and lvPreview
        btnPreview = new Button { Text = "Aperçu", Left = 90, Top = 55, Width = 120, Height = 28 };
        Controls.Add(btnPreview);
        lvPreview = new ListView { Left = 20, Top = 100, Width = 840, View = View.Details, FullRowSelect = true, GridLines = true };
        lvPreview.Height = ClientSize.Height - lvPreview.Top - 40;
        lvPreview.Columns.Add("Fichier", 400);
        lvPreview.Columns.Add("Extension", 120);
        lvPreview.Columns.Add("Destination (proposée)", 200);
        Controls.Add(lvPreview);


        btnBrowse.Click += BtnBrowse_Click;
        btnPreview.Click += btnPreview_Click;
    }

    private void BtnBrowse_Click(object? s, System.EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = System.IO.Directory.Exists(txtFolder.Text) ? txtFolder.Text : "" };
        if (dlg.ShowDialog(this) == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
    }

    private void btnPreview_Click(object? s, EventArgs e)
    {
        lvPreview.Items.Clear();

        var root = txtFolder.Text;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            MessageBox.Show(this, "Dossier invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(path);
            var ext = Path.GetExtension(path);
            if (ext.StartsWith(".")) ext = ext[1..];

            var dest = SuggestSubfolderForExtension(ext);
            lvPreview.Items.Add(new ListViewItem(new[] { name, ext, dest }) { Tag = path });
        }
    }

    private static string SuggestSubfolderForExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return "_SansExtention";
        ext = ext.ToLowerInvariant();

        string[] images = { "jpg", "jpeg", "png", "gif", "bmp", "webp", "tiff", "heic" };
        string[] docs = { "pdf", "doc", "docx", "txt", "rtf", "odt", "xls", "xlsx", "csv", "ppt", "pptx" };
        string[] audio = { "mp3", "wav", "flac", "m4a", "ogg", "aac" };
        string[] video = { "mp4", "mkv", "avi", "mov", "wmv", "webm" };
        string[] arch = { "zip", "7z", "rar", "tar", "gz", "bz2" };
        string[] exe = { "exe", "msi", "msix", "bat", "cmd", "ps1" };

        if (Array.Exists(images, e => e == ext)) return "Images";
        if (Array.Exists(docs, e => e == ext)) return "Documents";
        if (Array.Exists(audio, e => e == ext)) return "Audio";
        if (Array.Exists(video, e => e == ext)) return "Vidéos";
        if (Array.Exists(arch, e => e == ext)) return "Archives";
        if (Array.Exists(exe, e => e == ext)) return "Installateurs";

        return "_Autres";
    }
}