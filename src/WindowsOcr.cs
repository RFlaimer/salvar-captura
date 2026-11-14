using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class Transcription {
    internal string Text;
    internal string Language;
}
internal sealed class OcrLanguage {
    internal string Tag;
    internal string Name;
    public override string ToString() { return Name; }
}

internal static class WindowsOcr {
    private static Type WinType(string name) { return Type.GetType(name + ", Windows, ContentType=WindowsRuntime", true); }
    private static async Task<object> AwaitOperation(object operation, Type resultType) {
        var assembly = Assembly.Load("System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        var bridge = assembly.GetType("System.WindowsRuntimeSystemExtensions").GetMethods().Single(method =>
            method.Name == "AsTask" && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1 &&
            method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.Name == "IAsyncOperation`1");
        var task = (Task)bridge.MakeGenericMethod(resultType).Invoke(null, new[] { operation });
        await task;
        return task.GetType().GetProperty("Result").GetValue(task, null);
    }
    internal static OcrLanguage[] GetLanguages() {
        var engineType = WinType("Windows.Media.Ocr.OcrEngine");
        var languageType = WinType("Windows.Globalization.Language");
        return ((IEnumerable)engineType.GetProperty("AvailableRecognizerLanguages").GetValue(null, null)).Cast<object>()
            .Select(language => new OcrLanguage { Tag = (string)languageType.GetProperty("LanguageTag").GetValue(language, null), Name = (string)languageType.GetProperty("DisplayName").GetValue(language, null) }).ToArray();
    }
    internal static async Task<Transcription> RecognizeAsync(Bitmap image, string languageTag = null) {
        var engineType = WinType("Windows.Media.Ocr.OcrEngine");
        var languageType = WinType("Windows.Globalization.Language");
        var languages = ((IEnumerable)engineType.GetProperty("AvailableRecognizerLanguages").GetValue(null, null)).Cast<object>().ToArray();
        var portuguese = languages.FirstOrDefault(language => ((string)languageType.GetProperty("LanguageTag").GetValue(language, null)).StartsWith("pt", StringComparison.OrdinalIgnoreCase));
        if (!String.IsNullOrEmpty(languageTag)) {
            portuguese = languages.FirstOrDefault(language => String.Equals((string)languageType.GetProperty("LanguageTag").GetValue(language, null), languageTag, StringComparison.OrdinalIgnoreCase));
            if (portuguese == null) throw new InvalidOperationException("O idioma selecionado não está mais disponível no OCR do Windows.");
        }
        object engine = portuguese == null ? engineType.GetMethod("TryCreateFromUserProfileLanguages").Invoke(null, null) : engineType.GetMethod("TryCreateFromLanguage").Invoke(null, new[] { portuguese });
        if (engine == null && languages.Length > 0) engine = engineType.GetMethod("TryCreateFromLanguage").Invoke(null, new[] { languages[0] });
        if (engine == null) throw new InvalidOperationException("O OCR do Windows não encontrou um idioma de reconhecimento instalado. Adicione o recurso de OCR de um idioma nas configurações de idioma do Windows e tente novamente.");
        double maximum = Math.Min(4096, Convert.ToDouble(engineType.GetProperty("MaxImageDimension").GetValue(null, null)));
        double scale = Math.Min(1.0, maximum / Math.Max(image.Width, image.Height));
        var size = new Size(Math.Max(1, (int)Math.Round(image.Width * scale)), Math.Max(1, (int)Math.Round(image.Height * scale)));
        using (var flattened = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb)) {
            using (var graphics = Graphics.FromImage(flattened)) {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, size));
            }
            byte[] pixels = new byte[size.Width * size.Height * 4];
            var bits = flattened.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {
                for (int row = 0; row < size.Height; row++) Marshal.Copy(IntPtr.Add(bits.Scan0, row * bits.Stride), pixels, row * size.Width * 4, size.Width * 4);
            } finally { flattened.UnlockBits(bits); }
            var writerType = WinType("Windows.Storage.Streams.DataWriter");
            object writer = Activator.CreateInstance(writerType);
            object bitmap = null;
            try {
                writerType.GetMethod("WriteBytes").Invoke(writer, new object[] { pixels });
                object buffer = writerType.GetMethod("DetachBuffer").Invoke(writer, null);
                var bitmapType = WinType("Windows.Graphics.Imaging.SoftwareBitmap");
                object pixelFormat = Enum.Parse(WinType("Windows.Graphics.Imaging.BitmapPixelFormat"), "Bgra8");
                object alpha = Enum.Parse(WinType("Windows.Graphics.Imaging.BitmapAlphaMode"), "Ignore");
                bitmap = bitmapType.GetMethods().Single(method => method.Name == "CreateCopyFromBuffer" && method.GetParameters().Length == 5).Invoke(null, new[] { buffer, pixelFormat, (object)size.Width, (object)size.Height, alpha });
                object operation = engineType.GetMethod("RecognizeAsync").Invoke(engine, new[] { bitmap });
                var resultType = WinType("Windows.Media.Ocr.OcrResult");
                object result = await AwaitOperation(operation, resultType);
                var lineType = WinType("Windows.Media.Ocr.OcrLine");
                var lines = ((IEnumerable)resultType.GetProperty("Lines").GetValue(result, null)).Cast<object>();
                object language = engineType.GetProperty("RecognizerLanguage").GetValue(engine, null);
                return new Transcription { Text = String.Join(Environment.NewLine, lines.Select(line => (string)lineType.GetProperty("Text").GetValue(line, null))), Language = (string)languageType.GetProperty("DisplayName").GetValue(language, null) };
            } finally {
                if (bitmap is IDisposable) ((IDisposable)bitmap).Dispose();
                if (writer is IDisposable) ((IDisposable)writer).Dispose();
            }
        }
    }
}

internal sealed class OcrLanguageDialog : Form {
    private readonly ComboBox languages = new ComboBox();
    internal string SelectedLanguage { get { return ((OcrLanguage)languages.SelectedItem).Tag; } }
    internal OcrLanguageDialog(OcrLanguage[] available) {
        Text = "Idioma do texto"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(460, 180); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(new Label { Text = "Qual é o idioma do texto na imagem?", Left = 22, Top = 20, Width = 410, Height = 25 });
        languages.DropDownStyle = ComboBoxStyle.DropDownList; languages.SetBounds(22, 55, 415, 32);
        languages.Items.AddRange(available); languages.SelectedIndex = 0;
        for (int index = 0; index < available.Length; index++) if (available[index].Tag.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) { languages.SelectedIndex = index; break; }
        Controls.Add(languages);
        var apply = new Button { Text = "Transcrever", Left = 302, Top = 123, Width = 135, Height = 34, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancelar", Left = 179, Top = 123, Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
        Controls.Add(apply); Controls.Add(cancel); AcceptButton = apply; CancelButton = cancel;
    }
}

internal sealed class TranscriptionDialog : Form {
    internal TranscriptionDialog(Transcription result) {
        Text = "Texto da imagem"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(760, 520); MinimumSize = new Size(540, 360);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi; BackColor = Color.FromArgb(246, 248, 252);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); Controls.Add(layout);
        var hint = new Label { Text = "Confira o texto antes de copiar. Idioma: " + result.Language, Dock = DockStyle.Fill, AutoEllipsis = true };
        layout.Controls.Add(hint, 0, 0);
        var text = new TextBox { Text = result.Text, Multiline = true, AcceptsReturn = true, AcceptsTab = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
        layout.Controls.Add(text, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0) };
        layout.Controls.Add(actions, 0, 2);
        var close = new Button { Text = "Fechar", Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
        var copy = new Button { Text = "Copiar texto", Width = 145, Height = 34 };
        copy.Click += delegate {
            try { if (!String.IsNullOrEmpty(text.Text)) { Clipboard.SetText(text.Text); hint.Text = "Texto copiado. Você pode colar com Ctrl+V."; } }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível copiar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        actions.Controls.Add(close); actions.Controls.Add(copy); CancelButton = close;
    }
}
