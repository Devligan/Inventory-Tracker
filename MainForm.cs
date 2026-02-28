using System;
using System.Drawing;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Font = System.Drawing.Font;
using Application = System.Windows.Forms.Application;
using static System.Net.Mime.MediaTypeNames;

public class MainForm : Form
{
    private Inventory inventory = new();

    // Layout
    private SplitContainer splitMain;
    private Panel panelLeft;
    private Panel panelRight;

    // Left: item list
    private Label lblInventoryTitle;
    private RadioButton rbSortExpiration;
    private RadioButton rbSortAlphabetical;
    private ListView listView;

    // Right: tabbed actions
    private TabControl tabControl;
    private TabPage tabAdd;
    private TabPage tabUse;
    private TabPage tabSearch;
    private TabPage tabDate;

    // Add tab
    private RadioButton rbNonPerishable;
    private RadioButton rbPerishable;
    private Label lblAddName, lblAddQty, lblAddExp;
    private TextBox txtAddName, txtAddQty;
    private DateTimePicker dtpAddExp;
    private Button btnAdd;

    // Use tab
    private RadioButton rbUseNonPerishable;
    private RadioButton rbUsePerishable;
    private Label lblUseName, lblUseQty;
    private TextBox txtUseName, txtUseQty;
    private Button btnUse;

    // Search tab
    private Label lblSearchName, lblSearchExp;
    private TextBox txtSearchName;
    private RadioButton rbSearchAny, rbSearchNone, rbSearchDate;
    private DateTimePicker dtpSearchExp;
    private Button btnSearch;
    private RichTextBox rtbSearchResults;

    // Date tab
    private Label lblCurrentDate;
    private DateTimePicker dtpSetDate;
    private Button btnSetDate;

    // Status bar
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;

    public MainForm()
    {
        InitializeComponent();
        RefreshList();
        UpdateDateLabel();
    }

    private void InitializeComponent()
    {
        Text = "Inventory Manager";
        Size = new Size(950, 620);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;

        // ── Status bar ──────────────────────────────────────────
        statusStrip = new StatusStrip { BackColor = Color.WhiteSmoke };
        statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(statusLabel);
        Controls.Add(statusStrip);

        // ── Main split ──────────────────────────────────────────
        splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 300,
            Panel2MinSize = 280,
            BackColor = Color.Gainsboro
        };
        Controls.Add(splitMain);
        splitMain.SplitterMoved += (s, e) => { };
        Shown += (s, e) =>
        {
            int dist = Math.Min(530, splitMain.Width - 280);
            dist = Math.Max(dist, splitMain.Panel1MinSize);
            dist = Math.Min(dist, splitMain.Width - splitMain.Panel2MinSize);
            splitMain.SplitterDistance = dist;
        };
        splitMain.BringToFront();

        // ── LEFT PANEL ──────────────────────────────────────────
        panelLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White };
        splitMain.Panel1.Controls.Add(panelLeft);

        lblInventoryTitle = new Label
        {
            Text = "Inventory",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(12, 12)
        };
        panelLeft.Controls.Add(lblInventoryTitle);

        // Sort radio buttons
        rbSortAlphabetical = new RadioButton
        {
            Text = "A–Z",
            Checked = true,
            AutoSize = true,
            Location = new Point(14, 46)
        };
        rbSortExpiration = new RadioButton
        {
            Text = "By Expiration",
            AutoSize = true,
            Location = new Point(80, 46)
        };
        rbSortAlphabetical.CheckedChanged += (s, e) => RefreshList();
        rbSortExpiration.CheckedChanged += (s, e) => RefreshList();
        panelLeft.Controls.Add(rbSortAlphabetical);
        panelLeft.Controls.Add(rbSortExpiration);

        // ListView
        listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.FixedSingle,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Location = new Point(12, 72),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 30, 30)
        };
        listView.Columns.Add("Type", 110);
        listView.Columns.Add("Name", 170);
        listView.Columns.Add("Qty", 70);
        listView.Columns.Add("Expires", 110);
        // Resize columns on form resize
        panelLeft.Resize += (s, e) =>
        {
            listView.Size = new Size(panelLeft.ClientSize.Width - 24,
                                     panelLeft.ClientSize.Height - 82);
        };
        panelLeft.Controls.Add(listView);

        // ── RIGHT PANEL ─────────────────────────────────────────
        panelRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White };
        splitMain.Panel2.Controls.Add(panelRight);

        tabControl = new TabControl
        {
            Location = new Point(12, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        panelRight.Resize += (s, e) =>
        {
            tabControl.Size = new Size(panelRight.ClientSize.Width - 24,
                                       panelRight.ClientSize.Height - 24);
        };
        panelRight.Controls.Add(tabControl);

        BuildAddTab();
        BuildUseTab();
        BuildSearchTab();
        BuildDateTab();

        tabControl.TabPages.AddRange(new[] { tabAdd, tabUse, tabSearch, tabDate });
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static Label MakeLabel(string text, Point loc) =>
        new Label { Text = text, AutoSize = true, Location = loc };

    private static TextBox MakeTextBox(Point loc, int width = 180) =>
        new TextBox { Location = loc, Width = width, BorderStyle = BorderStyle.FixedSingle };

    private static Button MakeButton(string text, Point loc) =>
        new Button
        {
            Text = text,
            Location = loc,
            Width = 110,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

    private void SetStatus(string msg) => statusLabel.Text = msg;

    // ── ADD TAB ─────────────────────────────────────────────────

    private void BuildAddTab()
    {
        tabAdd = new TabPage("Add Item") { BackColor = Color.White, Padding = new Padding(10) };

        rbNonPerishable = new RadioButton { Text = "Non-Perishable", Checked = true, AutoSize = true, Location = new Point(10, 14) };
        rbPerishable = new RadioButton { Text = "Perishable", AutoSize = true, Location = new Point(145, 14) };

        lblAddName = MakeLabel("Name:", new Point(10, 50));
        txtAddName = MakeTextBox(new Point(10, 68));

        lblAddQty = MakeLabel("Quantity:", new Point(10, 100));
        txtAddQty = MakeTextBox(new Point(10, 118), 100);

        lblAddExp = MakeLabel("Expiration Date:", new Point(10, 150));
        dtpAddExp = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(10, 168),
            Width = 150,
            Enabled = false
        };

        rbNonPerishable.CheckedChanged += (s, e) => { lblAddExp.Enabled = rbPerishable.Checked; dtpAddExp.Enabled = rbPerishable.Checked; };
        rbPerishable.CheckedChanged += (s, e) => { lblAddExp.Enabled = rbPerishable.Checked; dtpAddExp.Enabled = rbPerishable.Checked; };

        btnAdd = MakeButton("Add Item", new Point(10, 210));
        btnAdd.Click += BtnAdd_Click;

        tabAdd.Controls.AddRange(new Control[] { rbNonPerishable, rbPerishable, lblAddName, txtAddName, lblAddQty, txtAddQty, lblAddExp, dtpAddExp, btnAdd });
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        string name = txtAddName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { SetStatus("Name cannot be empty."); return; }
        if (!double.TryParse(txtAddQty.Text.Trim(), out double qty) || qty <= 0) { SetStatus("Enter a valid quantity."); return; }

        if (rbPerishable.Checked)
        {
            var exp = DateOnly.FromDateTime(dtpAddExp.Value);
            inventory.AddItem(new PerishableItem(name, qty, exp));
        }
        else
        {
            inventory.AddItem(new NonPerishableItem(name, qty));
        }

        txtAddName.Clear(); txtAddQty.Clear();
        SetStatus($"Added '{name}' (qty: {qty}).");
        RefreshList();
    }

    // ── USE TAB ─────────────────────────────────────────────────

    private void BuildUseTab()
    {
        tabUse = new TabPage("Use Item") { BackColor = Color.White, Padding = new Padding(10) };

        rbUseNonPerishable = new RadioButton { Text = "Non-Perishable", Checked = true, AutoSize = true, Location = new Point(10, 14) };
        rbUsePerishable = new RadioButton { Text = "Perishable", AutoSize = true, Location = new Point(145, 14) };

        lblUseName = MakeLabel("Name:", new Point(10, 50));
        txtUseName = MakeTextBox(new Point(10, 68));

        lblUseQty = MakeLabel("Quantity:", new Point(10, 100));
        txtUseQty = MakeTextBox(new Point(10, 118), 100);

        btnUse = MakeButton("Use Item", new Point(10, 160));
        btnUse.Click += BtnUse_Click;

        tabUse.Controls.AddRange(new Control[] { rbUseNonPerishable, rbUsePerishable, lblUseName, txtUseName, lblUseQty, txtUseQty, btnUse });
    }

    private void BtnUse_Click(object? sender, EventArgs e)
    {
        string name = txtUseName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { SetStatus("Name cannot be empty."); return; }
        if (!int.TryParse(txtUseQty.Text.Trim(), out int qty) || qty <= 0) { SetStatus("Enter a valid integer quantity."); return; }

        if (rbUsePerishable.Checked)
            inventory.UsePerishable(name, qty);
        else
            inventory.UseNonPerishable(name, qty);

        txtUseName.Clear(); txtUseQty.Clear();
        SetStatus($"Used {qty} of '{name}'.");
        RefreshList();
    }

    // ── SEARCH TAB ──────────────────────────────────────────────

    private void BuildSearchTab()
    {
        tabSearch = new TabPage("Search") { BackColor = Color.White, Padding = new Padding(10) };

        lblSearchName = MakeLabel("Name:", new Point(10, 14));
        txtSearchName = MakeTextBox(new Point(10, 32));

        lblSearchExp = MakeLabel("Expiration filter:", new Point(10, 68));
        rbSearchAny = new RadioButton { Text = "Any", Checked = true, AutoSize = true, Location = new Point(10, 86) };
        rbSearchNone = new RadioButton { Text = "Non-Perishable only", AutoSize = true, Location = new Point(65, 86) };
        rbSearchDate = new RadioButton { Text = "Specific date:", AutoSize = true, Location = new Point(10, 110) };
        dtpSearchExp = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(130, 108), Width = 140, Enabled = false };
        rbSearchDate.CheckedChanged += (s, e) => dtpSearchExp.Enabled = rbSearchDate.Checked;

        btnSearch = MakeButton("Search", new Point(10, 144));
        btnSearch.Click += BtnSearch_Click;

        rtbSearchResults = new RichTextBox
        {
            Location = new Point(10, 186),
            Size = new Size(280, 120),
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke,
            Font = new Font("Consolas", 9f),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        tabSearch.Resize += (s, e) =>
            rtbSearchResults.Size = new Size(tabSearch.ClientSize.Width - 20, tabSearch.ClientSize.Height - 196);

        tabSearch.Controls.AddRange(new Control[] { lblSearchName, txtSearchName, lblSearchExp, rbSearchAny, rbSearchNone, rbSearchDate, dtpSearchExp, btnSearch, rtbSearchResults });
    }

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        string name = txtSearchName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { SetStatus("Enter a name to search."); return; }

        // Redirect console output to capture inventory's print calls
        var sw = new System.IO.StringWriter();
        var old = Console.Out;
        Console.SetOut(sw);

        if (rbSearchAny.Checked)
            inventory.GetItemInfo(name);
        else if (rbSearchNone.Checked)
            inventory.GetItemInfo(name, null);
        else
            inventory.GetItemInfo(name, DateOnly.FromDateTime(dtpSearchExp.Value));

        Console.SetOut(old);
        rtbSearchResults.Text = sw.ToString().Trim();
        SetStatus("Search complete.");
    }

    // ── DATE TAB ────────────────────────────────────────────────

    private void BuildDateTab()
    {
        tabDate = new TabPage("Date") { BackColor = Color.White, Padding = new Padding(10) };

        lblCurrentDate = MakeLabel("", new Point(10, 14));
        lblCurrentDate.Font = new Font("Segoe UI", 10f);

        var lblSet = MakeLabel("Set new date:", new Point(10, 54));
        dtpSetDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(10, 72), Width = 160 };

        btnSetDate = MakeButton("Set Date", new Point(10, 112));
        btnSetDate.Click += BtnSetDate_Click;

        tabDate.Controls.AddRange(new Control[] { lblCurrentDate, lblSet, dtpSetDate, btnSetDate });
    }

    private void BtnSetDate_Click(object? sender, EventArgs e)
    {
        var newDate = DateOnly.FromDateTime(dtpSetDate.Value);
        inventory.SetDate(newDate);
        UpdateDateLabel();
        RefreshList();
        SetStatus($"Date set to {newDate}.");
    }

    private void UpdateDateLabel()
    {
        var sw = new System.IO.StringWriter();
        var old = Console.Out;
        Console.SetOut(sw);
        inventory.GetDate();
        Console.SetOut(old);
        lblCurrentDate.Text = sw.ToString().Trim();
    }

    // ── LIST REFRESH ────────────────────────────────────────────

    private void RefreshList()
    {
        // Capture console output from inventory list methods
        var sw = new System.IO.StringWriter();
        var old = Console.Out;
        Console.SetOut(sw);

        if (rbSortExpiration.Checked)
            inventory.ListByExpiration();
        else
            inventory.ListByAlphabetical();

        Console.SetOut(old);

        listView.Items.Clear();
        foreach (var line in sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            var lvi = new ListViewItem(parts[0].Trim());
            for (int i = 1; i < parts.Length; i++)
            {
                string cell = parts[i].Trim();
                // Strip label prefixes like "Qty:", "Expires:"
                cell = System.Text.RegularExpressions.Regex.Replace(cell, @"^(Qty:|Expires:)\s*", "");
                lvi.SubItems.Add(cell);
            }
            // Tint perishable rows subtly
            if (parts[0].Trim() == "Perishable")
                lvi.BackColor = Color.FromArgb(245, 250, 255);
            listView.Items.Add(lvi);
        }
    }
}