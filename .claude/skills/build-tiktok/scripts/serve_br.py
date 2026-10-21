"""Serve a Unity WebGL build folder with the Content-Encoding headers its .br/.gz files need.

Usage: python3 serve_br.py <build-folder> [port]
Opening index.html through file:// fails ("Failed to download file Build/*.data.br").
"""
import http.server
import os
import sys


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        path = self.path.split("?")[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base = path[:-3] if path.endswith((".br", ".gz")) else path
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        return "application/octet-stream" if base != path else super().guess_type(path)


if __name__ == "__main__":
    os.chdir(sys.argv[1])
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 8765
    print(f"http://127.0.0.1:{port}/index.html")
    http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
