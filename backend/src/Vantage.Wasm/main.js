import { dotnet } from './_framework/dotnet.js'
const { getAssemblyExports, getConfig } = await dotnet.create()
const exports = await getAssemblyExports(getConfig().mainAssemblyName)
globalThis.vantage = exports.Vantage.Wasm.DemoApi
