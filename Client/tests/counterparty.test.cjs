const { test } = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const ts = require("typescript");
const { JSDOM } = require("jsdom");

require.extensions[".ts"] = (module, filename) => module._compile(ts.transpileModule(fs.readFileSync(filename, "utf8"), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 }
}).outputText, filename);
const { CounterpartyService, CounterpartyWidget } = require("../src/counterparty.ts");
const { openCounterpartyDialog } = require("../src/dialog.ts");
const party = { id: "42", name: "Company", legalAddress: "Address" };

test("uses POST, preserves virtual-directory endpoint and sends a JSON body", async () => {
    const service = new CounterpartyService({ post: async (url, body) => {
        assert.equal(url, "/WebClient/api/Counterparty/GetCounterpartyForCard");
        assert.deepEqual(JSON.parse(body), { cardId: "card", counterpartyId: "42" });
        return { success: true, counterparty: party };
    } }, "/WebClient/api/Counterparty/GetCounterpartyForCard");
    assert.equal((await service.getCounterparty("card", " 42 ")).counterparty.legalAddress, "Address");
});

test("blank IDs are rejected without sending a request", async () => {
    const service = new CounterpartyService({ post: () => assert.fail("Unexpected request") }, "/endpoint");
    assert.equal((await service.getCounterparty("card", " ")).errorCode, "INVALID_REQUEST");
});

test("rejects malformed server responses", async () => {
    for (const response of [null, {}, { success: true }, { success: true, counterparty: {} }]) {
        const service = new CounterpartyService({ post: async () => response }, "/endpoint");
        await assert.rejects(service.getCounterparty("card", "42"), /некорректные/);
    }
});

test("widget blocks duplicate requests and transitions from loading to data", async () => {
    let finish;
    let calls = 0;
    const states = [];
    const service = new CounterpartyService({ post: () => { calls++; return new Promise(resolve => { finish = resolve; }); } }, "/endpoint");
    const widget = new CounterpartyWidget(service, "card", "42", state => states.push(state));
    const pending = widget.load();
    await widget.load();
    assert.equal(calls, 1);
    assert.equal(states[0].loading, true);
    finish({ success: true, counterparty: party });
    await pending;
    assert.equal(states.at(-1).loading, false);
    assert.equal(states.at(-1).counterparty.name, "Company");
});

test("widget shows business and network errors", async () => {
    for (const post of [async () => ({ success: false, errorMessage: "Card missing" }), async () => { throw new Error("Offline"); }]) {
        let state;
        await new CounterpartyWidget(new CounterpartyService({ post }, "/endpoint"), "card", "42", next => { state = next; }).load();
        assert.equal(state.loading, false);
        assert.ok(["Card missing", "Offline"].includes(state.error));
        assert.equal(state.counterparty, null);
    }
});

function dom() {
    const environment = new JSDOM("<!doctype html><html><body></body></html>");
    global.document = environment.window.document;
    environment.window.HTMLDialogElement.prototype.showModal = function () { this.open = true; };
    environment.window.HTMLDialogElement.prototype.close = function () { this.dispatchEvent(new environment.window.Event("close")); };
    return environment;
}

test("dialog renders CRM values as text and removes itself on close", async () => {
    const environment = dom();
    try {
        const service = new CounterpartyService({ post: async () => ({ success: true, counterparty: { ...party, name: "<img src=x onerror=alert(1)>" } }) }, "/endpoint");
        const dialog = openCounterpartyDialog(service, "card");
        dialog.querySelector("input").value = "42";
        dialog.querySelector("form").dispatchEvent(new environment.window.Event("submit", { cancelable: true }));
        assert.equal(dialog.querySelector("button[type=submit]").disabled, true);
        await new Promise(setImmediate);
        assert.match(dialog.textContent, /Данные сохранены/);
        assert.match(dialog.textContent, /Address/);
        assert.equal(dialog.querySelector("img"), null);
        assert.match(dialog.textContent, /<img/);
        dialog.close();
        assert.equal(document.querySelector("dialog"), null);
    } finally { environment.window.close(); delete global.document; }
});

test("dialog shows failure and enables retry", async () => {
    const environment = dom();
    try {
        const dialog = openCounterpartyDialog(new CounterpartyService({ post: async () => ({ success: false, errorMessage: "CRM unavailable" }) }, "/endpoint"), "card");
        dialog.querySelector("input").value = "42";
        dialog.querySelector("form").dispatchEvent(new environment.window.Event("submit", { cancelable: true }));
        await new Promise(setImmediate);
        assert.match(dialog.textContent, /CRM unavailable/);
        assert.equal(dialog.querySelector("button[type=submit]").disabled, false);
        assert.equal(dialog.querySelectorAll("dd").length, 0);
    } finally { environment.window.close(); delete global.document; }
});

test("built AMD extension registers a usable handler with the real SDK module names", () => {
    const environment = dom();
    try {
        let extension;
        const services = {
            "@docsvision/webclient/System/ExtensionManager": { extensionManager: { registerExtension: value => { extension = value; } } },
            "@docsvision/webclient/System/$RequestManager": { $RequestManager: "requestManager" },
            "@docsvision/webclient/System/UrlStore": { urlStore: { urlResolver: { resolveUrl: (action, controller) => `/WebClient/api/${controller}/${action}` } } }
        };
        vm.runInNewContext(fs.readFileSync(path.join(__dirname, "../dist/extension.js"), "utf8"), {
            document,
            define: (dependencies, factory) => factory(...dependencies.map(name => { assert.ok(services[name], name); return services[name]; }))
        });
        assert.equal(extension.name, "SmartDocs.CounterpartyIntegration");
        extension.globalEventHandlers[0].showCounterparty({ layout: { cardInfo: { id: "card" } }, getService: name => {
            assert.equal(name, "requestManager");
            return { post: async () => ({ success: true, counterparty: party }) };
        } });
        assert.ok(document.querySelector("dialog"));
    } finally { environment.window.close(); delete global.document; }
});
