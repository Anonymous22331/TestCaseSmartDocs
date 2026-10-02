const { test } = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");
const { JSDOM } = require("jsdom");
const { createPreviewServer } = require("../preview/server.cjs");

const html = fs.readFileSync(path.join(__dirname, "../preview/index.html"), "utf8");
const bundle = fs.readFileSync(path.join(__dirname, "../dist/preview.js"), "utf8");

function preview() {
    const environment = new JSDOM(html, { runScripts: "outside-only" });
    const { window } = environment;
    const pending = [];
    window.setTimeout = (callback, delay) => { pending.push({ callback, delay }); return pending.length; };
    window.HTMLDialogElement.prototype.showModal = function () { this.open = true; };
    window.HTMLDialogElement.prototype.close = function () { this.dispatchEvent(new window.Event("close")); };
    window.eval(bundle);
    return { environment, window, document: window.document, pending };
}

test("standalone bundle handles every preview scenario through the shared dialog", async () => {
    const app = preview();
    try {
        const expected = {
            success: /Тестовые данные загружены/,
            partial: /Тестовые данные загружены/,
            "not-found": /Контрагент с указанным ID не найден/,
            unavailable: /CRM временно недоступна/,
            network: /Не удалось подключиться к CRM/,
            malformed: /Сервер вернул некорректные данные/
        };
        for (const option of app.document.querySelector("#scenario").options) {
            app.document.querySelector("#scenario").value = option.value;
            app.document.querySelector("#delay").value = "3000";
            app.document.querySelector("#open-dialog").click();
            const dialog = app.document.querySelector("dialog");
            const input = dialog.querySelector("input");
            const button = dialog.querySelector("button[type=submit]");
            assert.equal(input.value, "42");
            input.value = " 123 ";
            assert.equal(button.textContent, "Получить тестовые данные");
            const submit = () => dialog.querySelector("form").dispatchEvent(new app.window.Event("submit", { cancelable: true }));
            submit();
            submit();
            assert.equal(app.pending.length, 1, "duplicate requests must be blocked");
            assert.equal(app.pending[0].delay, 3000);
            assert.equal(input.disabled, true);
            assert.equal(button.disabled, true);
            assert.equal(dialog.getAttribute("aria-busy"), "true");
            assert.match(dialog.textContent, /Загрузка…/);
            app.pending.shift().callback();
            await new Promise(setImmediate);
            assert.equal(dialog.getAttribute("aria-busy"), "false");
            assert.equal(input.disabled, false);
            assert.equal(button.disabled, false);
            assert.match(dialog.textContent, expected[option.value]);
            assert.doesNotMatch(dialog.textContent, /Данные сохранены в карточке/);
            if (option.value === "success" || option.value === "partial") {
                const values = [...dialog.querySelectorAll("dd")].map(value => value.textContent);
                assert.equal(values[0], "123");
                assert.equal(values.length, 7);
                assert.equal(values[2], option.value === "success" ? "7700000000" : "—");
                assert.match(dialog.textContent, /Запись в карточку не выполнялась/);
            } else {
                assert.equal(dialog.querySelectorAll("dd").length, 0);
                submit();
                assert.equal(app.pending.length, 1, "retry is available after failure");
                app.pending.shift().callback();
                await new Promise(setImmediate);
                assert.match(dialog.textContent, expected[option.value]);
            }
            dialog.querySelector("button[type=button]").click();
            assert.equal(app.document.querySelector("dialog"), null);
        }
    } finally { app.environment.window.close(); }
});

test("closing during loading does not affect a newly opened dialog", async () => {
    const app = preview();
    try {
        app.document.querySelector("#open-dialog").click();
        const oldDialog = app.document.querySelector("dialog");
        oldDialog.querySelector("form").dispatchEvent(new app.window.Event("submit", { cancelable: true }));
        oldDialog.close();
        app.document.querySelector("#open-dialog").click();
        const newDialog = app.document.querySelector("dialog");
        app.pending.shift().callback();
        await new Promise(setImmediate);
        assert.equal(oldDialog.isConnected, false);
        assert.equal(newDialog.querySelector("[role=status]").textContent, "");
        assert.equal(newDialog.querySelector("button[type=submit]").disabled, false);
    } finally { app.environment.window.close(); }
});

test("local HTTP server serves preview assets and rejects other paths and writes", async () => {
    const server = createPreviewServer();
    await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
    const request = (url, method = "GET") => new Promise((resolve, reject) => {
        const req = http.request({ hostname: "127.0.0.1", port: server.address().port, path: url, method }, response => {
            let body = "";
            response.setEncoding("utf8");
            response.on("data", chunk => { body += chunk; });
            response.on("end", () => resolve({ status: response.statusCode, headers: response.headers, body }));
        });
        req.on("error", reject);
        req.end();
    });
    try {
        const page = await request("/");
        assert.equal(page.status, 200);
        assert.match(page.body, /Открыть окно контрагента/);
        assert.match(page.headers["content-type"], /text\/html/);
        for (const url of ["/preview.css", "/preview.js"]) {
            assert.equal((await request(url)).status, 200);
        }
        assert.equal((await request("/", "HEAD")).body, "");
        for (const url of ["/../package.json", "/package.json", "/src/preview.ts", "/api/Counterparty/GetCounterpartyForCard"]) {
            assert.equal((await request(url)).status, 404);
        }
        assert.equal((await request("/", "POST")).status, 405);
    } finally { await new Promise(resolve => server.close(resolve)); }
});
