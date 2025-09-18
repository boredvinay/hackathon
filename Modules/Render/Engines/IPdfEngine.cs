namespace RenderModule.Engines;

public partial interface IPdfEngine
{
    /// <summary>
    /// Renders a single-page PDF of the DSL canvas with the provided payload bound to widgets.
    /// The DSL size is in pixels at DSL.dpi; we rasterize to a bitmap, then embed it into a PDF page of equal physical size.
    /// </summary>
    byte[] RenderPdf(string dslJson, Dictionary<string, object>? payload);
}
