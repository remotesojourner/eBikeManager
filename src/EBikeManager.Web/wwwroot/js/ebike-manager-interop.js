window.ebikeManagerInterop = {
    getLocalStorage: function (key) {
        return localStorage.getItem(key);
    },

    setLocalStorage: function (key, value) {
        localStorage.setItem(key, value);
    },

    copyText: function (text) {
        return navigator.clipboard.writeText(text);
    },

    getTimeZone: function () {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    },

    getLanguage: function () {
        return navigator.language;
    },

    downloadFileFromStream: async function (filename, streamReference) {
        const buffer = await streamReference.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer]));
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};
