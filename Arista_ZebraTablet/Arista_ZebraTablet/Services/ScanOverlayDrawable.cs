namespace Arista_ZebraTablet.Services
{
    public class ScanOverlayDrawable : IDrawable
    {
        public static readonly ScanOverlayDrawable Instance = new();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Dim entire screen (semi-transparent overlay)
            canvas.FillColor = Color.FromArgb("#000000BF");
            canvas.FillRectangle(dirtyRect);

            // Punch-out rectangle (scan box)
            float boxWidth = 300;
            float boxHeight = 300;
            float x = (dirtyRect.Width - boxWidth) / 2;
            float y = (dirtyRect.Height - boxHeight) / 2;

            // Clear the scan box area
            canvas.BlendMode = BlendMode.DestinationOut;
            canvas.FillColor = Color.FromArgb("#00000000");
            canvas.FillRoundedRectangle(x, y, boxWidth, boxHeight, 12);

            // Draw corner brackets
            float bracketLength = 24;
            float bracketThickness = 6;
            float radius = 6;

            // Top-left horizontal
            canvas.FillColor = Colors.Cyan;
            canvas.FillRoundedRectangle(x, y, bracketLength, bracketThickness, radius);

            // Top-left vertical
            canvas.FillRoundedRectangle(x, y, bracketThickness, bracketLength, radius);

            // Top-right horizontal
            canvas.FillRoundedRectangle(x + boxWidth - bracketLength, y, bracketLength, bracketThickness, radius);

            // Top-right vertical
            canvas.FillRoundedRectangle(x + boxWidth - bracketThickness, y, bracketThickness, bracketLength, radius);

            // Bottom-left horizontal
            canvas.FillRoundedRectangle(x, y + boxHeight - bracketThickness, bracketLength, bracketThickness, radius);

            // Bottom-left vertical
            canvas.FillRoundedRectangle(x, y + boxHeight - bracketLength, bracketThickness, bracketLength, radius);

            // Bottom-right horizontal
            canvas.FillRoundedRectangle(x + boxWidth - bracketLength, y + boxHeight - bracketThickness, bracketLength, bracketThickness, radius);

            // Bottom-right vertical
            canvas.FillRoundedRectangle(x + boxWidth - bracketThickness, y + boxHeight - bracketLength, bracketThickness, bracketLength, radius);
        }
    }
}