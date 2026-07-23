window.textEditor = {
    getSelection: function (el) {
        return { start: el.selectionStart, end: el.selectionEnd };
    },
    setSelection: function (el, start, end) {
        el.focus();
        el.setSelectionRange(start, end);
    }
};
