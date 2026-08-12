using System.Drawing;

namespace WindowTitleRenamer.UI.Controls;

internal sealed class ThemedButton : Button
{
    public Color DisabledBackColor { get; set; } = SystemColors.Control;
    public Color DisabledForeColor { get; set; } = SystemColors.GrayText;
    public Color DisabledBorderColor { get; set; } = SystemColors.ControlDark;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        if (Enabled)
        {
            base.OnPaint(eventArgs);
            return;
        }

        eventArgs.Graphics.Clear(DisabledBackColor);

        if (FlatAppearance.BorderSize > 0)
        {
            using Pen borderPen = new(DisabledBorderColor, FlatAppearance.BorderSize);
            Rectangle border = ClientRectangle;
            border.Width -= 1;
            border.Height -= 1;
            eventArgs.Graphics.DrawRectangle(borderPen, border);
        }

        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            ClientRectangle,
            DisabledForeColor,
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine);
    }
}
