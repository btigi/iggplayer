let dotNetRef = null;
let audioEl = null;

export function init(dotNetObjRef) {
    dotNetRef = dotNetObjRef;
}

export function bindAudio(elementId) {
    audioEl = document.getElementById(elementId);
    if (!audioEl) return;

    audioEl.onended = () => {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnTrackEnded');
        }
    };
}

export function play(src) {
    if (!audioEl) return;
    audioEl.src = src;
    audioEl.load();
    audioEl.play().catch(() => {});
}

export function dispose() {
    if (audioEl) {
        audioEl.onended = null;
        audioEl.pause();
        audioEl.src = '';
    }
    dotNetRef = null;
    audioEl = null;
}
