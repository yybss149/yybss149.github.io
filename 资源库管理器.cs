using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace ZhiyuManager
{
    public class Entry
    {
        public string Title;
        public string Detail;
        public string Url;
    }

    public class GradientPanel : Panel
    {
        public System.Drawing.Color StartColor = System.Drawing.Color.FromArgb(32, 27, 82);
        public System.Drawing.Color EndColor = System.Drawing.Color.FromArgb(105, 58, 132);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle, StartColor, EndColor, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }
    }

    public class StyledTabControl : TabControl
    {
        public StyledTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            Appearance = TabAppearance.FlatButtons;
            SizeMode = TabSizeMode.Fixed;
            // 栏目切换由窗口顶部的导航按钮负责，这里只作为内容容器。
            ItemSize = new System.Drawing.Size(0, 1);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            var rect = GetTabRect(e.Index);
            bool selected = e.Index == SelectedIndex;
            using (var brush = new System.Drawing.SolidBrush(selected ? System.Drawing.Color.FromArgb(67, 52, 143) : System.Drawing.Color.FromArgb(238, 235, 250)))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            var color = selected ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(93, 82, 132);
            TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, rect, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class ContentSection
    {
        public readonly string Name;
        public readonly string DocumentRelativePath;
        private readonly string startMarker;
        private readonly string endMarker;
        private readonly bool isResource;
        private readonly string root;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly Action<string> setStatus;
        private DataGridView grid;
        private TextBox firstInput;
        private TextBox secondInput;

        public ContentSection(string name, string documentRelativePath, string startMarker, string endMarker, bool isResource, string root, Action<string> setStatus)
        {
            Name = name;
            DocumentRelativePath = documentRelativePath;
            this.startMarker = startMarker;
            this.endMarker = endMarker;
            this.isResource = isResource;
            this.root = root;
            this.setStatus = setStatus;
        }

        private string DocumentPath { get { return Path.Combine(root, DocumentRelativePath); } }
        private string WebsiteUrl { get { return isResource ? "https://yybss149.github.io/resources/" : "https://yybss149.github.io/reviews/"; } }

        public TabPage BuildTab()
        {
            var tab = new TabPage(Name) { Padding = new Padding(0), BackColor = System.Drawing.Color.FromArgb(247, 248, 252) };
            // 用表格分区固定上方编辑区和下方列表区，避免窗口缩放时按钮被遮住。
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = tab.BackColor,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tab.Controls.Add(layout);
            var hint = new Label
            {
                Text = isResource ? "输入网址和备注，名称会自动从网址生成。" : "纯主观无恶意 · 输入产品名和槽点，写完即可保存。",
                Dock = DockStyle.Fill,
                ForeColor = isResource ? System.Drawing.Color.FromArgb(100, 81, 170) : System.Drawing.Color.FromArgb(173, 69, 96),
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold)
            };
            layout.Controls.Add(hint, 0, 0);

            var editor = new TableLayoutPanel { Dock = DockStyle.Top, Height = 86, BackColor = System.Drawing.Color.White, Padding = new Padding(14), Margin = new Padding(0, 0, 0, 10) };
            editor.ColumnCount = 3;
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            editor.RowCount = 2;
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.Controls.Add(new Label { Text = isResource ? "网址" : "产品名", AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(77, 68, 112) }, 0, 0);
            editor.Controls.Add(new Label { Text = isResource ? "备注" : "槽点", AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(77, 68, 112) }, 1, 0);
            firstInput = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 5, 10, 0), BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.FromArgb(252, 251, 255) };
            secondInput = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 5, 10, 0), BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.FromArgb(252, 251, 255) };
            editor.Controls.Add(firstInput, 0, 1);
            editor.Controls.Add(secondInput, 1, 1);
            // 第二行的可用高度有限，使用小间距，避免按钮只露出一条色块。
            var add = new Button { Text = isResource ? "添加资源" : "添加锐评", Width = 100, Height = 28, Anchor = AnchorStyles.None, Margin = new Padding(4, 4, 0, 0), FlatStyle = FlatStyle.Flat, ForeColor = System.Drawing.Color.White, BackColor = isResource ? System.Drawing.Color.FromArgb(100, 81, 190) : System.Drawing.Color.FromArgb(195, 73, 101) };
            add.FlatAppearance.BorderSize = 0;
            add.Click += delegate { Add(); };
            editor.Controls.Add(add, 2, 1);
            layout.Controls.Add(editor, 0, 1);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 7, 0, 0), BackColor = tab.BackColor };
            var remove = new Button { Text = isResource ? "删除选中资源" : "删除选中锐评", Width = 126, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.White, ForeColor = System.Drawing.Color.FromArgb(155, 76, 100), Margin = new Padding(8, 0, 0, 0) };
            remove.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(229, 197, 207);
            remove.Click += delegate { DeleteSelected(); };
            actions.Controls.Add(remove);
            var openSite = new Button { Text = isResource ? "打开资料库网页" : "打开锐评网页", Width = 126, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(246, 243, 255), ForeColor = System.Drawing.Color.FromArgb(91, 72, 163), Margin = new Padding(8, 0, 0, 0) };
            openSite.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(211, 202, 239);
            openSite.Click += delegate { OpenWebsite(); };
            actions.Controls.Add(openSite);
            layout.Controls.Add(actions, 0, 2);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = System.Drawing.Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = System.Drawing.Color.FromArgb(235, 230, 244)
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = System.Drawing.Color.FromArgb(43, 35, 93), ForeColor = System.Drawing.Color.White, Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft };
            grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = System.Drawing.Color.White, ForeColor = System.Drawing.Color.FromArgb(64, 55, 92), SelectionBackColor = isResource ? System.Drawing.Color.FromArgb(235, 230, 255) : System.Drawing.Color.FromArgb(255, 231, 237), SelectionForeColor = System.Drawing.Color.FromArgb(57, 44, 92), Padding = new Padding(4, 0, 4, 0) };
            grid.ColumnHeadersHeight = 38;
            grid.RowTemplate.Height = 36;
            grid.Columns.Add("title", isResource ? "名称" : "产品名");
            if (isResource) grid.Columns.Add("url", "网址");
            grid.Columns.Add("detail", isResource ? "备注" : "槽点");
            layout.Controls.Add(new Label
            {
                Text = isResource ? "网站当前显示的资料" : "网站当前显示的电子锐评",
                Dock = DockStyle.Fill,
                Padding = new Padding(2, 7, 0, 0),
                ForeColor = System.Drawing.Color.FromArgb(77, 68, 112),
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold)
            }, 0, 3);
            layout.Controls.Add(grid, 0, 4);
            Refresh();
            return tab;
        }

        private void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = WebsiteUrl, UseShellExecute = true });
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "无法打开网页", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Refresh()
        {
            try
            {
                entries.Clear();
                string content = File.ReadAllText(DocumentPath, Encoding.UTF8);
                int start = content.IndexOf(startMarker, StringComparison.Ordinal);
                int end = content.IndexOf(endMarker, StringComparison.Ordinal);
                if (start < 0 || end < 0 || end <= start) throw new Exception("找不到“" + Name + "”的内容标记，请不要删除管理器标记。 ");
                string section = content.Substring(start + startMarker.Length, end - start - startMarker.Length);
                Regex pattern = isResource
                    ? new Regex(@"^\s*-\s+\[([^\]]+)\]\(([^)]+)\)：\s*(.*)\s*$")
                    : new Regex(@"^\s*-\s+\*\*([^*]+)\*\*：\s*(.*)\s*$");
                foreach (string line in section.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    Match match = pattern.Match(line);
                    if (match.Success) entries.Add(new Entry { Title = match.Groups[1].Value, Url = isResource ? match.Groups[2].Value : "", Detail = isResource ? match.Groups[3].Value : match.Groups[2].Value });
                }
                if (grid != null)
                {
                    grid.Rows.Clear();
                    foreach (Entry entry in entries)
                    {
                        if (isResource) grid.Rows.Add(entry.Title, entry.Url, entry.Detail);
                        else grid.Rows.Add(entry.Title, entry.Detail);
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "无法读取" + Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Save()
        {
            string content = File.ReadAllText(DocumentPath, Encoding.UTF8);
            int start = content.IndexOf(startMarker, StringComparison.Ordinal);
            int end = content.IndexOf(endMarker, StringComparison.Ordinal);
            if (start < 0 || end < 0 || end <= start) throw new Exception("找不到“" + Name + "”的内容标记。 ");
            var builder = new StringBuilder();
            builder.Append(content.Substring(0, start + startMarker.Length));
            builder.AppendLine();
            builder.AppendLine();
            foreach (Entry entry in entries)
            {
                if (isResource) builder.AppendLine("- [" + entry.Title + "](" + entry.Url + ")：" + entry.Detail);
                else builder.AppendLine("- **" + entry.Title + "**：" + entry.Detail);
            }
            builder.AppendLine();
            builder.Append(content.Substring(end));
            File.WriteAllText(DocumentPath, builder.ToString(), new UTF8Encoding(false));
        }

        private void Add()
        {
            string first = Regex.Replace(firstInput.Text.Trim(), @"\s+", " ");
            string detail = Regex.Replace(secondInput.Text.Trim(), @"\s+", " ");
            if (String.IsNullOrWhiteSpace(first) || String.IsNullOrWhiteSpace(detail))
            {
                MessageBox.Show("请把两项都填写完整。", "无法添加", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (isResource)
                {
                    if (!first.StartsWith("http://") && !first.StartsWith("https://")) first = "https://" + first;
                    Uri parsed;
                    if (!Uri.TryCreate(first, UriKind.Absolute, out parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)) throw new Exception("请输入有效网址，例如 https://example.com");
                    if (first.Contains(")") || detail.Contains("]")) throw new Exception("网址中不能包含 )，备注中不能包含 ]。 ");
                    entries.Add(new Entry { Title = parsed.Host.StartsWith("www.") ? parsed.Host.Substring(4) : parsed.Host, Url = first, Detail = detail });
                }
                else
                {
                    if (first.Contains("*") || detail.Contains("*") || first.Contains("：") || first.Contains(":")) throw new Exception("产品名和槽点中不能包含 *、中文冒号或英文冒号。 ");
                    entries.Add(new Entry { Title = first, Detail = detail });
                }
                Save();
                firstInput.Clear();
                secondInput.Clear();
                Refresh();
                setStatus("已保存“" + Name + "”内容，点击右上角“同步到网站”即可发布。");
                firstInput.Focus();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "无法添加", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeleteSelected()
        {
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先在列表中选择一条内容。", "请选择内容", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int index = grid.SelectedRows[0].Index;
            if (MessageBox.Show("确定删除「" + entries[index].Title + "」吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                entries.RemoveAt(index);
                Save();
                Refresh();
                setStatus("已从“" + Name + "”删除，点击同步即可更新网站。");
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class MainForm : Form
    {
        private readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        private readonly Label status = new Label();
        private readonly Button syncButton = new Button();
        private readonly List<ContentSection> sections = new List<ContentSection>();
        private StyledTabControl tabs;
        private Button resourcesNavButton;
        private Button reviewsNavButton;

        public MainForm()
        {
            Text = "知屿 · 内容管理器";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(820, 540);
            Size = new System.Drawing.Size(960, 650);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            BackColor = System.Drawing.Color.FromArgb(247, 248, 252);
            BuildInterface();
        }

        private void BuildInterface()
        {
            tabs = new StyledTabControl { Dock = DockStyle.Fill, Padding = new System.Drawing.Point(0, 0) };
            sections.Add(new ContentSection("资料库", "docs\\resources\\index.md", "<!-- RESOURCE_MANAGER_START -->", "<!-- RESOURCE_MANAGER_END -->", true, root, SetStatus));
            sections.Add(new ContentSection("电子锐评", "docs\\reviews\\index.md", "<!-- REVIEW_MANAGER_START -->", "<!-- REVIEW_MANAGER_END -->", false, root, SetStatus));
            foreach (ContentSection section in sections) tabs.TabPages.Add(section.BuildTab());

            // 使用纯色标题栏，避免 Windows 标签控件在渐变底色上出现浅色底块。
            var header = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(24, 17, 24, 12), BackColor = System.Drawing.Color.FromArgb(38, 31, 92) };
            header.Controls.Add(new Label { Text = "知屿 · 内容管理器", AutoSize = true, BackColor = header.BackColor, ForeColor = System.Drawing.Color.White, Font = new System.Drawing.Font("Microsoft YaHei UI", 18F, System.Drawing.FontStyle.Bold), Location = new System.Drawing.Point(24, 16) });
            header.Controls.Add(new Label { Text = "管理资料库与电子锐评；保存后可一键同步到公开网站。", AutoSize = true, BackColor = header.BackColor, ForeColor = System.Drawing.Color.FromArgb(235, 231, 255), Location = new System.Drawing.Point(26, 51) });

            var navigation = new FlowLayoutPanel { Location = new System.Drawing.Point(332, 29), Size = new System.Drawing.Size(250, 38), BackColor = header.BackColor, WrapContents = false };
            resourcesNavButton = CreateNavigationButton("资料库");
            reviewsNavButton = CreateNavigationButton("电子锐评");
            resourcesNavButton.Click += delegate { ShowSection(0); };
            reviewsNavButton.Click += delegate { ShowSection(1); };
            navigation.Controls.Add(resourcesNavButton);
            navigation.Controls.Add(reviewsNavButton);
            header.Controls.Add(navigation);

            syncButton.Text = "同步到网站";
            syncButton.BackColor = System.Drawing.Color.FromArgb(255, 217, 152);
            syncButton.ForeColor = System.Drawing.Color.FromArgb(48, 31, 76);
            syncButton.FlatStyle = FlatStyle.Flat;
            syncButton.FlatAppearance.BorderSize = 0;
            syncButton.Size = new System.Drawing.Size(116, 34);
            syncButton.Location = new System.Drawing.Point(806, 24);
            // 标题栏在加入窗体后才得到实际宽度；手动计算右边距，避免按钮跑到窗口外。
            syncButton.Anchor = AnchorStyles.Top;
            header.Resize += delegate
            {
                syncButton.Left = Math.Max(600, header.ClientSize.Width - syncButton.Width - 22);
            };
            syncButton.Click += delegate { Sync(); };
            header.Controls.Add(syncButton);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(20, 11, 20, 0), BackColor = System.Drawing.Color.FromArgb(253, 251, 255) };
            status.Text = "选择栏目后即可开始编辑。";
            status.ForeColor = System.Drawing.Color.FromArgb(102, 112, 133);
            status.AutoSize = true;
            footer.Controls.Add(status);

            // Dock 顺序固定为：顶部栏、内容区、底部状态栏，彼此不会遮盖。
            Controls.Add(tabs);
            Controls.Add(footer);
            Controls.Add(header);
            syncButton.Left = Math.Max(600, header.ClientSize.Width - syncButton.Width - 22);
            ShowSection(0);
        }

        private Button CreateNavigationButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = text == "资料库" ? 92 : 112,
                Height = 32,
                Margin = new Padding(0, 2, 8, 0),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            return button;
        }

        private void ShowSection(int index)
        {
            if (tabs == null || tabs.TabPages.Count <= index) return;
            tabs.SelectedIndex = index;
            StyleNavigationButton(resourcesNavButton, index == 0);
            StyleNavigationButton(reviewsNavButton, index == 1);
            SetStatus(index == 0 ? "正在编辑资料库。" : "正在编辑电子锐评。");
        }

        private void StyleNavigationButton(Button button, bool selected)
        {
            if (button == null) return;
            button.BackColor = selected ? System.Drawing.Color.FromArgb(255, 217, 152) : System.Drawing.Color.FromArgb(59, 49, 122);
            button.ForeColor = selected ? System.Drawing.Color.FromArgb(48, 31, 76) : System.Drawing.Color.FromArgb(238, 235, 255);
        }

        private void SetStatus(string message) { status.Text = message; }

        private string RunGit(string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = root;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception(String.IsNullOrWhiteSpace(error) ? output : error);
            return output.Trim();
        }

        private void Sync()
        {
            syncButton.Enabled = false;
            SetStatus("正在同步到 GitHub，请稍候…");
            new Thread(delegate()
            {
                try
                {
                    const string files = "docs/resources/index.md docs/reviews/index.md";
                    string changes = RunGit("status --porcelain -- " + files);
                    string message;
                    if (String.IsNullOrWhiteSpace(changes)) message = "没有新的资源或锐评内容，网站已经是最新状态。";
                    else
                    {
                        RunGit("add -- " + files);
                        RunGit("commit -m \"Update site content\"");
                        RunGit("push");
                        message = "已同步到 GitHub。网站通常会在 1～3 分钟内更新。";
                    }
                    BeginInvoke((Action)delegate { syncButton.Enabled = true; SetStatus(message); MessageBox.Show(message, "同步完成", MessageBoxButtons.OK, MessageBoxIcon.Information); });
                }
                catch (Exception error)
                {
                    BeginInvoke((Action)delegate { syncButton.Enabled = true; SetStatus("同步失败，请查看提示。"); MessageBox.Show(error.Message, "同步失败", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                }
            }) { IsBackground = true }.Start();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
