using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;
using Label = System.Windows.Forms.Label;

public class MainForm : Form
{
    private readonly Inventory inventory = new();

    private Panel sidebar;
    private TableLayoutPanel mainTable;
    private FlowLayoutPanel contentPanel;
    private Label sectionTitle;
    private RichTextBox outputBox;

    static readonly Color ColorBackground = Color.FromArgb(8, 8, 18);
    static readonly Color ColorSidebar = Color.FromArgb(10, 10, 26);
    static readonly Color ColorAccentBright = Color.FromArgb(0, 110, 255);
    static readonly Color ColorAccentDark = Color.FromArgb(0, 40, 130);
    static readonly Color ColorText = Color.FromArgb(210, 220, 255);
    static readonly Color ColorSubtext = Color.FromArgb(90, 110, 170);
    static readonly Color ColorInputBg = Color.FromArgb(13, 13, 32);
    static readonly Color ColorInputBorder = Color.FromArgb(0, 55, 150);

    public MainForm()
    {
        Text = "Inventory Manager";
        Size = new Size(1020, 680);
        MinimumSize = new Size(860, 540);
        BackColor = ColorBackground;
        ForeColor = ColorText;
        Font = new Font("Segoe UI", 9.5f);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        ShowSection("List Items");
    }


    // Layout

    void BuildLayout()
    {
        sidebar = new GradientPanel(ColorSidebar, Color.FromArgb(4, 4, 24))
        {
            Dock = DockStyle.Left,
            Width = 195,
        };

        var titleLabel = new Label
        {
            Text = "INVENTORY",
            ForeColor = ColorAccentBright,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Bounds = new Rectangle(0, 20, 195, 26),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var subtitleLabel = new Label
        {
            Text = "MANAGER",
            ForeColor = ColorSubtext,
            Font = new Font("Segoe UI", 7.5f),
            Bounds = new Rectangle(0, 44, 195, 18),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var divider = new Panel
        {
            Bounds = new Rectangle(20, 70, 155, 1),
            BackColor = ColorAccentDark,
        };

        sidebar.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, divider });

        string[] buttonLabels = { "List Items", "Add Item", "Use / Remove", "Item Info", "Date", "History" };
        string[] sectionTags = { "List Items", "Add Item", "Use / Remove Item", "Item Info", "Date", "History" };

        int buttonY = 86;
        for (int i = 0; i < buttonLabels.Length; i++)
        {
            var navBtn = new NavButton(buttonLabels[i], sectionTags[i], ColorSidebar, ColorAccentBright);
            navBtn.SetBounds(0, buttonY, 195, 44);
            navBtn.Click += (sender, args) => ShowSection((string)((NavButton)sender).Tag);
            sidebar.Controls.Add(navBtn);
            buttonY += 48;
        }

        sectionTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(24, 0, 0, 0),
            BackColor = Color.Transparent,
        };

        var accentLine = new GradientPanel(ColorAccentBright, Color.FromArgb(0, 20, 80))
        {
            Dock = DockStyle.Top,
            Height = 2,
        };

        contentPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 220,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(24, 12, 12, 8),
            AutoScroll = false,
        };

        outputBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = ColorInputBg,
            ForeColor = ColorText,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9.5f),
            Padding = new Padding(6),
        };

        var outputBorder = new BorderPanel(ColorInputBorder)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(24, 0, 24, 20),
        };
        outputBorder.Controls.Add(outputBox);

        mainTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.Transparent,
        };
        mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 2));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        mainTable.Controls.Add(sectionTitle, 0, 0);
        mainTable.Controls.Add(accentLine, 0, 1);
        mainTable.Controls.Add(contentPanel, 0, 2);
        mainTable.Controls.Add(outputBorder, 0, 3);

        var rightPanel = new GradientPanel(ColorBackground, Color.FromArgb(4, 8, 28))
        {
            Dock = DockStyle.Fill,
        };
        rightPanel.Controls.Add(mainTable);

        Controls.Add(rightPanel);
        Controls.Add(sidebar);
    }


    // Section Router

    void ShowSection(string name)
    {
        sectionTitle.Text = name.ToUpper();
        contentPanel.Controls.Clear();
        outputBox.Clear();

        switch (name)
        {
            case "List Items": BuildListItems(); break;
            case "Add Item": BuildAddItem(); break;
            case "Use / Remove Item": BuildUseItem(); break;
            case "Item Info": BuildItemInfo(); break;
            case "Date": BuildDate(); break;
            case "History": BuildHistory(); break;
        }

        foreach (Control control in sidebar.Controls)
            if (control is NavButton navBtn)
                navBtn.SetActive((string)navBtn.Tag == name);
    }


    // Sections

    void BuildListItems()
    {
        contentPanel.Controls.Add(MakeSectionNote("Choose a sort order to display all inventory items."));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("By Expiration", () => CaptureOutput(inventory.ListByExpiration)),
            MakeButton("Alphabetical", () => CaptureOutput(inventory.ListByAlphabetical))
        ));
    }

    void BuildAddItem()
    {
        var nameBox = MakeInput("e.g. Milk");
        var qtyBox = MakeInput("e.g. 10");
        var expBox = MakeInput("yyyy-MM-dd  (blank = non-perishable)");

        contentPanel.Controls.Add(MakeFieldRow("Name", nameBox, "Quantity", qtyBox));
        contentPanel.Controls.Add(MakeFieldRow("Expiry Date", expBox));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("Add Item", () =>
            {
                string name = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(name)) { ShowError("Enter an item name."); return; }
                if (!double.TryParse(qtyBox.Text.Trim(), out double qty) || qty <= 0) { ShowError("Enter a valid positive quantity."); return; }

                string expStr = expBox.Text.Trim();
                if (string.IsNullOrEmpty(expStr))
                    CaptureOutput(() => inventory.AddItem(new NonPerishableItem(name, qty)));
                else if (DateOnly.TryParse(expStr, out var exp))
                    CaptureOutput(() => inventory.AddItem(new PerishableItem(name, qty, exp)));
                else { ShowError("Invalid date. Use yyyy-MM-dd."); return; }

                nameBox.Clear(); qtyBox.Clear(); expBox.Clear();
                Output("Item added successfully.");
            })
        ));
    }

    void BuildUseItem()
    {
        var nameBox = MakeInput("Item name");
        var qtyBox = MakeInput("Integer quantity");

        contentPanel.Controls.Add(MakeFieldRow("Name", nameBox, "Quantity", qtyBox));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("Use Perishable", () =>
            {
                if (!ValidateUse(nameBox, qtyBox, out string name, out int qty)) return;
                CaptureOutput(() => inventory.UsePerishable(name, qty));
            }),
            MakeButton("Use Non-Perishable", () =>
            {
                if (!ValidateUse(nameBox, qtyBox, out string name, out int qty)) return;
                CaptureOutput(() => inventory.UseNonPerishable(name, qty));
            })
        ));
    }

    void BuildItemInfo()
    {
        var nameBox = MakeInput("Item name");
        var expBox = MakeInput("yyyy-MM-dd  (blank = non-perishable)");

        contentPanel.Controls.Add(MakeFieldRow("Name", nameBox, "Expiry Date", expBox));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("Look Up Specific", () =>
            {
                string name = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(name)) { ShowError("Enter a name."); return; }

                string expStr = expBox.Text.Trim();
                if (string.IsNullOrEmpty(expStr))
                    CaptureOutput(() => inventory.GetItemInfo(name, null));
                else if (DateOnly.TryParse(expStr, out var exp))
                    CaptureOutput(() => inventory.GetItemInfo(name, exp));
                else
                    ShowError("Invalid date. Use yyyy-MM-dd.");
            }),
            MakeButton("Show All Batches", () =>
            {
                string name = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(name)) { ShowError("Enter a name."); return; }
                CaptureOutput(() => inventory.GetItemInfo(name));
            })
        ));
    }

    void BuildDate()
    {
        var dateBox = MakeInput("yyyy-MM-dd");

        contentPanel.Controls.Add(MakeFieldRow("New Date", dateBox));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("Set Date", () =>
            {
                if (DateOnly.TryParse(dateBox.Text.Trim(), out var d))
                    CaptureOutput(() => inventory.SetDate(d));
                else
                    ShowError("Invalid date. Use yyyy-MM-dd.");
            }),
            MakeButton("Get Current Date", () => CaptureOutput(inventory.GetDate))
        ));
    }

    void BuildHistory()
    {
        contentPanel.Controls.Add(MakeSectionNote("View or clear the inventory change log."));
        contentPanel.Controls.Add(MakeButtonRow(
            MakeButton("Load Log", () =>
            {
                if (!File.Exists("log.txt")) { Output("No log file found."); return; }
                outputBox.Text = File.ReadAllText("log.txt");
            }),
            MakeDangerButton("Clear Log", () =>
            {
                if (MessageBox.Show("Clear the entire log?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.WriteAllText("log.txt", "");
                    outputBox.Clear();
                    Output("Log cleared.");
                }
            })
        ));
    }


    // Helpers

    bool ValidateUse(TextBox nameBox, TextBox qtyBox, out string name, out int qty)
    {
        name = nameBox.Text.Trim();
        qty = 0;
        if (string.IsNullOrEmpty(name)) { ShowError("Enter an item name."); return false; }
        if (!int.TryParse(qtyBox.Text.Trim(), out qty) || qty <= 0) { ShowError("Enter a valid positive integer quantity."); return false; }
        return true;
    }

    void CaptureOutput(Action action)
    {
        outputBox.Clear();
        var previousOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        action();
        Console.SetOut(previousOut);
        string result = writer.ToString().Trim();
        outputBox.Text = string.IsNullOrEmpty(result) ? "(no output)" : result;
    }

    void Output(string message) => outputBox.AppendText(message + Environment.NewLine);
    void ShowError(string message) => MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);


    // UI Factories

    FlowLayoutPanel MakeFieldRow(string label1, Control control1, string label2 = null, Control control2 = null)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8),
        };

        row.Controls.Add(MakeLabeledField(label1, control1));
        if (label2 != null && control2 != null)
            row.Controls.Add(MakeLabeledField(label2, control2));

        return row;
    }

    Panel MakeLabeledField(string labelText, Control input)
    {
        var container = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 20, 0),
        };

        container.Controls.Add(new Label
        {
            Text = labelText.ToUpper(),
            ForeColor = ColorSubtext,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 3),
        });

        container.Controls.Add(input);
        return container;
    }

    FlowLayoutPanel MakeButtonRow(params Button[] buttons)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0),
        };

        row.Controls.AddRange(buttons);
        return row;
    }

    Label MakeSectionNote(string text) => new Label
    {
        Text = text,
        ForeColor = ColorSubtext,
        AutoSize = true,
        Font = new Font("Segoe UI", 9f),
        Margin = new Padding(0, 4, 0, 10),
    };

    TextBox MakeInput(string placeholder) => new PlaceholderTextBox(placeholder)
    {
        Width = 240,
        Height = 30,
        BackColor = ColorInputBg,
        ForeColor = ColorText,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10f),
        Margin = new Padding(0),
    };

    Button MakeButton(string text, Action onClick)
    {
        var button = new GradientButton(ColorAccentDark, ColorAccentBright)
        {
            Text = text,
            Size = new Size(170, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0),
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (sender, args) => onClick();
        return button;
    }

    Button MakeDangerButton(string text, Action onClick)
    {
        var button = new GradientButton(Color.FromArgb(80, 10, 10), Color.FromArgb(180, 30, 30))
        {
            Text = text,
            Size = new Size(170, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0),
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (sender, args) => onClick();
        return button;
    }
}


// Custom Controls

class GradientPanel : Panel
{
    readonly Color colorTop;
    readonly Color colorBottom;

    public GradientPanel(Color top, Color bottom)
    {
        colorTop = top;
        colorBottom = bottom;
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (ClientRectangle.Width == 0 || ClientRectangle.Height == 0) return;
        using var brush = new LinearGradientBrush(ClientRectangle, colorTop, colorBottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

class BorderPanel : Panel
{
    readonly Color borderColor;

    public BorderPanel(Color border)
    {
        borderColor = border;
        BackColor = Color.FromArgb(13, 13, 32);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(borderColor, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

class GradientButton : Button
{
    readonly Color colorFrom;
    readonly Color colorTo;
    bool isHovered;

    public GradientButton(Color from, Color to)
    {
        colorFrom = from;
        colorTo = to;
        DoubleBuffered = true;
    }

    protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientRectangle.Width == 0 || ClientRectangle.Height == 0) return;

        Color from = isHovered ? colorTo : colorFrom;
        Color to = isHovered
            ? Color.FromArgb(Math.Min(colorTo.R + 40, 255), Math.Min(colorTo.G + 40, 255), 255)
            : colorTo;

        using var brush = new LinearGradientBrush(ClientRectangle, from, to, LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

class NavButton : Button
{
    readonly Color bgColor;
    readonly Color accentColor;
    bool isActive;

    public NavButton(string text, string tag, Color bg, Color accent)
    {
        Text = text;
        Tag = tag;
        bgColor = bg;
        accentColor = accent;

        FlatStyle = FlatStyle.Flat;
        ForeColor = Color.FromArgb(150, 165, 210);
        Font = new Font("Segoe UI", 9.5f);
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(18, 0, 0, 0);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        FlatAppearance.BorderSize = 0;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        ForeColor = active ? Color.White : Color.FromArgb(150, 165, 210);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(bgColor);

        if (isActive)
        {
            using var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(160, 0, 55, 170),
                Color.Transparent,
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, ClientRectangle);

            using var pen = new Pen(accentColor, 3);
            e.Graphics.DrawLine(pen, 0, 0, 0, Height);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
    }
}

class PlaceholderTextBox : TextBox
{
    readonly string placeholderText;
    bool isFocused;

    static readonly Color PlaceholderColor = Color.FromArgb(65, 85, 135);

    public PlaceholderTextBox(string placeholder)
    {
        placeholderText = placeholder;
        GotFocus += (sender, args) => { isFocused = true; Invalidate(); };
        LostFocus += (sender, args) => { isFocused = false; Invalidate(); };
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == 0xF && string.IsNullOrEmpty(Text) && !isFocused)
        {
            using var g = Graphics.FromHwnd(Handle);
            TextRenderer.DrawText(g, placeholderText, Font,
                new Rectangle(2, 2, Width - 4, Height - 4),
                PlaceholderColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}