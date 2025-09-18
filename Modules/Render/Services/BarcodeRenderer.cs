using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Rendering;

namespace RenderModule.Engines;

/// <summary>
/// Minimal ZXing-based barcode renderer that returns System.Drawing.Bitmap,
/// without relying on ZXing.Rendering.BitmapRenderer.
/// </summary>
public static class BarcodeRenderer
{
    public static Bitmap Render1D(
        string? value,
        string type,
        int width,
        int height,
        bool humanReadable,
        int xDim,
        int quiet)
    {
        var format = type?.ToLowerInvariant() switch
        {
            "code128" => BarcodeFormat.CODE_128,
            "ean13" => BarcodeFormat.EAN_13,
            "upca" => BarcodeFormat.UPC_A,
            _ => BarcodeFormat.CODE_128
        };

        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = format,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = quiet,
                PureBarcode = !humanReadable  // true => no human readable text
            }
        };

        var pixelData = writer.Write(value ?? string.Empty);
        return PixelDataToBitmap(pixelData, width, height);
    }

    public static Bitmap Render2D(
        string? value,
        string type,
        int width,
        int height)
    {
        var format = type?.ToLowerInvariant() switch
        {
            "qrcode" => BarcodeFormat.QR_CODE,
            "datamatrix" => BarcodeFormat.DATA_MATRIX,
            "aztec" => BarcodeFormat.AZTEC,
            // ZXing doesn’t encode MaxiCode; fallback so your UI still shows “something”
            "maxicode" => BarcodeFormat.QR_CODE,
            _ => BarcodeFormat.QR_CODE
        };

        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = format,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0
            }
        };

        var pixelData = writer.Write(value ?? string.Empty);
        return PixelDataToBitmap(pixelData, width, height);
    }

    private static Bitmap PixelDataToBitmap(PixelData pixelData, int width, int height)
    {
        // ZXing gives BGRA 32bpp in byte[]; copy directly into a 32bpp ARGB bitmap
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, width, height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(bmpData);
        }
        return bmp;
    }
}
