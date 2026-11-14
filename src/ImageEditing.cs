using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

internal static class ImageEditing {
    internal static Rectangle Fit(Size view, Size image) {
        double scale = Math.Min((double)view.Width / image.Width, (double)view.Height / image.Height);
        int width = Math.Max(1, (int)Math.Round(image.Width * scale));
        int height = Math.Max(1, (int)Math.Round(image.Height * scale));
        return new Rectangle((view.Width - width) / 2, (view.Height - height) / 2, width, height);
    }
    internal static Rectangle SelectionToImage(Point start, Point end, Rectangle display, Size image) {
        if (start == end || display.Width <= 0 || display.Height <= 0) return Rectangle.Empty;
        int left = Math.Max(0, Math.Min(image.Width, (int)Math.Floor((Math.Min(start.X, end.X) - display.Left) * (double)image.Width / display.Width)));
        int top = Math.Max(0, Math.Min(image.Height, (int)Math.Floor((Math.Min(start.Y, end.Y) - display.Top) * (double)image.Height / display.Height)));
        int right = Math.Max(0, Math.Min(image.Width, (int)Math.Ceiling((Math.Max(start.X, end.X) - display.Left) * (double)image.Width / display.Width)));
        int bottom = Math.Max(0, Math.Min(image.Height, (int)Math.Ceiling((Math.Max(start.Y, end.Y) - display.Top) * (double)image.Height / display.Height)));
        return Rectangle.FromLTRB(left, top, right, bottom);
    }
    internal static Bitmap Crop(Bitmap source, Rectangle region) {
        region.Intersect(new Rectangle(Point.Empty, source.Size));
        if (region.Width < 1 || region.Height < 1) throw new ArgumentException("Selecione uma área da imagem para recortar.");
        return source.Clone(region, PixelFormat.Format32bppArgb);
    }
    internal static Bitmap Resize(Bitmap source, Size target) {
        if (target.Width < 1 || target.Height < 1 || (long)target.Width * target.Height > 100000000)
            throw new ArgumentException("Escolha dimensões válidas, com no máximo 100 milhões de pixels.");
        var result = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
        try {
            using (var graphics = Graphics.FromImage(result))
            using (var attributes = new ImageAttributes()) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(source, new Rectangle(Point.Empty, target), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return result;
        } catch { result.Dispose(); throw; }
    }
}

internal sealed class CropCanvas : Control {
    private readonly Bitmap source;
    private Point start, end;
    private bool dragging, hasSelection;
    internal event EventHandler SelectionChanged;
    internal Rectangle SelectedRegion { get { return hasSelection ? ImageEditing.SelectionToImage(start, end, ImageEditing.Fit(ClientSize, source.Size), source.Size) : Rectangle.Empty; } }
    internal CropCanvas(Bitmap image) {
        source = image; DoubleBuffered = true; ResizeRedraw = true;
        Cursor = Cursors.Cross; BackColor = Color.FromArgb(229, 234, 242); Dock = DockStyle.Fill;
    }
    private Point Clamp(Point point) {
        Rectangle rect = ImageEditing.Fit(ClientSize, source.Size);
        return new Point(Math.Max(rect.Left, Math.Min(rect.Right, point.X)), Math.Max(rect.Top, Math.Min(rect.Bottom, point.Y)));
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !ImageEditing.Fit(ClientSize, source.Size).Contains(e.Location)) return;
        start = end = Clamp(e.Location); dragging = hasSelection = true; Capture = true; Changed();
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) { end = Clamp(e.Location); Changed(); } }
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e); if (!dragging) return;
        end = Clamp(e.Location); dragging = false; Capture = false; Changed();
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); hasSelection = dragging = false; Capture = false; Changed(); }
    private void Changed() { Invalidate(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        Rectangle display = ImageEditing.Fit(ClientSize, source.Size);
        e.Graphics.DrawImage(source, display);
        Rectangle region = SelectedRegion;
        if (region.Width < 1 || region.Height < 1) return;
        Rectangle selected = Rectangle.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        using (var shade = new SolidBrush(Color.FromArgb(130, Color.Black)))
        using (var outside = new Region(display)) {
            outside.Exclude(selected); e.Graphics.FillRegion(shade, outside);
        }
        using (var pen = new Pen(Color.White, 2)) { pen.DashStyle = DashStyle.Dash; e.Graphics.DrawRectangle(pen, selected); }
    }
}

internal sealed class CropDialog : Form {
    private readonly CropCanvas canvas;
    internal Rectangle SelectedRegion { get { return canvas.SelectedRegion; } }
    internal CropDialog(Bitmap image) {
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Text = "Recortar imagem"; StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(850, 580); MinimumSize = new Size(600, 430);
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(246, 248, 252);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        Controls.Add(layout);
        var instructions = new Label { Text = "Arraste sobre a imagem para selecionar a área que deseja manter.", Dock = DockStyle.Fill };
        layout.Controls.Add(instructions, 0, 0);
        canvas = new CropCanvas(image); layout.Controls.Add(canvas, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        layout.Controls.Add(actions, 0, 2);
        var apply = new Button { Text = "Aplicar recorte", Width = 150, Height = 34, Enabled = false, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancelar", Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
        var dimensions = new Label { Text = "Nenhuma seleção", Width = 230, Height = 34, TextAlign = ContentAlignment.MiddleLeft };
        actions.Controls.Add(apply); actions.Controls.Add(cancel); actions.Controls.Add(dimensions);
        canvas.SelectionChanged += delegate {
            Rectangle region = canvas.SelectedRegion; apply.Enabled = region.Width > 0 && region.Height > 0;
            dimensions.Text = apply.Enabled ? String.Format("Recorte: {0} × {1} pixels", region.Width, region.Height) : "Nenhuma seleção";
        };
        AcceptButton = apply; CancelButton = cancel;
    }
}

internal sealed class ResizeDialog : Form {
    private readonly NumericUpDown width = new NumericUpDown(), height = new NumericUpDown();
    private readonly CheckBox proportion = new CheckBox();
    private readonly Size original;
    private bool updating;
    internal Size TargetSize { get { return new Size((int)width.Value, (int)height.Value); } }
    internal ResizeDialog(Size image) {
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        original = image; Text = "Redimensionar imagem"; StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 270); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(246, 248, 252);
        var label = new Label { Text = String.Format("Atual: {0} × {1} pixels", image.Width, image.Height), Left = 24, Top = 20, Width = 390, Height = 28 }; Controls.Add(label);
        Controls.Add(new Label { Text = "Largura (px)", Left = 24, Top = 62, Width = 180 });
        Controls.Add(new Label { Text = "Altura (px)", Left = 235, Top = 62, Width = 180 });
        width.SetBounds(24, 90, 180, 30); height.SetBounds(235, 90, 180, 30);
        width.Minimum = height.Minimum = 1; width.Maximum = height.Maximum = 65535;
        width.Value = Math.Min(image.Width, 65535); height.Value = Math.Min(image.Height, 65535);
        Controls.Add(width); Controls.Add(height);
        proportion.Text = "Manter proporção"; proportion.Checked = true; proportion.SetBounds(24, 140, 300, 30); Controls.Add(proportion);
        width.ValueChanged += delegate { Sync(true); }; height.ValueChanged += delegate { Sync(false); };
        proportion.CheckedChanged += delegate { if (proportion.Checked) Sync(true); };
        var apply = new Button { Text = "Aplicar", Left = 295, Top = 209, Width = 120, Height = 36 };
        var cancel = new Button { Text = "Cancelar", Left = 163, Top = 209, Width = 120, Height = 36, DialogResult = DialogResult.Cancel };
        apply.Click += delegate {
            if ((long)width.Value * (long)height.Value > 100000000) { MessageBox.Show(this, "Escolha dimensões com no máximo 100 milhões de pixels.", "Dimensões muito grandes"); return; }
            DialogResult = DialogResult.OK;
        };
        Controls.Add(apply); Controls.Add(cancel); AcceptButton = apply; CancelButton = cancel;
    }
    private void Sync(bool fromWidth) {
        if (updating || !proportion.Checked) return;
        updating = true;
        try {
            var other = fromWidth ? height : width;
            decimal value = fromWidth ? Math.Round(width.Value * original.Height / original.Width) : Math.Round(height.Value * original.Width / original.Height);
            if (value > other.Maximum) {
                other.Value = other.Maximum;
                if (fromWidth) width.Value = Math.Max(width.Minimum, Math.Round(other.Maximum * original.Width / original.Height));
                else height.Value = Math.Max(height.Minimum, Math.Round(other.Maximum * original.Height / original.Width));
            } else other.Value = Math.Max(other.Minimum, value);
        } finally { updating = false; }
    }
}
