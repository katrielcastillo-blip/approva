// One-off pipeline that turns the raw nano-banana JPEG exports in public/brand/source/
// into the actual assets the app uses. Re-run with `node scripts/process-brand-assets.js`
// if the source images ever get regenerated.
//
// Why any processing is needed at all: nano-banana exported these as JPEG, which has no
// alpha channel — the "transparent" checkerboard you see in the source files is baked-in
// pixels, not real transparency. For the logomark and empty-state icon we key it back out
// (checkerboard is neutral gray/white, the artwork is saturated indigo) and crop tight to
// content. The login illustration and OG card keep their light background by design (they
// render as a "floating card"), so those just get a content-aware crop / resize instead.
const sharp = require("sharp");
const path = require("path");

const APP_DIR = path.join(__dirname, "..", "src", "app");
const BRAND_DIR = path.join(__dirname, "..", "public", "brand");
const SOURCE_DIR = path.join(BRAND_DIR, "source");

/** Turns the neutral-gray checkerboard into real alpha, then crops to the non-transparent
 * bounding box. Works because the artwork is saturated (blue/indigo) and the checkerboard
 * is perfectly achromatic (R≈G≈B). */
async function keyOutCheckerboard(inputFile, outputFile, { satLo = 12, satHi = 40, pad = 30 } = {}) {
  const { data, info } = await sharp(inputFile).raw().toBuffer({ resolveWithObject: true });
  const { width, height, channels } = info;
  const out = Buffer.alloc(width * height * 4);

  let minX = width, minY = height, maxX = 0, maxY = 0;

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = (y * width + x) * channels;
      const p = (y * width + x) * 4;
      const r = data[i], g = data[i + 1], b = data[i + 2];
      const sat = Math.max(r, g, b) - Math.min(r, g, b);
      let alpha;
      if (sat <= satLo) alpha = 0;
      else if (sat >= satHi) alpha = 255;
      else alpha = Math.round(((sat - satLo) / (satHi - satLo)) * 255);

      out[p] = r; out[p + 1] = g; out[p + 2] = b; out[p + 3] = alpha;

      if (alpha > 10) {
        if (x < minX) minX = x; if (x > maxX) maxX = x;
        if (y < minY) minY = y; if (y > maxY) maxY = y;
      }
    }
  }

  minX = Math.max(0, minX - pad); minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad); maxY = Math.min(height - 1, maxY + pad);

  await sharp(out, { raw: { width, height, channels: 4 } })
    .extract({ left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 })
    .png()
    .toFile(outputFile);
}

/** Crops to the bounding box of "not the light background" content — used for the
 * illustration and OG source, which keep an intentional light background (no alpha). */
async function cropToContent(inputFile, outputFile, { satThreshold = 18, darkThreshold = 60, pad = 90 } = {}) {
  const { data, info } = await sharp(inputFile).raw().toBuffer({ resolveWithObject: true });
  const { width, height, channels } = info;
  let minX = width, minY = height, maxX = 0, maxY = 0;

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = (y * width + x) * channels;
      const r = data[i], g = data[i + 1], b = data[i + 2];
      const sat = Math.max(r, g, b) - Math.min(r, g, b);
      const dark = 255 - Math.min(r, g, b);
      if (sat > satThreshold || dark > darkThreshold) {
        if (x < minX) minX = x; if (x > maxX) maxX = x;
        if (y < minY) minY = y; if (y > maxY) maxY = y;
      }
    }
  }

  minX = Math.max(0, minX - pad); minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad); maxY = Math.min(height - 1, maxY + pad);

  await sharp(inputFile)
    .extract({ left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 })
    .jpeg({ quality: 92 })
    .toFile(outputFile);
}

async function main() {
  // 1. Logomark — keyed transparent, then exported at the two sizes the app actually uses.
  const logomarkFull = path.join(SOURCE_DIR, "_logomark-keyed.png");
  await keyOutCheckerboard(path.join(SOURCE_DIR, "Approva_SaaS_logo_design_2K_202608200123.jpeg"), logomarkFull);
  await sharp(logomarkFull).resize(256, 256, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 }).toFile(path.join(BRAND_DIR, "logomark.png"));
  await sharp(logomarkFull).resize(32, 32, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 }).toFile(path.join(APP_DIR, "icon.png"));
  await sharp(logomarkFull).resize(180, 180, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 }).toFile(path.join(APP_DIR, "apple-icon.png"));
  console.log("logomark.png + icon.png + apple-icon.png: done");

  // 2. Empty-state icon — same keying, exported at display size (renders at ~140px).
  const emptyStateFull = path.join(SOURCE_DIR, "_empty-state-keyed.png");
  await keyOutCheckerboard(path.join(SOURCE_DIR, "Documents_with_checkmark_vector_…_202608200123.jpeg"), emptyStateFull);
  await sharp(emptyStateFull).resize(400, 400, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 }).toFile(path.join(BRAND_DIR, "empty-state.png"));
  console.log("empty-state.png: done");

  // 3. Login illustration — content-aware crop (keeps its light background, no alpha).
  await cropToContent(
    path.join(SOURCE_DIR, "Document_moving_through_approval…_2K_202608200123.jpeg"),
    path.join(BRAND_DIR, "login-illustration.jpg")
  );
  console.log("login-illustration.jpg: done");

  // 4. OG card — resize to the standard 1200x630 social preview size, then bake in the
  // wordmark + tagline in the empty left third the source image was composed to leave.
  const ogResized = path.join(SOURCE_DIR, "_og-resized.jpg");
  await sharp(path.join(SOURCE_DIR, "Workflow_approval_preview_card_v…_202608200123.jpeg"))
    .resize(1200, 630, { fit: "cover" })
    .jpeg({ quality: 92 })
    .toFile(ogResized);

  const svgText = Buffer.from(`
    <svg width="1200" height="630" xmlns="http://www.w3.org/2000/svg">
      <text x="90" y="300" font-family="Arial, Helvetica, sans-serif" font-size="72" font-weight="700" fill="#3730a3">Approva</text>
      <text x="90" y="355" font-family="Arial, Helvetica, sans-serif" font-size="28" fill="#52525b">Motor de aprobaciones empresariales</text>
      <text x="90" y="392" font-family="Arial, Helvetica, sans-serif" font-size="28" fill="#52525b">configurable, multi-tenant.</text>
    </svg>
  `);
  await sharp(ogResized).composite([{ input: svgText, top: 0, left: 0 }]).jpeg({ quality: 92 })
    .toFile(path.join(BRAND_DIR, "og-image.jpg"));
  console.log("og-image.jpg: done");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
