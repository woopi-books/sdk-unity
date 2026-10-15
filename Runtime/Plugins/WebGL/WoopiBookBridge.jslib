mergeInto(LibraryManager.library, {
  // Unity -> host. Delivers a JSON event string to the hosting page as a DOM CustomEvent and,
  // if present, a global callback (window.WoopiBook.onEvent).
  WoopiBook_Emit: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    var detail;
    try {
      detail = JSON.parse(json);
    } catch (e) {
      detail = { raw: json, parseError: true };
    }
    try {
      if (typeof window !== 'undefined') {
        if (window.WoopiBook && typeof window.WoopiBook.onEvent === 'function') {
          window.WoopiBook.onEvent(detail);
        }
        if (typeof window.dispatchEvent === 'function' && typeof CustomEvent === 'function') {
          window.dispatchEvent(new CustomEvent('woopi-book-event', { detail: detail }));
        }
      }
    } catch (e) {
      if (typeof console !== 'undefined') {
        console.error('WoopiBook_Emit failed', e);
      }
    }
  },

  // Host -> Unity at startup. Returns '{"lang":"es","page":5,"content":"https://..."}' (either key may be missing) from
  // window.WoopiInit, which the Woopi template fills from an `init` message or the page URL.
  // `consume` (1 when the book starts with these args, 0 to peek) controls whether a later `init`
  // message is treated as too late.
  WoopiBook_GetStartArgs: function (consume) {
    var json = '';
    try {
      if (typeof window !== 'undefined') {
        if (consume) window.WoopiInitConsumed = true;
        if (window.WoopiInit) json = JSON.stringify(window.WoopiInit);
      }
    } catch (e) {
      json = '';
    }
    var size = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(size);
    stringToUTF8(json, buffer, size);
    return buffer;
  }
});
