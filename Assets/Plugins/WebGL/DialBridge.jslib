mergeInto(LibraryManager.library, {
  DialPublish: function(json) {
    if (window.ascendantDial) window.ascendantDial.receive(JSON.parse(UTF8ToString(json)));
  }
});
