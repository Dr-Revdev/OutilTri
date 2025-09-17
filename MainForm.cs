using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.Json;


public class MainForm : Form
{

    private TextBox txtFolder;
    private Button btnBrowse;
    private Button btnPreview;
    private ListView lvPreview;
    private Button btnApply;
    private List<(string from, string to)> lastMoves = new();
    private Button btnUndo;

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
        btnBrowse.Click += BtnBrowse_Click;

        // default path
        var downloads = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
        if (System.IO.Directory.Exists(downloads)) txtFolder.Text = downloads;

        //btnPreview and lvPreview
        btnPreview = new Button { Text = "Aperçu", Left = 90, Top = 55, Width = 120, Height = 28 };
        Controls.Add(btnPreview);
        lvPreview = new ListView { Left = 20, Top = 100, Width = 840, View = View.Details, FullRowSelect = true, GridLines = true };
        lvPreview.Height = ClientSize.Height - lvPreview.Top - 40;
        lvPreview.Columns.Add("Fichier", 400);
        lvPreview.Columns.Add("Extention", 120);
        lvPreview.Columns.Add("Destination (proposée)", 200);
        lvPreview.Columns.Add("État", 80);
        Controls.Add(lvPreview);
        btnPreview.Click += BtnPreview_Click;

        //btnApply
        btnApply = new Button { Text = "Appliquer", Left = 220, Top = 55, Width = 120, Height = 28, Enabled = false };
        Controls.Add(btnApply);
        btnApply.Click += BtnApply_Click;

        //btnUndo
        btnUndo = new Button { Text = "Annuler le dernier tri", Left = 350, Top = 55, Width = 180, Height = 28, Enabled = false };
        Controls.Add(btnUndo);
        btnUndo.Click += BtnUndo_Click;

    }

    private void BtnBrowse_Click(object? s, System.EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = System.IO.Directory.Exists(txtFolder.Text) ? txtFolder.Text : "" };
        if (dlg.ShowDialog(this) == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
    }

    private void BtnPreview_Click(object? s, EventArgs e)
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
            lvPreview.Items.Add(new ListViewItem(new[] { name, ext, dest, "" }) { Tag = path });
        }
        btnApply.Enabled = lvPreview.Items.Count > 0;
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

    private static string EnsureUniquePath(string basePath)
    {
        if (!File.Exists(basePath)) return basePath;
        var dir = Path.GetDirectoryName(basePath)!;
        var name = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);
        int i = 1; string candidate;
        do candidate = Path.Combine(dir, $"{name} ({i++}){ext}");
        while (File.Exists(candidate));
        return candidate;
    }

    private void BtnApply_Click(object? s, EventArgs e)
    {
        var root = txtFolder.Text;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            MessageBox.Show(this, "Dossier invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int moved = 0, failed = 0;
        lastMoves.Clear();

        foreach (ListViewItem it in lvPreview.Items)
        {
            var src = it.Tag as string;
            if (string.IsNullOrEmpty(src))
            {
                if (it.SubItems.Count >= 4) it.SubItems[3].Text = "Erreur";
                failed++;
                continue;
            }

            var destSub = it.SubItems[2].Text;
            if (string.IsNullOrWhiteSpace(destSub))
            {
                if (it.SubItems.Count >= 4) it.SubItems[3].Text = "_";
                continue;
            }
            try
            {
                var destDir = Path.Combine(root, destSub);

                var srcDir = Path.GetDirectoryName(src);
                if (string.Equals(srcDir ?? "", destDir, StringComparison.OrdinalIgnoreCase))
                {
                    if (it.SubItems.Count >= 4) it.SubItems[3].Text = "Déjà";
                    continue;
                }

                Directory.CreateDirectory(destDir);

                var destPath = Path.Combine(destDir, Path.GetFileName(src));
                destPath = EnsureUniquePath(destPath);

                File.Move(src, destPath);
                if (it.SubItems.Count >= 4) it.SubItems[3].Text = "OK";
                lastMoves.Add((from: destPath, to: src));
                moved++;
            }
            catch
            {
                if (it.SubItems.Count >= 4) it.SubItems[3].Text = "Erreur";
                failed++;
            }

        }
        MessageBox.Show(this, $"Déplacés : {moved}\nErreurs : {failed}", "Tri terminé", MessageBoxButtons.OK, MessageBoxIcon.Information);
        btnUndo.Enabled = moved > 0;

        BtnPreview_Click(this, EventArgs.Empty);
        btnApply.Enabled = false;

    }

    private void BtnUndo_Click(object? s, EventArgs e)
    {
        if (lastMoves.Count == 0)
        {
            MessageBox.Show(this, "Rien à annuler.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int undone = 0, failed = 0;

        for (int i = lastMoves.Count - 1; i >= 0; i--)
        {
            var (from, to) = lastMoves[i];
            try
            {
                if (!File.Exists(from)) { failed++; continue; }

                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                var back = EnsureUniquePath(to);
                File.Move(from, back);
                undone++;
            }
            catch { failed++; }
        }

        lastMoves.Clear();
        btnUndo.Enabled = false;

        MessageBox.Show(this, $"Annulés : {undone}\nErreurs : {failed}", "Annulation terminée", MessageBoxButtons.OK, MessageBoxIcon.Information);
        btnPreview.PerformClick();
    }
}