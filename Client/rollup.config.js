const typescript = require("rollup-plugin-typescript2");

module.exports = {
    input: "src/Index.ts",
    output: { file: "dist/extension.js", format: "amd", sourcemap: true },
    external: id => id.startsWith("@docsvision/"),
    plugins: [typescript({ tsconfigOverride: { compilerOptions: { noEmit: false } } })]
};
