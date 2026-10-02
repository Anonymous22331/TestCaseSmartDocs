const http = require("node:http");
const fs = require("node:fs/promises");
const path = require("node:path");

// Explicit routes: never serve the repository, configuration or arbitrary file paths.
const routes = new Map([
    ["/", ["preview/index.html", "text/html; charset=utf-8"]],
    ["/preview.css", ["preview/preview.css", "text/css; charset=utf-8"]],
    ["/preview.js", ["dist/preview.js", "text/javascript; charset=utf-8"]]
]);

function createPreviewServer() {
    return http.createServer(async (request, response) => {
        response.setHeader("Cache-Control", "no-store");
        response.setHeader("X-Content-Type-Options", "nosniff");
        if (request.method !== "GET" && request.method !== "HEAD") {
            response.writeHead(405, { Allow: "GET, HEAD" });
            response.end();
            return;
        }
        const route = routes.get((request.url || "/").split("?")[0]);
        if (!route) {
            response.writeHead(404);
            response.end("Not found");
            return;
        }
        try {
            const content = await fs.readFile(path.join(__dirname, "..", route[0]));
            response.writeHead(200, { "Content-Type": route[1] });
            response.end(request.method === "HEAD" ? undefined : content);
        } catch (error) {
            console.error("Cannot read preview asset:", error.message);
            response.writeHead(500, { "Content-Type": "text/plain; charset=utf-8" });
            response.end("Preview asset unavailable. Run npm run build in Client.");
        }
    });
}

if (require.main === module) {
    const port = Number(process.env.PORT || 4173);
    const server = createPreviewServer();
    server.on("error", error => {
        console.error(`Preview server: ${error.message}`);
        process.exitCode = 1;
    });
    server.listen(port, "127.0.0.1", () => {
        console.log(`Counterparty preview: http://127.0.0.1:${server.address().port}`);
        console.log("Mock data only; no Docsvision or CRM connection. Stop with Ctrl+C.");
    });
}

module.exports = { createPreviewServer };
