using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal sealed class CaptureForm : Form
{
    private readonly PictureBox preview = new PictureBox();
    private readonly Label placeholder = new Label();
    private readonly Label status = new Label();
    private readonly Button save = new Button();
    private readonly Button crop = new Button();
    private readonly Button resize = new Button();
    private readonly Button restore = new Button();
    private readonly ToolTip tip = new ToolTip();
    private Bitmap capture;
    private Bitmap original;
    private readonly string picturesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    public CaptureForm()
    {
        Text = "Salvar captura";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(900, 620);
        MinimumSize = new Size(680, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 248, 252);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5 };
        foreach (int height in new[] { 42, 38, 58 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "Sua captura, no formato que você precisa.", Font = new Font("Segoe UI Semibold", 19), Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(new Label { Text = "Faça a captura com Print Screen e cole aqui com Ctrl+V.", ForeColor = Color.FromArgb(75, 85, 105), Dock = DockStyle.Fill }, 0, 1);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true };
        layout.RowStyles[2] = new RowStyle(SizeType.AutoSize);
        layout.Controls.Add(toolbar, 0, 2);
        var paste = MakeButton("Colar  (Ctrl+V)", 155, Color.FromArgb(225, 232, 244));
        paste.Click += delegate { PasteCapture(); };
        toolbar.Controls.Add(paste);
        save.Text = "Salvar como...  (Ctrl+S)";
        StyleButton(save, 195, Color.FromArgb(36, 87, 214));
        save.ForeColor = Color.White;
        save.Enabled = false;
        save.Click += delegate { SaveCapture(); };
        toolbar.Controls.Add(save);
        crop.Text = "Recortar"; StyleButton(crop, 105, Color.FromArgb(225, 232, 244));
        crop.Enabled = false; crop.Click += delegate { CropCapture(); }; toolbar.Controls.Add(crop);
        resize.Text = "Redimensionar"; StyleButton(resize, 145, Color.FromArgb(225, 232, 244));
        resize.Enabled = false; resize.Click += delegate { ResizeCapture(); }; toolbar.Controls.Add(resize);
        restore.Text = "Restaurar"; StyleButton(restore, 110, Color.FromArgb(225, 232, 244));
        restore.Enabled = false;
        restore.Click += delegate { if (original != null) { SetCapture(original, false); restore.Enabled = false; } };
        toolbar.Controls.Add(restore);
        preview.Dock = DockStyle.Fill;
        preview.BackColor = Color.FromArgb(229, 234, 242);
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        preview.Margin = new Padding(0);
        layout.Controls.Add(preview, 0, 3);
        placeholder.Text = "A prévia da sua captura aparece aqui.\r\n\r\nCtrl+V para começar";
        placeholder.Dock = DockStyle.Fill;
        placeholder.TextAlign = ContentAlignment.MiddleCenter;
        placeholder.ForeColor = Color.FromArgb(90, 104, 125);
        placeholder.Font = new Font("Segoe UI", 13);
        preview.Controls.Add(placeholder);
        status.Text = "PNG  •  JPG  •  BMP  •  GIF  •  TIFF  |  Salve na pasta que preferir.";
        status.Dock = DockStyle.Fill;
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.AutoEllipsis = true;
        status.ForeColor = Color.FromArgb(75, 85, 105);
        layout.Controls.Add(status, 0, 4);
        KeyDown += delegate(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.V) { e.SuppressKeyPress = true; PasteCapture(); }
            if (e.Control && e.KeyCode == Keys.S) { e.SuppressKeyPress = true; SaveCapture(); }
        };

    }

    private static Button MakeButton(string text, int width, Color color) {
        var button = new Button { Text = text }; StyleButton(button, width, color); return button;
    }
    private static void StyleButton(Button button, int width, Color color) {
        button.Size = new Size(width, 40); button.Margin = new Padding(0, 0, 12, 14);
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand; button.BackColor = color;
    }
    private void SetCapture(Image image, bool rememberOriginal = true) {
        var old = capture; capture = new Bitmap(image); preview.Image = capture;
        if (old != null) old.Dispose();
        if (rememberOriginal) { if (original != null) original.Dispose(); original = new Bitmap(image); }
        placeholder.Visible = false; save.Enabled = crop.Enabled = resize.Enabled = true;
        restore.Enabled = !rememberOriginal;
        status.Text = String.Format("Imagem: {0} × {1} pixels. Recorte, redimensione ou salve.", capture.Width, capture.Height);
    }
    private void CropCapture() {
        if (capture == null) return;
        using (var dialog = new CropDialog(capture)) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { using (var result = ImageEditing.Crop(capture, dialog.SelectedRegion)) SetCapture(result, false); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível recortar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
    private void ResizeCapture() {
        if (capture == null) return;
        using (var dialog = new ResizeDialog(capture.Size)) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { using (var result = ImageEditing.Resize(capture, dialog.TargetSize)) SetCapture(result, false); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível redimensionar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
    private void PasteCapture() {
        try {
            if (!Clipboard.ContainsImage()) { status.Text = "Nenhuma imagem para colar. Faça uma captura e tente Ctrl+V novamente."; return; }
            using (var image = Clipboard.GetImage()) if (image != null) SetCapture(image);
        } catch (Exception ex) { MessageBox.Show(this, "Não foi possível ler a área de transferência. Tente novamente.\r\n\r\n" + ex.Message, "Colar imagem", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void SaveCapture() {
        if (capture == null) return;
        using (var dialog = new SaveFileDialog()) {
            dialog.Title = "Salvar captura como";
            dialog.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|GIF (*.gif)|*.gif|TIFF (*.tiff;*.tif)|*.tiff;*.tif";
            dialog.FilterIndex = 2;
            dialog.DefaultExt = "jpg";
            dialog.FileName = "Captura_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            dialog.InitialDirectory = picturesDirectory;
            dialog.RestoreDirectory = true;
            dialog.AddExtension = true; dialog.OverwritePrompt = true; dialog.CheckPathExists = true;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { Export(capture, dialog.FileName); Saved(dialog.FileName); }
            catch (UnauthorizedAccessException) { SaveFallback(dialog.FileName); }
            catch (System.Security.SecurityException) { SaveFallback(dialog.FileName); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível salvar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
    private void SaveFallback(string originalPath) {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Capturas");
        if (MessageBox.Show(this, "O Windows não permitiu salvar na pasta escolhida.\r\n\r\nDeseja salvar na pasta do aplicativo?\r\n" + directory, "Escolher outra pasta", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
        try {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, Path.GetFileNameWithoutExtension(originalPath) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + Path.GetExtension(originalPath));
            Export(capture, path); Saved(path);
            MessageBox.Show(this, "Captura salva em:\r\n" + path, "Captura salva", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível salvar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void Saved(string path) {
        status.Text = "Salvo: " + path; tip.SetToolTip(status, status.Text);
    }

    internal static void Export(Image image, string path) {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        ImageFormat format;
        switch (extension) {
            case ".png": format = ImageFormat.Png; break;
            case ".jpg": case ".jpeg": format = ImageFormat.Jpeg; break;
            case ".bmp": format = ImageFormat.Bmp; break;
            case ".gif": format = ImageFormat.Gif; break;
            case ".tif": case ".tiff": format = ImageFormat.Tiff; break;
            default: throw new ArgumentException("Use uma extensão PNG, JPG, JPEG, BMP, GIF, TIF ou TIFF.");
        }
        using (var stream = new MemoryStream()) {
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".bmp") {
                using (var flattened = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb)) {
                    using (var graphics = Graphics.FromImage(flattened)) {
                        graphics.Clear(Color.White); graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                    }
                    if (format.Guid == ImageFormat.Jpeg.Guid) {
                        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
                        using (var parameters = new EncoderParameters(1)) {
                            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                            flattened.Save(stream, codec, parameters);
                        }
                    } else flattened.Save(stream, format);
                }
            } else image.Save(stream, format);
            // Encode fully before touching the destination file.
            File.WriteAllBytes(path, stream.ToArray());
        }
    }
    protected override void Dispose(bool disposing) {
        if (disposing) { preview.Image = null; if (capture != null) capture.Dispose(); if (original != null) original.Dispose(); tip.Dispose(); }
        base.Dispose(disposing);
    }

    [STAThread]
    public static void Main(string[] args) {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new CaptureForm());
    }
}
