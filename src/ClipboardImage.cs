using System.Drawing;
using System.Windows.Forms;

internal static class ClipboardImage {
    internal static Bitmap ReadImageOnly(IDataObject data) {
        if (data == null || data.GetDataPresent(DataFormats.FileDrop) ||
            data.GetDataPresent(DataFormats.Text) || data.GetDataPresent(DataFormats.UnicodeText)) return null;
        if (!data.GetDataPresent(DataFormats.Bitmap)) return null;
        var image = data.GetData(DataFormats.Bitmap) as Image;
        return image == null ? null : new Bitmap(image);
    }
}
