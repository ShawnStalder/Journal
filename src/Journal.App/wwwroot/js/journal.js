(function () {
    const themeKey = 'journal-theme';

    // Apply the last-used theme before Blazor renders, so a light-theme user does not see a dark flash.
    try {
        const saved = localStorage.getItem(themeKey);
        if (saved) {
            document.documentElement.setAttribute('data-theme', saved);
        }
    } catch (error) {
        // Storage can be unavailable; the theme is then applied once Blazor starts.
    }

    // Dropping a file anywhere else would make the browser navigate to it.
    window.addEventListener('dragover', event => event.preventDefault());
    window.addEventListener('drop', event => event.preventDefault());

    let lastEditor = null;
    let lastRange = null;

    document.addEventListener('selectionchange', () => {
        const selection = document.getSelection();
        if (!selection || selection.rangeCount === 0) {
            return;
        }

        const range = selection.getRangeAt(0);
        const editor = range.commonAncestorContainer.nodeType === Node.ELEMENT_NODE
            ? range.commonAncestorContainer.closest('.rte-surface')
            : range.commonAncestorContainer.parentElement?.closest('.rte-surface');

        if (editor) {
            lastEditor = editor;
            lastRange = range.cloneRange();
        }
    });

    function restoreSelection(element) {
        element.focus();
        if (lastEditor === element && lastRange) {
            const selection = document.getSelection();
            selection.removeAllRanges();
            selection.addRange(lastRange);
        }
    }

    function extensionFor(mimeType) {
        if (mimeType === 'image/bmp') {
            return 'bmp';
        }
        return mimeType === 'image/jpeg' ? 'jpg' : 'png';
    }

    function timestamp() {
        const now = new Date();
        const pad = value => String(value).padStart(2, '0');
        return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
    }

    function readAsBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(String(reader.result).split(',')[1]);
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(file);
        });
    }

    let pasteHandler = null;

    window.journalInterop = {
        setTheme(isDark) {
            const theme = isDark ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', theme);
            try {
                localStorage.setItem(themeKey, theme);
            } catch (error) {
                // Not essential; the theme is re-applied on every start.
            }
        },

        copyText(text) {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
        },

        rte: {
            init(element, html) {
                document.execCommand('styleWithCSS', false, true);
                element.innerHTML = html;
                element.focus();
            },

            exec(element, command, value) {
                restoreSelection(element);
                const argument = command === 'formatBlock' ? `<${value}>` : value;
                document.execCommand(command, false, argument);
            },

            getHtml(element) {
                const isEmpty = element.innerText.trim() === '' && !element.querySelector('img');
                return isEmpty ? '' : element.innerHTML;
            }
        },

        sql: {
            attach(textarea, highlight) {
                const syncScroll = () => {
                    highlight.scrollTop = textarea.scrollTop;
                    highlight.scrollLeft = textarea.scrollLeft;
                };

                textarea.addEventListener('scroll', syncScroll);
                textarea.addEventListener('keydown', event => {
                    if (event.key === 'Tab') {
                        event.preventDefault();
                        document.execCommand('insertText', false, '    ');
                    }
                });
            }
        },

        images: {
            attach(panel, fileInput, dotNetReference) {
                panel.addEventListener('dragover', event => {
                    event.preventDefault();
                    panel.classList.add('drop-target');
                });
                panel.addEventListener('dragleave', () => panel.classList.remove('drop-target'));
                panel.addEventListener('drop', event => {
                    event.preventDefault();
                    panel.classList.remove('drop-target');
                    if (event.dataTransfer.files.length > 0) {
                        fileInput.files = event.dataTransfer.files;
                        fileInput.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                });

                pasteHandler = async event => {
                    const target = event.target;
                    const isEditing = target.isContentEditable || target.tagName === 'TEXTAREA' || target.tagName === 'INPUT';
                    const file = [...(event.clipboardData?.files ?? [])].find(item => item.type.startsWith('image/'));
                    if (isEditing || !file) {
                        return;
                    }

                    event.preventDefault();
                    const content = await readAsBase64(file);
                    await dotNetReference.invokeMethodAsync('OnImagePasted', `Pasted ${timestamp()}.${extensionFor(file.type)}`, content);
                };
                document.addEventListener('paste', pasteHandler);
            }
        }
    };
})();
