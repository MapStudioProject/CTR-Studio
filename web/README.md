# CTR Studio Web (early preview)

A browser-first UI scaffold for the CTR Studio project.

## Run locally

```bash
cd web
npm install
npm run dev
```

## Current scope

- Browser-based editor workspace and Three.js viewport.
- Drag/drop and local file selection.
- GLB, glTF and OBJ preview.
- Basic file metadata inspection and JSON inspection-report export.
- BCH and BCRES files are accepted as inputs for metadata inspection only.

## Important porting work

The desktop project targets .NET 6 and depends on OpenTK/OpenGL, ImGui.NET, native libraries, and the CtrLibrary project. Those components do not run directly in a normal browser. Real BCH/BCRES support requires porting the format readers, texture codecs, material editing and export pipeline to browser-compatible TypeScript/WebAssembly or a server-side service. The current preview does not claim to decode or edit BCH/BCRES assets.
