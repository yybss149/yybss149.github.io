using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public TabPage BuildTab()
        {
            var tab = new TabPage(Name) { Padding = new Padding(18), BackColor = System.Drawing.Color.FromArgb(247, 248, 252) };
            var hint = new Label
            {
                Text = isResource ? "输入网址和备注，名称会自动从网址生成。" : "输入产品名和槽点，写完即可保存。",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = System.Drawing.Color.FromArgb(102, 112, 133)
            };
            tab.Controls.Add(hint);

            var editor = new TableLayoutPanel { Dock = DockStyle.Top, Height = 86, BackColor = System.Drawing.Color.White, Padding = new Padding(14) };
            editor.ColumnCount = 3;
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            editor.RowCount = 2;
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.Controls.Add(new Label { Text = isResource ? "网址" : "产品名", AutoSize = true }, 0, 0);
            editor.Controls.Add(new Label { Text = isResource ? "备注" : "槽点", AutoSize = true }, 1, 0);
            firstInput = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 5, 10, 0) };
            secondInput = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 5, 10, 0) };
            editor.Controls.Add(firstInput, 0, 1);
            editor.Controls.Add(secondInput, 1, 1);
            var add = new Button { Text = isResource ? "添加资源" : "添加锐评", Width = 100, Height = 30, Anchor = AnchorStyles.None, Margin = new Padding(4, 20, 0, 0) };
            add.Click += delegate { Add(); };
            editor.Controls.Add(add, 2, 1);
            tab.Controls.Add(editor);

            var actions = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 8, 0, 0) };
            var remove = new Button { Text = isResource ? "删除选中资源" : "删除选中锐评", Dock = DockStyle.Right, Width = 126 };
            remove.Click += delegate { DeleteSelected(); };
            actions.Controls.Add(remove);
            tab.Controls.Add(actions);

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.Columns.Add("title", isResource ? "名称" : "产品名");
            if (isResource) grid.Columns.Add("url", "网址");
            grid.Columns.Add("detail", isResource ? "备注" : "槽点");
            tab.Controls.Add(grid);
            Refresh();
            return tab;
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
            var header = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(24, 17, 24, 12), BackColor = System.Drawing.Color.FromArgb(31, 27, 80) };
            Controls.Add(header);
            header.Controls.Add(new Label { Text = "知屿 · 内容管理器", AutoSize = true, ForeColor = System.Drawing.Color.White, Font = new System.Drawing.Font("Microsoft YaHei UI", 18F, System.Drawing.FontStyle.Bold), Location = new System.Drawing.Point(24, 16) });
            header.Controls.Add(new Label { Text = "管理资源库与电子锐评；保存后可一键同步到公开网站。", AutoSize = true, ForeColor = System.Drawing.Color.FromArgb(225, 220, 255), Location = new System.Drawing.Point(26, 51) });
            syncButton.Text = "同步到网站";
            syncButton.BackColor = System.Drawing.Color.FromArgb(255, 217, 152);
            syncButton.ForeColor = System.Drawing.Color.FromArgb(48, 31, 76);
            syncButton.FlatStyle = FlatStyle.Flat;
            syncButton.FlatAppearance.BorderSize = 0;
            syncButton.Size = new System.Drawing.Size(116, 34);
            syncButton.Location = new System.Drawing.Point(806, 24);
            syncButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            syncButton.Click += delegate { Sync(); };
            header.Controls.Add(syncButton);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(20, 11, 20, 0), BackColor = System.Drawing.Color.White };
            status.Text = "选择栏目后即可开始编辑。";
            status.ForeColor = System.Drawing.Color.FromArgb(102, 112, 133);
            status.AutoSize = true;
            footer.Controls.Add(status);
            Controls.Add(footer);

            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new System.Drawing.Point(18, 7) };
            sections.Add(new ContentSection("资源库", "docs\\resources\\index.md", "<!-- RESOURCE_MANAGER_START -->", "<!-- RESOURCE_MANAGER_END -->", true, root, SetStatus));
            sections.Add(new ContentSection("电子锐评", "docs\\reviews\\index.md", "<!-- REVIEW_MANAGER_START -->", "<!-- REVIEW_MANAGER_END -->", false, root, SetStatus));
            foreach (ContentSection section in sections) tabs.TabPages.Add(section.BuildTab());
            Controls.Add(tabs);
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
