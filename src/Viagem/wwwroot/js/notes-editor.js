window.notesEditor = {
    _instances: {},

    init: function (elementId, initialContent) {
        if (this._instances[elementId]) {
            delete this._instances[elementId];
        }

        const el = document.getElementById(elementId);
        if (!el) return;

        const quill = new Quill('#' + elementId, {
            theme: 'snow',
            placeholder: 'Packing lists, reminders, important contacts, visa info...',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline', 'strike'],
                    ['blockquote'],
                    [{ 'header': 1 }, { 'header': 2 }],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    ['link'],
                    ['clean']
                ]
            }
        });

        if (initialContent) {
            quill.clipboard.dangerouslyPasteHTML(initialContent);
        }

        this._instances[elementId] = quill;
    },

    getHTML: function (elementId) {
        const quill = this._instances[elementId];
        if (!quill) return '';
        const html = quill.root.innerHTML;
        // Return empty string for empty editor (Quill default empty state)
        if (html === '<p><br></p>') return '';
        return html;
    },

    destroy: function (elementId) {
        if (this._instances[elementId]) {
            delete this._instances[elementId];
        }
    }
};
