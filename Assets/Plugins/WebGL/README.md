# WebGL Console Warning Filter

## Problem

Unity WebGL builds display the following non-critical console warning:
```
WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat
```

This warning is caused by a known interaction between Unity's WebGL implementation and certain browsers (particularly Chrome and Edge). It occurs when Unity queries texture format support using format enums that some browsers don't recognize or properly report. **This warning does not affect the functionality of the game.**

## Solution

This project implements a console warning filter to suppress this specific warning, reducing console noise during development and production.

### Implementation Details

1. **ConsoleWarningFilter.jslib** (`Assets/Plugins/WebGL/ConsoleWarningFilter.jslib`)
   - A JavaScript plugin that overrides `console.warn` to filter out specific WebGL warnings
   - Only filters warnings containing "INVALID_ENUM" and "getInternalformatParameter"
   - All other console warnings pass through normally

2. **WebGLConsoleFilter.cs** (`Assets/Scripts/WebGLConsoleFilter.cs`)
   - A C# static class that automatically initializes the warning filter
   - Uses `RuntimeInitializeOnLoadMethod` to run before the first scene loads
   - Only active in WebGL builds (not in the Unity Editor)

### How It Works

1. When the WebGL build starts, the `WebGLConsoleFilter` class automatically runs
2. It calls the JavaScript function `InitializeConsoleWarningFilter()` from the `.jslib` plugin
3. The JavaScript code overrides `console.warn` to filter out the specific warning
4. All other console messages (warnings, errors, logs) continue to work normally

### Testing

To verify the fix is working:
1. Build the project for WebGL
2. Open the build in a browser and open the developer console
3. You should see the log message: `[WebGLConsoleFilter] Console warning filter initialized`
4. The "INVALID_ENUM: getInternalformatParameter" warning should no longer appear

### References

- [Unity Issue Tracker - INVALID_ENUM warning](https://issuetracker.unity3d.com/issues/invalid-enum-warning-is-thrown-in-webgl-player-when-building-an-empty-scene)
- [Chromium Bug Tracker](https://issues.chromium.org/issues/454273251)
- [Unity Forum Discussion](https://discussions.unity.com/t/webgl-invalid-enum-getinternalformatparameter-invalid-internalformat/1502790)

### Notes

- This is a cosmetic fix that only suppresses console output
- The underlying browser/Unity interaction that causes the warning is harmless
- Other console warnings and errors will continue to display normally
- The filter only runs in WebGL builds, not in the Unity Editor
