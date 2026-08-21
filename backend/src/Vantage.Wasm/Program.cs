namespace Vantage.Wasm;

/// <summary>
/// The WebAssembly module's entry point. It intentionally does nothing: the
/// browser loads the runtime, and everything after that is driven by the
/// React app calling the <see cref="DemoApi"/> exports. There is no managed
/// main loop and no managed UI.
/// </summary>
public static class Program
{
    public static void Main()
    {
    }
}
