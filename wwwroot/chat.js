window.chatScrollToBottom = function (element) {
    if (!element) return;
    element.scrollTop = element.scrollHeight;
};

window.chatComposerInit = function (textarea, dotnetRef) {
    if (!textarea || textarea.dataset.chatComposerBound === "1") return;
    textarea.dataset.chatComposerBound = "1";
    textarea.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            dotnetRef.invokeMethodAsync("ComposerSend", textarea.value);
        }
    });
};
