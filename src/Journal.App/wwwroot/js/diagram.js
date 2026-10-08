// Mermaid diagram support: lazy-loads the bundled library, renders into a pan/zoom viewport and copies SVG/PNG.
// Loaded after journal.js, which owns window.journalInterop.
(function () {
    const mermaidUrl = 'lib/mermaid/mermaid.min.js';
    const zoomStep = 1.25;
    const minScale = 0.1;
    const maxScale = 8;
    const fitPadding = 16;
    const maxPngSide = 8000;
    const feedbackMilliseconds = 1500;

    let loading = null;
    let queue = Promise.resolve();
    let nextId = 0;

    function loadMermaid() {
        if (window.mermaid) {
            return Promise.resolve(window.mermaid);
        }

        if (!loading) {
            loading = new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = mermaidUrl;
                script.onload = () => resolve(window.mermaid);
                script.onerror = () => {
                    loading = null;
                    reject(new Error('The diagram library could not be loaded.'));
                };
                document.head.appendChild(script);
            });
        }

        return loading;
    }

    // Mermaid keeps global state while rendering, so renders run one at a time.
    function enqueue(task) {
        const result = queue.then(task);
        queue = result.catch(() => { });
        return result;
    }

    function isDark() {
        return document.documentElement.getAttribute('data-theme') !== 'light';
    }

    function messageOf(error) {
        return (error && error.message) ? error.message : String(error);
    }

    async function renderSvg(source, dark) {
        const mermaid = await loadMermaid();
        // 'strict' stops scripts and click handlers in labels; notes can be edited by hand on the share.
        // HTML labels are off so the SVG can also be drawn to a canvas for PNG export.
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'strict',
            theme: dark ? 'dark' : 'default',
            htmlLabels: false,
            flowchart: { htmlLabels: false },
            fontFamily: '"Segoe UI", sans-serif'
        });

        const id = `journal-diagram-${nextId++}`;
        try {
            await mermaid.parse(source);
            const { svg } = await mermaid.render(id, source);
            return svg;
        } finally {
            // A failed render leaves its scratch elements in the page.
            document.getElementById(id)?.remove();
            document.getElementById(`d${id}`)?.remove();
        }
    }

    function sizeOf(svg) {
        const box = svg.viewBox && svg.viewBox.baseVal;
        if (box && box.width > 0 && box.height > 0) {
            return { width: box.width, height: box.height };
        }

        const rect = svg.getBoundingClientRect();
        return { width: rect.width || 300, height: rect.height || 150 };
    }

    function applyTransform(view) {
        view.canvas.style.transform = `translate(${view.x}px, ${view.y}px) scale(${view.scale})`;
        view.zoomLabel.textContent = `${Math.round(view.scale * 100)}%`;
    }

    function fit(view) {
        view.adjusted = false;
        const available = { width: view.viewport.clientWidth, height: view.viewport.clientHeight };
        if (!view.size || available.width === 0 || available.height === 0) {
            return;
        }

        const widthScale = (available.width - fitPadding * 2) / view.size.width;
        const heightScale = (available.height - fitPadding * 2) / view.size.height;
        view.scale = Math.max(minScale, Math.min(widthScale, heightScale, 1));
        view.x = (available.width - view.size.width * view.scale) / 2;
        view.y = (available.height - view.size.height * view.scale) / 2;
        applyTransform(view);
    }

    function zoomAt(view, factor, centerX, centerY) {
        const next = Math.min(maxScale, Math.max(minScale, view.scale * factor));
        const ratio = next / view.scale;
        view.x = centerX - (centerX - view.x) * ratio;
        view.y = centerY - (centerY - view.y) * ratio;
        view.scale = next;
        view.adjusted = true;
        applyTransform(view);
    }

    function zoomFromCenter(view, factor) {
        zoomAt(view, factor, view.viewport.clientWidth / 2, view.viewport.clientHeight / 2);
    }

    function showError(view, message) {
        view.error.textContent = message;
        view.error.hidden = false;
    }

    function show(view, svgText) {
        view.error.hidden = true;
        view.canvas.innerHTML = svgText;

        const svg = view.canvas.querySelector('svg');
        if (!svg) {
            return;
        }

        view.size = sizeOf(svg);
        svg.style.maxWidth = 'none';
        svg.setAttribute('width', view.size.width);
        svg.setAttribute('height', view.size.height);

        if (view.adjusted) {
            applyTransform(view);
        } else {
            fit(view);
        }
    }

    async function render(view) {
        const token = ++view.token;
        const source = view.source;

        if (!source.trim()) {
            view.canvas.replaceChildren();
            view.size = null;
            showError(view, 'Nothing to preview yet.');
            return;
        }

        try {
            const svg = await enqueue(() => (token === view.token ? renderSvg(source, isDark()) : null));
            if (svg !== null && token === view.token && view.root.isConnected) {
                show(view, svg);
            }
        } catch (error) {
            // The last good diagram stays visible while the source has a syntax error.
            if (token === view.token) {
                showError(view, messageOf(error));
            }
        }
    }

    function flash(button, text) {
        const original = button.dataset.label ?? button.textContent;
        button.dataset.label = original;
        button.textContent = text;
        setTimeout(() => { button.textContent = original; }, feedbackMilliseconds);
    }

    // Copies always use the light theme so the picture reads well when pasted into other documents.
    async function exportSvg(view) {
        const svgText = await enqueue(() => renderSvg(view.source, false));
        const parsed = new DOMParser().parseFromString(svgText, 'image/svg+xml');
        const svg = parsed.documentElement;
        if (parsed.querySelector('parsererror')) {
            return { text: svgText, size: view.size };
        }

        const size = sizeOf(svg);
        svg.removeAttribute('style');
        svg.setAttribute('width', size.width);
        svg.setAttribute('height', size.height);
        return { text: new XMLSerializer().serializeToString(svg), size };
    }

    async function toPngBlob(view) {
        const { text, size } = await exportSvg(view);
        const factor = Math.min(2, maxPngSide / Math.max(size.width, size.height));
        const image = new Image();
        await new Promise((resolve, reject) => {
            image.onload = resolve;
            image.onerror = () => reject(new Error('The diagram could not be converted to an image.'));
            image.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(text)}`;
        });

        const canvas = document.createElement('canvas');
        canvas.width = Math.ceil(size.width * factor);
        canvas.height = Math.ceil(size.height * factor);
        const context = canvas.getContext('2d');
        context.fillStyle = '#ffffff';
        context.fillRect(0, 0, canvas.width, canvas.height);
        context.drawImage(image, 0, 0, canvas.width, canvas.height);

        return new Promise((resolve, reject) => {
            canvas.toBlob(blob => (blob ? resolve(blob) : reject(new Error('The image could not be created.'))), 'image/png');
        });
    }

    async function copy(view, button, action) {
        try {
            await action();
            flash(button, 'Copied');
        } catch (error) {
            flash(button, 'Failed');
            showError(view, `Copy failed: ${messageOf(error)}`);
        }
    }

    function handleToolbarClick(view, event) {
        const button = event.target.closest('[data-action]');
        if (!button || !view.root.contains(button)) {
            return;
        }

        switch (button.dataset.action) {
            case 'zoom-in':
                zoomFromCenter(view, zoomStep);
                break;
            case 'zoom-out':
                zoomFromCenter(view, 1 / zoomStep);
                break;
            case 'fit':
                fit(view);
                break;
            case 'copy-svg':
                copy(view, button, async () => window.journalInterop.copyText((await exportSvg(view)).text));
                break;
            case 'copy-png':
                // The clipboard item takes a promise so the write starts inside the click.
                copy(view, button, () => navigator.clipboard.write([new ClipboardItem({ 'image/png': toPngBlob(view) })]));
                break;
        }
    }

    function attachPanning(view) {
        let startX = 0;
        let startY = 0;

        view.viewport.addEventListener('pointerdown', event => {
            if (event.button !== 0) {
                return;
            }

            startX = event.clientX - view.x;
            startY = event.clientY - view.y;
            view.viewport.setPointerCapture(event.pointerId);
            view.viewport.classList.add('panning');
        });

        view.viewport.addEventListener('pointermove', event => {
            if (!view.viewport.hasPointerCapture(event.pointerId)) {
                return;
            }

            view.x = event.clientX - startX;
            view.y = event.clientY - startY;
            view.adjusted = true;
            applyTransform(view);
        });

        const stop = event => {
            view.viewport.classList.remove('panning');
            if (view.viewport.hasPointerCapture(event.pointerId)) {
                view.viewport.releasePointerCapture(event.pointerId);
            }
        };
        view.viewport.addEventListener('pointerup', stop);
        view.viewport.addEventListener('pointercancel', stop);
    }

    function createView(root) {
        const viewport = root.querySelector('[data-role="viewport"]');
        const canvas = document.createElement('div');
        canvas.className = 'diagram-canvas';
        viewport.appendChild(canvas);

        const view = {
            root,
            viewport,
            canvas,
            error: root.querySelector('[data-role="error"]'),
            zoomLabel: root.querySelector('[data-role="zoom"]'),
            source: '',
            size: null,
            scale: 1,
            x: 0,
            y: 0,
            adjusted: false,
            token: 0
        };

        root.addEventListener('click', event => handleToolbarClick(view, event));
        viewport.addEventListener('dblclick', () => fit(view));
        // Plain wheel keeps scrolling the page; Ctrl+wheel zooms toward the pointer.
        viewport.addEventListener('wheel', event => {
            if (!event.ctrlKey) {
                return;
            }

            event.preventDefault();
            const rect = viewport.getBoundingClientRect();
            zoomAt(view, event.deltaY < 0 ? zoomStep : 1 / zoomStep, event.clientX - rect.left, event.clientY - rect.top);
        }, { passive: false });
        attachPanning(view);
        new ResizeObserver(() => {
            if (!view.adjusted) {
                fit(view);
            }
        }).observe(viewport);

        return view;
    }

    window.journalInterop.diagram = {
        mount(root, source) {
            root._diagramView ??= createView(root);
            root._diagramView.source = source;
            return render(root._diagramView);
        },

        // Resolves to null when the source parses, otherwise to the parser's message.
        validate(source) {
            if (!source.trim()) {
                return Promise.resolve('The diagram is empty.');
            }

            return enqueue(async () => {
                try {
                    const mermaid = await loadMermaid();
                    await mermaid.parse(source);
                    return null;
                } catch (error) {
                    return messageOf(error);
                }
            });
        },

        refreshAll() {
            document.querySelectorAll('.diagram-view').forEach(root => {
                if (root._diagramView) {
                    render(root._diagramView);
                }
            });
        },

        attachEditor(textarea) {
            textarea.addEventListener('keydown', event => {
                if (event.key === 'Tab' && !event.shiftKey && !event.ctrlKey && !event.altKey) {
                    event.preventDefault();
                    document.execCommand('insertText', false, '    ');
                }
            });
        }
    };
})();
