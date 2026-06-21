/**
 * NativeBridge.jslib
 *
 * WebGL JavaScript plugin providing the JS→Unity and Unity→JS bridge functions
 * used by RuntimeBootstrap.cs.
 *
 * Compiled into the Unity WebGL build by the Unity Editor.
 * Functions listed in mergeInto(LibraryManager.library, {...}) are exported
 * and callable from C# via [DllImport("__Internal")].
 */
mergeInto(LibraryManager.library, {

    /**
     * Called by RuntimeBootstrap.cs when the game reaches a rendered state.
     * Dispatches a CustomEvent on the Unity iframe's window so the host page
     * (unity/index.html) can relay it to the parent React frame.
     *
     * C# signature:
     *   [DllImport("__Internal")]
     *   private static extern void NotifyJsBridge(string mode);
     *
     * @param {number} modePtr  WASM linear-memory pointer to a UTF-8 string.
     *                          Use UTF8ToString() to decode.
     */
    NotifyJsBridge: function(modePtr) {
        var mode = UTF8ToString(modePtr);

        // Fire on the current window (inside the iframe).
        try {
            window.dispatchEvent(new CustomEvent("UNITY_GAME_READY", {
                detail: { mode: mode }
            }));
        } catch(e) {}

        // Relay to the parent frame (the React host).
        try {
            window.parent.postMessage({ type: "UNITY_GAME_READY", mode: mode }, "*");
        } catch(e) {}

        console.log("[EK-NativeBridge] UNITY_GAME_READY dispatched — mode:", mode);
    }

});
