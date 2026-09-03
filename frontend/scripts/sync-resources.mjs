import { cp, mkdir, rm, stat } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repositoryRoot = resolve(frontendRoot, "..");
const destination = resolve(frontendRoot, "public", "generated-assets");

await rm(destination, { recursive: true, force: true });
await mkdir(resolve(destination, "fonts"), { recursive: true });
await cp(
  resolve(
    repositoryRoot,
    "resources",
    "fonts",
    "vazirmatn",
    "Vazirmatn[wght].woff2",
  ),
  resolve(destination, "fonts", "Vazirmatn[wght].woff2"),
);
await cp(
  resolve(repositoryRoot, "resources", "fonts", "vazirmatn", "OFL.txt"),
  resolve(destination, "fonts", "OFL.txt"),
);

const logoSource = resolve(repositoryRoot, "resources", "branding", "eos.svg");
try {
  await stat(logoSource);
  await mkdir(resolve(destination, "brand"), { recursive: true });
  await cp(logoSource, resolve(destination, "brand", "eos.svg"));
  await cp(
    resolve(repositoryRoot, "resources", "branding", "auth-background.jpg"),
    resolve(destination, "brand", "auth-background.jpg"),
  );
} catch (error) {
  if (error?.code !== "ENOENT") throw error;
}
