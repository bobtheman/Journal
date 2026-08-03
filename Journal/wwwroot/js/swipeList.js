window.swipeList = {
    _openRow: null,

    initAll: function (rowSelector) {
        document.querySelectorAll(rowSelector).forEach(function (row) {
            if (row.dataset.swipeInit) {
                return;
            }
            row.dataset.swipeInit = "true";
            window.swipeList._initRow(row);
        });
    },

    resetAll: function (rowSelector) {
        document.querySelectorAll(rowSelector).forEach(function (row) {
            window.swipeList._snap(row, 0);
        });
        window.swipeList._openRow = null;
    },

    _initRow: function (row) {
        const content = row.querySelector(".journal-swipe-content");
        const deleteEl = row.querySelector(".journal-swipe-delete");
        if (!content || !deleteEl) {
            return;
        }

        const revealWidth = deleteEl.offsetWidth || 72;
        let startX = 0;
        let currentX = 0;
        let dragging = false;

        content.addEventListener("pointerdown", function (e) {
            dragging = true;
            startX = e.clientX;
            content.style.transition = "none";
        });

        content.addEventListener("pointermove", function (e) {
            if (!dragging) {
                return;
            }
            const delta = e.clientX - startX;
            const open = row.classList.contains("journal-swipe-open") ? -revealWidth : 0;
            currentX = Math.min(0, Math.max(-revealWidth, open + delta));
            content.style.transform = `translateX(${currentX}px)`;
        });

        const endDrag = function () {
            if (!dragging) {
                return;
            }
            dragging = false;
            content.style.transition = "";
            if (currentX < -revealWidth / 2) {
                window.swipeList._snap(row, -revealWidth);
                if (window.swipeList._openRow && window.swipeList._openRow !== row) {
                    window.swipeList._snap(window.swipeList._openRow, 0);
                }
                window.swipeList._openRow = row;
            } else {
                window.swipeList._snap(row, 0);
                if (window.swipeList._openRow === row) {
                    window.swipeList._openRow = null;
                }
            }
        };

        content.addEventListener("pointerup", endDrag);
        content.addEventListener("pointercancel", endDrag);
    },

    _snap: function (row, x) {
        const content = row.querySelector(".journal-swipe-content");
        if (!content) {
            return;
        }
        content.style.transform = `translateX(${x}px)`;
        row.classList.toggle("journal-swipe-open", x !== 0);
    }
};
