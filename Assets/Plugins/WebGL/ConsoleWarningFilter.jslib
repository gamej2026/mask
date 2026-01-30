// ConsoleWarningFilter.jslib
// This plugin filters out known non-critical WebGL console warnings
// to reduce console noise during development and production.

mergeInto(LibraryManager.library, {
    InitializeConsoleWarningFilter: function () {
        // Store the original console.warn function
        var originalWarn = console.warn;
        
        // Override console.warn to filter specific warnings
        console.warn = function() {
            var msg = arguments[0];
            
            // Filter out the known non-critical WebGL warnings
            if (typeof msg === 'string') {
                // Filter INVALID_ENUM: getInternalformatParameter warnings
                if (msg.indexOf('INVALID_ENUM') !== -1 && 
                    msg.indexOf('getInternalformatParameter') !== -1) {
                    return; // Suppress this warning
                }
                
                // Filter "invalid internalformat" warnings
                if (msg.indexOf('invalid internalformat') !== -1) {
                    return; // Suppress this warning
                }
            }
            
            // Pass through all other warnings
            originalWarn.apply(console, arguments);
        };
        
        console.log('[ConsoleWarningFilter] Console warning filter initialized - filtering WebGL format warnings');
    }
});
