using System.Drawing;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private static readonly Color ColorBackground = Color.FromArgb(15, 17, 21);
        private static readonly Color ColorSurface = Color.FromArgb(27, 31, 37);
        private static readonly Color ColorSurfaceRaised = Color.FromArgb(38, 43, 50);
        private static readonly Color ColorAccent = Color.FromArgb(90, 169, 230);
        private static readonly Color ColorCheckedOn = Color.FromArgb(120, 224, 143);
        private static readonly Color ColorTextPrimary = Color.FromArgb(230, 232, 235);
        private static readonly Color ColorTextSecondary = Color.FromArgb(176, 183, 190);

        // Walks the whole control tree once (after every tab/group/row has been built) applying a
        // dark, flat palette. Kept generic by control type rather than styling each control at the
        // call site, so new rows/tabs added later inherit the theme automatically.
        private void ApplyDarkTheme(Control root)
        {
            BackColor = ColorBackground;

            foreach (Control control in root.Controls)
            {
                switch (control)
                {
                    case TabControl tabControl:
                        StyleTabControl(tabControl);
                        break;
                    case TabPage tabPage:
                        tabPage.BackColor = ColorBackground;
                        break;
                    case GroupBox groupBox:
                        // Solid "card" rather than transparent: WinForms' transparent-BackColor trick is
                        // reliable one level deep (a Label straight on an imaged TabPage), but chaining it
                        // through a GroupBox into its CheckBox children is prone to render glitches.
                        groupBox.BackColor = ColorSurface;
                        groupBox.ForeColor = ColorAccent;
                        groupBox.Font = new Font(groupBox.Font, FontStyle.Bold);
                        break;
                    case CheckBox checkBox:
                        checkBox.BackColor = ColorSurface;
                        void UpdateCheckBoxLook(object? _ = null, EventArgs? __ = null)
                        {
                            checkBox.ForeColor = checkBox.Checked ? ColorCheckedOn : ColorTextPrimary;
                        }
                        UpdateCheckBoxLook();
                        checkBox.CheckedChanged += UpdateCheckBoxLook;
                        break;
                    case Button button:
                        button.FlatStyle = FlatStyle.Flat;
                        button.BackColor = ColorSurfaceRaised;
                        button.ForeColor = ColorTextPrimary;
                        button.FlatAppearance.BorderColor = ColorAccent;
                        button.FlatAppearance.BorderSize = 2;
                        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(ColorSurfaceRaised, 0.2f);
                        break;
                    case ComboBox comboBox:
                        comboBox.FlatStyle = FlatStyle.Flat;
                        comboBox.BackColor = ColorSurfaceRaised;
                        comboBox.ForeColor = ColorTextPrimary;
                        break;
                    case TextBox textBox:
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        textBox.BackColor = ColorSurfaceRaised;
                        textBox.ForeColor = ColorTextPrimary;
                        break;
                    case Panel panel:
                        panel.BackColor = Color.Transparent;
                        break;
                    case Label label:
                        label.BackColor = Color.Transparent;
                        label.ForeColor = ColorTextSecondary;
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyDarkTheme(control);
                }
            }
        }

        private sealed class LegibleCheckBox : CheckBox
        {
            private const int BoxSize = 18;

            public LegibleCheckBox()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);

                var boxRect = new Rectangle(0, (Height - BoxSize) / 2, BoxSize, BoxSize);
                using (var fillBrush = new SolidBrush(Checked ? ColorCheckedOn : ColorSurfaceRaised))
                {
                    e.Graphics.FillRectangle(fillBrush, boxRect);
                }
                using (var borderPen = new Pen(ColorAccent, 2))
                {
                    e.Graphics.DrawRectangle(borderPen, boxRect);
                }
                if (Checked)
                {
                    using var checkPen = new Pen(Color.Black, 2.5f);
                    e.Graphics.DrawLines(checkPen, new[]
                    {
                        new Point(boxRect.Left + 4, boxRect.Top + 9),
                        new Point(boxRect.Left + 8, boxRect.Top + 14),
                        new Point(boxRect.Left + 15, boxRect.Top + 4),
                    });
                }

                var textRect = new Rectangle(BoxSize + 8, 0, Width - BoxSize - 8, Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        private static void StyleTabControl(TabControl tabControl)
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.Padding = new Point(12, 5);
            tabControl.Font = new Font(tabControl.Font.FontFamily, 9.5f);
            tabControl.DrawItem -= TabControl_DrawItem;
            tabControl.DrawItem += TabControl_DrawItem;
        }

        private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl)
            {
                return;
            }

            TabPage page = tabControl.TabPages[e.Index];
            bool selected = e.Index == tabControl.SelectedIndex;

            using (var backBrush = new SolidBrush(selected ? ColorAccent : ColorSurface))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            Color textColor = selected ? Color.Black : ColorTextPrimary;
            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font, e.Bounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
