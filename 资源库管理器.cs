using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ZhiyuResourceManager
{
    public class Resource
    {
        public string Title;
        public string Url;
        public string Note;
    }

    public class MainForm : Form
    {
        private const string StartMarker = "<!-- RESOURCE_MANAGER_START -->";
        private const string EndMarker = "<!-- RESOURCE_MANAGER_END -->";
        private readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        private readonly List<Resource> resources = new List<Resource>();
        private readonly TextBox urlBox = new TextBox();
        private readonly TextBox noteBox = new TextBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label status = new Label();
        private readonly Button syncButton = new Button();

        private string DocumentPath
        {
            get { return Path.Combine(root, "docs", "resources", "index.md"); }
        }

        public MainForm()
        {
            Text = "知屿 · 资源库管理器";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(760, 500);
            Size = new System.Drawing.Size(900, 600);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            BackColor = System.Drawing.Color.FromArgb(247, 248, 252);

            BuildInterface();
            RefreshResources();
        }

        private void BuildInterface()
        {
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = BackColor };
            Controls.Add(main);

            var title = new Label
            {
                Text = "知屿 · 资源库管理器",
                Font = new System.Drawing.Font("Microsoft YaHei UI", 18F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(23, 32, 51),
                AutoSize = true,
                Location = new System.Drawing.Point(24, 20)
            };
            main.Controls.Add(title);
            var subtitle = new Label
            {
                Text = "输入网址和备注即可保存；点击同步会自动更新公开网站。",
                ForeColor = System.Drawing.Color.FromArgb(102, 112, 133),
                AutoSize = true,
                Location = new System.Drawing.Point(26, 58)
            };
            main.Controls.Add(subtitle);

            var inputPanel = new Panel { Location = new System.Drawing.Point(24, 92), Height = 82, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = System.Drawing.Color.White };
            main.Controls.Add(inputPanel);
            var urlLabel = new Label { Text = "网址", AutoSize = true, Location = new System.Drawing.Point(16, 12) };
            inputPanel.Controls.Add(urlLabel);
            urlBox.Location = new System.Drawing.Point(16, 36);
            urlBox.Width = 310;
            urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            inputPanel.Controls.Add(urlBox);
            var noteLabel = new Label { Text = "备注", AutoSize = true, Location = new System.Drawing.Point(344, 12) };
            inputPanel.Controls.Add(noteLabel);
            noteBox.Location = new System.Drawing.Point(344, 36);
            noteBox.Width = 310;
            noteBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            inputPanel.Controls.Add(noteBox);
            var addButton = new Button { Text = "添加资源", Location = new System.Drawing.Point(672, 34), Width = 100, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            addButton.Click += delegate { AddResource(); };
            inputPanel.Controls.Add(addButton);

            var toolbar = new Panel { Location = new System.Drawing.Point(24, 184), Height = 42, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = BackColor };
            main.Controls.Add(toolbar);
            status.Text = "输入网址和备注后，点击“添加资源”。";
            status.ForeColor = System.Drawing.Color.FromArgb(102, 112, 133);
            status.AutoSize = true;
            status.Location = new System.Drawing.Point(2, 12);
            toolbar.Controls.Add(status);
            syncButton.Text = "同步到网站";
            syncButton.BackColor = System.Drawing.Color.FromArgb(36, 73, 216);
            syncButton.ForeColor = System.Drawing.Color.White;
            syncButton.FlatStyle = FlatStyle.Flat;
            syncButton.FlatAppearance.BorderSize = 0;
            syncButton.Size = new System.Drawing.Size(110, 32);
            syncButton.Location = new System.Drawing.Point(660, 4);
            syncButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            syncButton.Click += delegate { Sync(); };
            toolbar.Controls.Add(syncButton);
            var deleteButton = new Button { Text = "删除选中资源", Size = new System.Drawing.Size(120, 32), Location = new System.Drawing.Point(530, 4), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            deleteButton.Click += delegate { DeleteSelected(); };
            toolbar.Controls.Add(deleteButton);

            grid.Location = new System.Drawing.Point(24, 234);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.Size = new System.Drawing.Size(828, 290);
            grid.BackgroundColor = System.Drawing.Color.White;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoGenerateColumns = false;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add("name", "名称");
            grid.Columns.Add("url", "网址");
            grid.Columns.Add("note", "备注");
            main.Controls.Add(grid);

            main.Resize += delegate
            {
                inputPanel.Width = main.ClientSize.Width - 48;
                toolbar.Width = main.ClientSize.Width - 48;
                grid.Size = new System.Drawing.Size(main.ClientSize.Width - 48, main.ClientSize.Height - 258);
            };
        }

        private void RefreshResources()
        {
            try
            {
                resources.Clear();
                string content = File.ReadAllText(DocumentPath, Encoding.UTF8);
                int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
                int end = content.IndexOf(EndMarker, StringComparison.Ordinal);
                if (start < 0 || end < 0 || end <= start) throw new Exception("找不到资源库标记，请不要删除 RESOURCE_MANAGER_START/END 两行。");
                string resourceText = content.Substring(start + StartMarker.Length, end - start - StartMarker.Length);
                Regex pattern = new Regex(@"^\s*-\s+\[([^\]]+)\]\(([^)]+)\)：\s*(.*)\s*$");
                foreach (string line in resourceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    Match match = pattern.Match(line);
                    if (match.Success) resources.Add(new Resource { Title = match.Groups[1].Value, Url = match.Groups[2].Value, Note = match.Groups[3].Value });
                }
                grid.Rows.Clear();
                foreach (Resource resource in resources) grid.Rows.Add(resource.Title, resource.Url, resource.Note);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "无法读取资源库", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveResources()
        {
            string content = File.ReadAllText(DocumentPath, Encoding.UTF8);
            int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
            int end = content.IndexOf(EndMarker, StringComparison.Ordinal);
            if (start < 0 || end < 0 || end <= start) throw new Exception("找不到资源库标记。");
            var builder = new StringBuilder();
            builder.Append(content.Substring(0, start + StartMarker.Length));
            builder.AppendLine();
            builder.AppendLine();
            foreach (Resource resource in resources) builder.AppendLine("- [" + resource.Title + "](" + resource.Url + ")：" + resource.Note);
            builder.AppendLine();
            builder.Append(content.Substring(end));
            File.WriteAllText(DocumentPath, builder.ToString(), new UTF8Encoding(false));
        }

        private void AddResource()
        {
            string url = urlBox.Text.Trim();
            string note = Regex.Replace(noteBox.Text.Trim(), @"\s+", " ");
            if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("请输入有效网址，例如 https://example.com", "网址不正确", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (String.IsNullOrWhiteSpace(note) || url.Contains(")") || note.Contains("]"))
            {
                MessageBox.Show("请填写备注；网址中不能包含 )，备注中不能包含 ]。", "无法添加资源", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            resources.Add(new Resource { Title = parsed.Host.StartsWith("www.") ? parsed.Host.Substring(4) : parsed.Host, Url = url, Note = note });
            try { SaveResources(); }
            catch (Exception error) { MessageBox.Show(error.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            urlBox.Clear();
            noteBox.Clear();
            RefreshResources();
            status.Text = "已保存到本机资源库。点击“同步到网站”即可发布。";
            urlBox.Focus();
        }

        private void DeleteSelected()
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("请先在列表中选择一条资源。", "请选择资源", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int index = grid.SelectedRows[0].Index;
            if (MessageBox.Show("确定删除「" + resources[index].Title + "」吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            resources.RemoveAt(index);
            try { SaveResources(); }
            catch (Exception error) { MessageBox.Show(error.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            RefreshResources();
            status.Text = "已从本机资源库删除。点击“同步到网站”即可发布。";
        }

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
            status.Text = "正在同步到 GitHub，请稍候…";
            System.Threading.Thread thread = new System.Threading.Thread(delegate()
            {
                try
                {
                    string changes = RunGit("status --porcelain -- docs/resources/index.md");
                    string message;
                    if (String.IsNullOrWhiteSpace(changes)) message = "没有新的资源改动，网站已经是最新状态。";
                    else
                    {
                        RunGit("add -- docs/resources/index.md");
                        RunGit("commit -m \"Update resource library\"");
                        RunGit("push");
                        message = "已同步到 GitHub。网站通常会在 1～3 分钟内更新。";
                    }
                    BeginInvoke((Action)delegate { syncButton.Enabled = true; status.Text = message; MessageBox.Show(message, "同步完成", MessageBoxButtons.OK, MessageBoxIcon.Information); });
                }
                catch (Exception error)
                {
                    BeginInvoke((Action)delegate { syncButton.Enabled = true; status.Text = "同步失败，请查看提示。"; MessageBox.Show(error.Message, "同步失败", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                }
            });
            thread.IsBackground = true;
            thread.Start();
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
