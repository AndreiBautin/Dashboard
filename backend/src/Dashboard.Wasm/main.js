// The entry point the wasm SDK requires (WasmMainJSPath), and the one the
// AppBundle would use if the runtime were loaded standalone.
//
// The React app does not use this file: `frontend/src/lib/adapters/demoApi.ts`
// imports `_framework/dotnet.js` directly so it can control when the runtime
// boots, and only `_framework/` is staged into the site. Kept correct anyway,
// because a file that quietly disagrees with the code beside it is worse than
// no file at all.
import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig } = await dotnet.create()
const exports = await getAssemblyExports(getConfig().mainAssemblyName)

globalThis.dashboard = exports.Dashboard.Wasm.DemoApi
