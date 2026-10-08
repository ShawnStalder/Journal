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

    function isInListItem() {
        const selection = document.getSelection();
        const node = selection && selection.anchorNode;
        const element = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        return Boolean(element && element.closest('li'));
    }

    // Outside a list, Tab keeps its normal meaning (moving focus) so keyboard users are not trapped.
    function handleListIndent(event) {
        if (event.key !== 'Tab' || !isInListItem()) {
            return;
        }

        event.preventDefault();
        document.execCommand(event.shiftKey ? 'outdent' : 'indent');
    }

    // Same ids as Journal.Core CodeLanguages; unknown ids fall back to the id itself.
    const languageLabels = {
        csharp: 'C#', sql: 'SQL', json: 'JSON', xml: 'XML / HTML', javascript: 'JavaScript', typescript: 'TypeScript',
        powershell: 'PowerShell', yaml: 'YAML', bash: 'Bash', python: 'Python', css: 'CSS', plaintext: 'Plain text',
        auto: 'Auto-detect'
    };

    function languageOf(code) {
        const match = [...code.classList].find(name => name.startsWith('language-'));
        return match ? match.substring('language-'.length) : 'plaintext';
    }

    function labelFor(language) {
        return languageLabels[language] ?? language;
    }

    function escapeHtml(text) {
        return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function currentCodeElement() {
        const selection = document.getSelection();
        const node = selection && selection.anchorNode;
        const element = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        return element ? element.closest('pre code') : null;
    }

    function isCaretAtEnd(code) {
        const selection = document.getSelection();
        if (!selection.isCollapsed) {
            return false;
        }

        const tail = document.createRange();
        tail.setStart(selection.anchorNode, selection.anchorOffset);
        tail.setEnd(code, code.childNodes.length);
        return tail.toString() === '';
    }

    // A newline at the very end of a <pre> renders as nothing, so a sentinel newline is added and skipped over.
    // The sentinel is trimmed again in getHtml.
    function insertCodeNewline(code) {
        if (isCaretAtEnd(code)) {
            document.execCommand('insertText', false, '\n\n');
            document.getSelection().modify('move', 'backward', 'character');
        } else {
            document.execCommand('insertText', false, '\n');
        }
    }

    function exitCodeBlock(code) {
        const pre = code.closest('pre');
        let next = pre.nextElementSibling;
        if (!next) {
            next = document.createElement('p');
            next.appendChild(document.createElement('br'));
            pre.after(next);
        }

        const range = document.createRange();
        range.setStart(next, 0);
        range.collapse(true);
        const selection = document.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    }

    // insertHTML leaves the caret in the paragraph after the block; move it into the block so typing starts there.
    function placeCaretInInsertedBlock() {
        const selection = document.getSelection();
        const node = selection && selection.anchorNode;
        const element = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        const previous = element ? element.closest('p')?.previousElementSibling : null;
        const code = previous && previous.tagName === 'PRE' ? previous.querySelector('code') : null;
        if (!code) {
            return;
        }

        const range = document.createRange();
        range.selectNodeContents(code);
        range.collapse(code.textContent.length > 0 ? false : true);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    function handleCodeKey(event, code) {
        if (event.key === 'Tab' && !event.shiftKey) {
            event.preventDefault();
            document.execCommand('insertText', false, '    ');
        } else if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            exitCodeBlock(code);
        } else if (event.key === 'Enter') {
            event.preventDefault();
            insertCodeNewline(code);
        }
    }

    // Typing "- ", "* " or "1. " at the start of a line turns it into a list, like Markdown, so the toolbar is optional.
    function handleListShortcut(event) {
        if (event.key !== ' ' || event.ctrlKey || event.metaKey || event.altKey || isInListItem()) {
            return false;
        }

        const selection = document.getSelection();
        const node = selection.anchorNode;
        if (!selection.isCollapsed || !node || node.nodeType !== Node.TEXT_NODE) {
            return false;
        }

        // The marker must be the first thing on its line: nothing before it, or only a line break.
        const startsLine = !node.previousSibling || node.previousSibling.nodeName === 'BR';
        const marker = node.data.slice(0, selection.anchorOffset);
        const match = /^(?:[-*]|(\d+)\.)$/.exec(marker);
        if (!startsLine || !match) {
            return false;
        }

        event.preventDefault();
        node.deleteData(0, marker.length);
        if (node.data === '' && !node.nextSibling) {
            node.parentNode.appendChild(document.createElement('br'));
        }

        const range = document.createRange();
        range.setStart(node, 0);
        range.collapse(true);
        selection.removeAllRanges();
        selection.addRange(range);

        const isNumbered = match[1] !== undefined;
        document.execCommand(isNumbered ? 'insertOrderedList' : 'insertUnorderedList');
        applyListStart(isNumbered, Number(match[1]));
        return true;
    }

    function applyListStart(isNumbered, number) {
        if (!isNumbered || number === 1) {
            return;
        }

        const node = document.getSelection().anchorNode;
        const element = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        const list = element ? element.closest('ol') : null;
        if (list) {
            list.start = number;
        }
    }

    function handleEditorKeys(event) {
        const code = currentCodeElement();
        if (code) {
            handleCodeKey(event, code);
        } else if (!handleListShortcut(event)) {
            handleListIndent(event);
        }
    }

    // Code is always pasted as plain text so formatting from the source never leaks into the block.
    function handleEditorPaste(event) {
        if (!currentCodeElement()) {
            return;
        }

        event.preventDefault();
        const text = (event.clipboardData.getData('text/plain') || '').replace(/\r\n?/g, '\n');
        document.execCommand('insertText', false, text);
    }

    // Code is not prose, so it should not get red squiggles. The attribute is editor-only and removed again on save.
    function disableSpellcheckInCode(root) {
        root.querySelectorAll('pre, code').forEach(element => {
            element.spellcheck = false;
        });
    }

    function labelCodeBlocks(root) {
        root.querySelectorAll('pre > code').forEach(code => {
            code.parentElement.dataset.lang = labelFor(languageOf(code));
        });
    }

    // The editor stores code as plain text only; browsers may add <br>, spans or the data-lang badge while editing.
    function cleanCodeBlocks(root) {
        root.querySelectorAll('pre').forEach(pre => {
            pre.removeAttribute('data-lang');
            const language = pre.querySelector('code') ? languageOf(pre.querySelector('code')) : 'plaintext';
            pre.querySelectorAll('br').forEach(lineBreak => lineBreak.replaceWith('\n'));
            const code = document.createElement('code');
            code.className = `language-${language}`;
            code.textContent = pre.textContent.replace(/\n+$/, '');
            pre.replaceChildren(code);
        });
    }

    function highlightCode(code, pre) {
        if (!window.hljs) {
            return;
        }

        const language = languageOf(code);
        const text = code.textContent;
        let result = null;
        if (language === 'auto') {
            result = window.hljs.highlightAuto(text);
        } else if (window.hljs.getLanguage(language)) {
            result = window.hljs.highlight(text, { language, ignoreIllegals: true });
        }

        if (result) {
            code.innerHTML = result.value;
            code.classList.add('hljs');
            if (language === 'auto' && result.language) {
                pre.dataset.lang = `${labelFor(result.language)} (auto)`;
            }
        }
    }

    function wrapWithCopyButton(pre, code) {
        const wrapper = document.createElement('div');
        wrapper.className = 'code-block';
        pre.replaceWith(wrapper);
        wrapper.appendChild(pre);

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'code-copy';
        button.textContent = 'Copy';
        button.addEventListener('click', () => {
            window.journalInterop.copyText(code.textContent);
            button.textContent = 'Copied';
            setTimeout(() => { button.textContent = 'Copy'; }, 1500);
        });
        wrapper.appendChild(button);
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
            init(element, html, autoFocus) {
                document.execCommand('styleWithCSS', false, true);
                element.innerHTML = html;
                labelCodeBlocks(element);
                disableSpellcheckInCode(element);
                element.addEventListener('keydown', handleEditorKeys);
                element.addEventListener('paste', handleEditorPaste);
                if (autoFocus) {
                    element.focus();
                }
            },

            insertCodeBlock(element, language) {
                restoreSelection(element);

                const existing = currentCodeElement();
                if (existing) {
                    existing.className = `language-${language}`;
                    existing.parentElement.dataset.lang = labelFor(language);
                    return;
                }

                const selected = document.getSelection().toString().replace(/\r/g, '');
                const body = selected === '' ? '<br>' : escapeHtml(selected);
                // The empty paragraph after the block gives the caret somewhere to go when leaving it.
                document.execCommand(
                    'insertHTML',
                    false,
                    `<pre data-lang="${labelFor(language)}"><code class="language-${language}">${body}</code></pre><p><br></p>`);
                disableSpellcheckInCode(element);
                placeCaretInInsertedBlock();
            },

            insertInlineCode(element) {
                restoreSelection(element);
                const selected = document.getSelection().toString();
                if (selected === '' || currentCodeElement()) {
                    return;
                }

                document.execCommand('insertHTML', false, `<code>${escapeHtml(selected)}</code> `);
                disableSpellcheckInCode(element);
            },

            resetSelect(select) {
                select.value = '';
            },

            exec(element, command, value) {
                restoreSelection(element);
                const argument = command === 'formatBlock' ? `<${value}>` : value;
                document.execCommand(command, false, argument);
            },

            getHtml(element) {
                const isEmpty = element.innerText.trim() === '' && !element.querySelector('img');
                if (isEmpty) {
                    return '';
                }

                const clone = element.cloneNode(true);
                cleanCodeBlocks(clone);
                clone.querySelectorAll('[spellcheck]').forEach(node => node.removeAttribute('spellcheck'));
                return clone.innerHTML;
            }
        },

        code: {
            // Called after a note renders; blocks that are already processed are skipped.
            highlight(container) {
                container.querySelectorAll('pre > code').forEach(code => {
                    const pre = code.parentElement;
                    if (pre.dataset.processed) {
                        return;
                    }

                    pre.dataset.processed = 'yes';
                    pre.dataset.lang = labelFor(languageOf(code));
                    highlightCode(code, pre);
                    wrapWithCopyButton(pre, code);
                });
            }
        },

        modal: {
            // Tab from the title goes straight to the editor instead of through every toolbar control.
            skipToEditor(titleInput, modal) {
                titleInput.addEventListener('keydown', event => {
                    if (event.key !== 'Tab' || event.shiftKey) {
                        return;
                    }

                    const editor = modal.querySelector('.rte-surface, .sql-input');
                    if (editor) {
                        event.preventDefault();
                        editor.focus();
                    }
                });
            },

            makeDraggable(modal, handle) {
                let offsetX = 0;
                let offsetY = 0;
                let startX = 0;
                let startY = 0;

                // Keeps at least part of the dialog on screen so it can never be dragged out of reach.
                const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

                handle.addEventListener('pointerdown', event => {
                    if (event.button !== 0) {
                        return;
                    }

                    startX = event.clientX - offsetX;
                    startY = event.clientY - offsetY;
                    handle.setPointerCapture(event.pointerId);
                    handle.classList.add('dragging');
                });

                handle.addEventListener('pointermove', event => {
                    if (!handle.hasPointerCapture(event.pointerId)) {
                        return;
                    }

                    const rect = modal.getBoundingClientRect();
                    const maxX = window.innerWidth - 80 - (rect.left - offsetX);
                    const minX = 80 - rect.right + offsetX;
                    const maxY = window.innerHeight - 40 - (rect.top - offsetY);
                    const minY = -(rect.top - offsetY);
                    offsetX = clamp(event.clientX - startX, minX, maxX);
                    offsetY = clamp(event.clientY - startY, minY, maxY);
                    modal.style.transform = `translate(${offsetX}px, ${offsetY}px)`;
                });

                const stop = event => {
                    handle.classList.remove('dragging');
                    if (handle.hasPointerCapture(event.pointerId)) {
                        handle.releasePointerCapture(event.pointerId);
                    }
                };
                handle.addEventListener('pointerup', stop);
                handle.addEventListener('pointercancel', stop);

                // Clicks stay blocked so an open editor cannot be replaced, but the wheel can scroll the page behind it.
                const backdrop = modal.parentElement;
                backdrop.addEventListener('wheel', event => {
                    if (modal.contains(event.target)) {
                        return;
                    }

                    const panel = document.elementsFromPoint(event.clientX, event.clientY)
                        .find(element => element.classList.contains('panel-body'));
                    if (panel) {
                        panel.scrollTop += event.deltaY;
                    }
                    event.preventDefault();
                }, { passive: false });
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
