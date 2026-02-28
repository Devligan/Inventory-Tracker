using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Label = System.Windows.Forms.Label;

public class MainForm : Form
{
    private readonly Inventory inventory = new();

    // Layout
    private Panel sidebar;
    private Panel contentPanel;
    private Label sectionTitle;

    // Output
    private RichTextBox outputBox;

    public MainForm()
    {
        Text = "Inventory Manager";
        Size = new Size(950, 620);
        MinimumSize = new Size(800, 500);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(245, 245, 248);

        BuildLayout();
        ShowSection("List Items"); // default view
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    void BuildLayout()
    {
        // Sidebar
        sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 175,
            BackColor = Color.FromArgb(40, 44, 52),
            Padding = new Padding(0, 10, 0, 10)
        };

        string[] sections = { "List Items", "Add Item", "Use / Remove Item", "Item Info", "Date", "History" };
        int y = 60;

        var appLabel = new Label
        {
            Text = "📦 Inventory",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Bounds = new Rectangle(0, 15, 175, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };
        sidebar.Controls.Add(appLabel);

        foreach (var s in sections)
        {
            var btn = new Button
            {
                Text = s,
                Bounds = new Rectangle(10, y, 155, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.FromArgb(200, 200, 210),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = s
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 65, 77);
            btn.Click += (s2, e) => ShowSection((string)((Button)s2).Tag);
            sidebar.Controls.Add(btn);
            y += 42;
        }

        // Content area
        var contentWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

        sectionTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 44, 52)
        };

        contentPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 10) };

        outputBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(40, 44, 52),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9.5f),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        contentWrapper.Controls.Add(outputBox);
        contentWrapper.Controls.Add(contentPanel);
        contentWrapper.Controls.Add(sectionTitle);

        Controls.Add(contentWrapper);
        Controls.Add(sidebar);
    }

    // ── Section Router ────────────────────────────────────────────────────────

    void ShowSection(string name)
    {
        sectionTitle.Text = name;
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

        // Highlight active sidebar button
        foreach (Control c in sidebar.Controls)
            if (c is Button b)
                b.BackColor = (string)b.Tag == name
                    ? Color.FromArgb(80, 86, 100)
                    : Color.FromArgb(40, 44, 52);
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    void BuildListItems()
    {
        var row = MakeRow();

        var btnExp = MakeButton("By Expiration");
        var btnAlpha = MakeButton("Alphabetical");

        btnExp.Click += (s, e) => CaptureOutput(inventory.ListByExpiration);
        btnAlpha.Click += (s, e) => CaptureOutput(inventory.ListByAlphabetical);

        row.Controls.Add(btnExp);
        row.Controls.Add(btnAlpha);
        contentPanel.Controls.Add(row);
    }

    void BuildAddItem()
    {
        var (nameBox, nameRow) = MakeLabeledInput("Name:");
        var (qtyBox, qtyRow) = MakeLabeledInput("Quantity:");
        var (expBox, expRow) = MakeLabeledInput("Expiration (yyyy-MM-dd, blank = non-perishable):");

        var btnAdd = MakeButton("Add Item");
        btnAdd.Click += (s, e) =>
        {
            string name = nameBox.Text.Trim();
            if (!double.TryParse(qtyBox.Text.Trim(), out double qty) || qty <= 0)
            { ShowError("Enter a valid positive quantity."); return; }

            string expStr = expBox.Text.Trim();
            if (string.IsNullOrEmpty(expStr))
            {
                CaptureOutput(() => inventory.AddItem(new NonPerishableItem(name, qty)));
            }
            else if (DateOnly.TryParse(expStr, out var exp))
            {
                CaptureOutput(() => inventory.AddItem(new PerishableItem(name, qty, exp)));
            }
            else { ShowError("Invalid date format. Use yyyy-MM-dd."); return; }

            nameBox.Clear(); qtyBox.Clear(); expBox.Clear();
            Output($"✔ Added: {name} x{qty}");
        };

        contentPanel.Controls.AddRange(new Control[] { nameRow, qtyRow, expRow, MakeRow(btnAdd) });
    }

    void BuildUseItem()
    {
        var (nameBox, nameRow) = MakeLabeledInput("Item Name:");
        var (qtyBox, qtyRow) = MakeLabeledInput("Quantity to use:");

        var btnPerish = MakeButton("Use Perishable");
        var btnNonPerish = MakeButton("Use Non-Perishable");

        btnPerish.Click += (s, e) =>
        {
            if (!ValidateUseInputs(nameBox, qtyBox, out var name, out int qty)) return;
            CaptureOutput(() => inventory.UsePerishable(name, qty));
        };

        btnNonPerish.Click += (s, e) =>
        {
            if (!ValidateUseInputs(nameBox, qtyBox, out var name, out int qty)) return;
            CaptureOutput(() => inventory.UseNonPerishable(name, qty));
        };

        var btnRow = MakeRow(btnPerish, btnNonPerish);
        contentPanel.Controls.AddRange(new Control[] { nameRow, qtyRow, btnRow });
    }

    void BuildItemInfo()
    {
        var (nameBox, nameRow) = MakeLabeledInput("Item Name:");
        var (expBox, expRow) = MakeLabeledInput("Expiration (yyyy-MM-dd, blank = non-perishable):");

        var btnLookup = MakeButton("Look Up");
        btnLookup.Click += (s, e) =>
        {
            string name = nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowError("Enter an item name."); return; }

            string expStr = expBox.Text.Trim();
            if (string.IsNullOrEmpty(expStr))
                CaptureOutput(() => inventory.GetItemInfo(name, null));
            else if (DateOnly.TryParse(expStr, out var exp))
                CaptureOutput(() => inventory.GetItemInfo(name, exp));
            else
                ShowError("Invalid date format. Use yyyy-MM-dd.");
        };

        var btnAll = MakeButton("Show All with Name");
        btnAll.Click += (s, e) =>
        {
            string name = nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { ShowError("Enter an item name."); return; }
            CaptureOutput(() => inventory.GetItemInfo(name));
        };

        contentPanel.Controls.AddRange(new Control[] { nameRow, expRow, MakeRow(btnLookup, btnAll) });
    }

    void BuildDate()
    {
        var (dateBox, dateRow) = MakeLabeledInput("New Date (yyyy-MM-dd):");

        var btnSet = MakeButton("Set Date");
        var btnGet = MakeButton("Get Current Date");

        btnSet.Click += (s, e) =>
        {
            if (DateOnly.TryParse(dateBox.Text.Trim(), out var d))
                CaptureOutput(() => inventory.SetDate(d));
            else
                ShowError("Invalid date format. Use yyyy-MM-dd.");
        };

        btnGet.Click += (s, e) => CaptureOutput(inventory.GetDate);

        contentPanel.Controls.AddRange(new Control[] { dateRow, MakeRow(btnSet, btnGet) });
    }

    void BuildHistory()
    {
        var btnLoad = MakeButton("Load Log");
        btnLoad.Click += (s, e) =>
        {
            if (!File.Exists("log.txt")) { Output("No log file found."); return; }
            outputBox.Text = File.ReadAllText("log.txt");
            outputBox.ScrollToCaret();
        };

        var btnClear = MakeButton("Clear Log");
        btnClear.ForeColor = Color.FromArgb(180, 60, 60);
        btnClear.Click += (s, e) =>
        {
            if (MessageBox.Show("Clear the log file?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                File.WriteAllText("log.txt", "");
                outputBox.Clear();
                Output("Log cleared.");
            }
        };

        contentPanel.Controls.Add(MakeRow(btnLoad, btnClear));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    bool ValidateUseInputs(TextBox nameBox, TextBox qtyBox, out string name, out int qty)
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
        var oldOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        action();
        Console.SetOut(oldOut);
        string result = sw.ToString().Trim();
        outputBox.Text = string.IsNullOrEmpty(result) ? "(no output)" : result;
    }

    void Output(string msg) => outputBox.AppendText(msg + Environment.NewLine);
    void ShowError(string msg) => MessageBox.Show(msg, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // ── UI Factories ──────────────────────────────────────────────────────────

    FlowLayoutPanel MakeRow(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 4, 0, 4),
            WrapContents = false
        };
        row.Controls.AddRange(controls);
        return row;
    }

    (TextBox box, FlowLayoutPanel row) MakeLabeledInput(string labelText)
    {
        var lbl = new Label
        {
            Text = labelText,
            AutoSize = false,
            Width = 280,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(80, 80, 100)
        };
        var box = new TextBox { Width = 220, Height = 22, Margin = new Padding(0, 0, 10, 0) };
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0, 4, 0, 6)
        };
        row.Controls.Add(lbl);
        row.Controls.Add(box);
        return (box, row);
    }

    Button MakeButton(string text) => new Button
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(12, 6, 12, 6),
        Margin = new Padding(0, 0, 8, 0),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(60, 120, 200),
        ForeColor = Color.White,
        Cursor = Cursors.Hand
    };
}