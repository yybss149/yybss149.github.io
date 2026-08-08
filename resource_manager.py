"""知屿资源库管理器：在浏览器中添加、删除资源，并自动保存 Markdown 文件。"""

from __future__ import annotations

import json
import re
import subprocess
import threading
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse


HOST = "127.0.0.1"
PORT = 8001
DOCUMENT = Path(__file__).parent / "docs" / "resources" / "index.md"
ROOT = Path(__file__).parent
START = "<!-- RESOURCE_MANAGER_START -->"
END = "<!-- RESOURCE_MANAGER_END -->"
RESOURCE_PATTERN = re.compile(r"^\s*-\s+\[([^\]]+)\]\(([^)]+)\)：\s*(.*)\s*$")
SYNC_LOCK = threading.Lock()


def read_document() -> tuple[str, list[dict[str, str]], str]:
    """读出标记中间的资源，并保留标记外的原始内容。"""
    content = DOCUMENT.read_text(encoding="utf-8")
    try:
        before, remainder = content.split(START, 1)
        resource_text, after = remainder.split(END, 1)
    except ValueError as error:
        raise RuntimeError("找不到资源库标记，请不要删除 RESOURCE_MANAGER_START/END 两行。") from error

    resources: list[dict[str, str]] = []
    for line in resource_text.splitlines():
        match = RESOURCE_PATTERN.match(line)
        if match:
            title, url, note = match.groups()
            resources.append({"title": title, "url": url, "note": note})
    return before, resources, after


def write_resources(resources: list[dict[str, str]]) -> None:
    before, _, after = read_document()
    lines = [START, ""]
    for resource in resources:
        lines.append(f"- [{resource['title']}]({resource['url']})：{resource['note']}")
    lines.extend(["", END])
    DOCUMENT.write_text(before + "\n".join(lines) + after, encoding="utf-8")


def clean_url(value: str) -> str:
    value = value.strip()
    if not value.startswith(("http://", "https://")):
        value = "https://" + value
    parsed = urlparse(value)
    if parsed.scheme not in {"http", "https"} or not parsed.netloc:
        raise ValueError("请输入有效网址，例如 https://example.com")
    return value


def create_resource(url: str, note: str) -> dict[str, str]:
    url = clean_url(url)
    note = " ".join(note.split())
    if not note:
        raise ValueError("请填写一句备注，方便以后记得这个资源的用途。")
    if ")" in url or "]" in note:
        raise ValueError("网址或备注中不能包含 ) 或 ] 字符。")
    title = urlparse(url).netloc.removeprefix("www.")
    return {"title": title, "url": url, "note": note}


def run_git(*arguments: str) -> str:
    """运行固定的 Git 命令；用户输入不会参与任何命令。"""
    result = subprocess.run(
        ["git", *arguments],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if result.returncode != 0:
        message = (result.stderr or result.stdout).strip()
        raise RuntimeError(message or "Git 操作失败，请检查网络和 GitHub 登录状态。")
    return result.stdout.strip()


def sync_to_github() -> str:
    """仅提交资源库文件，再推送到已关联的 GitHub 仓库。"""
    with SYNC_LOCK:
        changes = run_git("status", "--porcelain", "--", "docs/resources/index.md")
        if not changes:
            return "没有新的资源改动，网站已经是最新状态。"
        run_git("add", "--", "docs/resources/index.md")
        run_git("commit", "-m", "Update resource library")
        run_git("push")
        return "已同步到 GitHub。网站通常会在 1～3 分钟内更新。"


PAGE = """<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>知屿 · 资源库管理器</title>
  <style>
    :root { color-scheme: light; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; color: #172033; background: #f7f8fc; }
    body { margin: 0; }
    main { max-width: 760px; margin: 0 auto; padding: 64px 24px; }
    h1 { font-size: 2.4rem; margin: 0 0 8px; }
    .intro { color: #667085; margin: 0 0 36px; }
    section { background: #fff; border: 1px solid #e6e8ef; border-radius: 16px; padding: 24px; box-shadow: 0 8px 30px #1c2b4a0a; }
    form { display: grid; grid-template-columns: 1fr 1.2fr auto; gap: 12px; }
    .actions { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-top: 16px; }
    input { min-width: 0; padding: 12px; border: 1px solid #cfd5e1; border-radius: 9px; font: inherit; }
    button { border: 0; border-radius: 9px; padding: 12px 16px; background: #2449d8; color: #fff; font: inherit; cursor: pointer; }
    button:hover { background: #1939b7; }
    #message { min-height: 24px; color: #357342; margin: 14px 0 0; }
    ul { padding: 0; list-style: none; margin: 28px 0 0; }
    li { display: flex; align-items: center; gap: 14px; padding: 16px 0; border-top: 1px solid #edf0f5; }
    .resource { min-width: 0; flex: 1; }
    a { color: #2449d8; font-weight: 600; text-decoration: none; overflow-wrap: anywhere; }
    .note { margin: 5px 0 0; color: #667085; overflow-wrap: anywhere; }
    .delete { background: #fff; color: #c33737; border: 1px solid #efc4c4; white-space: nowrap; }
    .delete:hover { background: #fff4f4; }
    .empty { color: #667085; text-align: center; padding: 26px; }
    .footnote { color: #667085; font-size: .9rem; margin-top: 22px; }
    @media (max-width: 620px) { form { grid-template-columns: 1fr; } main { padding: 36px 16px; } }
  </style>
</head>
<body>
  <main>
    <h1>知屿 · 资源库管理器</h1>
    <p class="intro">添加或删除后会立刻保存到网站的资源库文件。</p>
    <section>
      <form id="add-form">
        <input id="url" type="url" placeholder="网址，例如 https://example.com" required>
        <input id="note" type="text" placeholder="备注，例如：一个好用的设计工具" required>
        <button type="submit">添加资源</button>
      </form>
      <div class="actions">
        <p id="message" aria-live="polite"></p>
        <button id="sync" type="button">同步到网站</button>
      </div>
      <ul id="resource-list"></ul>
    </section>
    <p class="footnote">点击“同步到网站”后，GitHub Pages 通常会在 1～3 分钟内完成更新。</p>
  </main>
  <script>
    const list = document.querySelector('#resource-list');
    const message = document.querySelector('#message');
    const form = document.querySelector('#add-form');
    const syncButton = document.querySelector('#sync');

    function showMessage(text, isError = false) {
      message.textContent = text;
      message.style.color = isError ? '#c33737' : '#357342';
    }

    function render(resources) {
      list.textContent = '';
      if (!resources.length) {
        const item = document.createElement('li');
        item.className = 'empty';
        item.textContent = '还没有资源，先添加第一条吧。';
        list.append(item);
        return;
      }
      resources.forEach((resource, index) => {
        const item = document.createElement('li');
        const box = document.createElement('div');
        box.className = 'resource';
        const link = document.createElement('a');
        link.href = resource.url;
        link.target = '_blank';
        link.rel = 'noopener';
        link.textContent = resource.title;
        const note = document.createElement('p');
        note.className = 'note';
        note.textContent = resource.note;
        const remove = document.createElement('button');
        remove.className = 'delete';
        remove.textContent = '删除';
        remove.onclick = async () => {
          if (!confirm(`确定删除「${resource.title}」吗？`)) return;
          const response = await fetch(`/api/resources/${index}`, { method: 'DELETE' });
          const data = await response.json();
          if (!response.ok) return showMessage(data.error, true);
          render(data.resources);
          showMessage('已删除并保存。');
        };
        box.append(link, note);
        item.append(box, remove);
        list.append(item);
      });
    }

    async function load() {
      const response = await fetch('/api/resources');
      const data = await response.json();
      if (!response.ok) return showMessage(data.error, true);
      render(data.resources);
    }

    form.onsubmit = async (event) => {
      event.preventDefault();
      const response = await fetch('/api/resources', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: document.querySelector('#url').value, note: document.querySelector('#note').value })
      });
      const data = await response.json();
      if (!response.ok) return showMessage(data.error, true);
      form.reset();
      render(data.resources);
      showMessage('已添加并保存。');
    };

    syncButton.onclick = async () => {
      syncButton.disabled = true;
      syncButton.textContent = '正在同步…';
      showMessage('正在提交并推送到 GitHub…');
      try {
        const response = await fetch('/api/sync', { method: 'POST' });
        const data = await response.json();
        showMessage(data.message || data.error, !response.ok);
      } catch (error) {
        showMessage('无法连接管理器，请稍后重试。', true);
      } finally {
        syncButton.disabled = false;
        syncButton.textContent = '同步到网站';
      }
    };
    load();
  </script>
</body>
</html>"""


class ResourceManagerHandler(BaseHTTPRequestHandler):
    def respond_json(self, status: HTTPStatus, data: dict) -> None:
        body = json.dumps(data, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:
        if self.path == "/":
            body = PAGE.encode("utf-8")
            self.send_response(HTTPStatus.OK)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
        elif self.path == "/api/resources":
            try:
                _, resources, _ = read_document()
                self.respond_json(HTTPStatus.OK, {"resources": resources})
            except RuntimeError as error:
                self.respond_json(HTTPStatus.INTERNAL_SERVER_ERROR, {"error": str(error)})
        else:
            self.respond_json(HTTPStatus.NOT_FOUND, {"error": "找不到此页面。"})

    def do_POST(self) -> None:
        if self.path == "/api/sync":
            try:
                message = sync_to_github()
                return self.respond_json(HTTPStatus.OK, {"message": message})
            except RuntimeError as error:
                return self.respond_json(HTTPStatus.BAD_REQUEST, {"error": str(error)})
        if self.path != "/api/resources":
            return self.respond_json(HTTPStatus.NOT_FOUND, {"error": "找不到此页面。"})
        try:
            length = int(self.headers.get("Content-Length", "0"))
            payload = json.loads(self.rfile.read(length).decode("utf-8"))
            resource = create_resource(str(payload.get("url", "")), str(payload.get("note", "")))
            _, resources, _ = read_document()
            resources.append(resource)
            write_resources(resources)
            self.respond_json(HTTPStatus.CREATED, {"resources": resources})
        except (ValueError, json.JSONDecodeError, RuntimeError) as error:
            self.respond_json(HTTPStatus.BAD_REQUEST, {"error": str(error)})

    def do_DELETE(self) -> None:
        match = re.fullmatch(r"/api/resources/(\d+)", self.path)
        if not match:
            return self.respond_json(HTTPStatus.NOT_FOUND, {"error": "找不到此资源。"})
        try:
            index = int(match.group(1))
            _, resources, _ = read_document()
            resource = resources.pop(index)
            write_resources(resources)
            self.respond_json(HTTPStatus.OK, {"resources": resources, "deleted": resource})
        except IndexError:
            self.respond_json(HTTPStatus.NOT_FOUND, {"error": "这条资源已经不存在。"})
        except RuntimeError as error:
            self.respond_json(HTTPStatus.BAD_REQUEST, {"error": str(error)})

    def log_message(self, format: str, *args: object) -> None:
        return  # 不在命令行重复显示浏览器请求记录


if __name__ == "__main__":
    print(f"资源库管理器已启动： http://{HOST}:{PORT}")
    print("按 Ctrl+C 可以停止。")
    ThreadingHTTPServer((HOST, PORT), ResourceManagerHandler).serve_forever()
