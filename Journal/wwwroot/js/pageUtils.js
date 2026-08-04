window.pageUtils = {
    scrollIntoView: function (element) {
        element?.scrollIntoView({ behavior: "smooth", block: "start" });
    }
};
