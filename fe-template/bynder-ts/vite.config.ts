import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const outputBase = "sitecore modules/shell/ContentWorkflow/static";

export default defineConfig({
  plugins: [react()],
  base: "./",
  build: {
    outDir: "build",
    emptyOutDir: true,
    cssCodeSplit: true,
    sourcemap: true,
    rollupOptions: {
      output: {
        entryFileNames: `${outputBase}/js/main.js`,
        chunkFileNames: `${outputBase}/js/[name].chunk.js`,
        assetFileNames: (assetInfo) => {
          const name = assetInfo.names?.[0] ?? "";
          if (name.endsWith(".css")) {
            const base = name === "index.css" ? "main.css" : name;
            return `${outputBase}/css/${base}`;
          }
          return `${outputBase}/assets/[name][extname]`;
        },
        manualChunks(id) {
          if (id.includes("/components/Update/")) return "Update";
          if (id.includes("/components/Import/MultiLocationImport")) return "MultiLocationImport";
          if (id.includes("/components/Import/")) return "Import";
          if (id.includes("/components/Mappings/AddOrUpdateMapping")) return "AddOrUpdateMapping";
          if (id.includes("/components/Mappings/")) return "Mappings";
          if (id.includes("node_modules")) return "vendor";
        },
      },
    },
  },
  server: {
    port: 3000,
  },
});
