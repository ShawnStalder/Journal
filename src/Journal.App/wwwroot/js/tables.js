// Table editing for the rich text editor. Loaded after journal.js, which owns window.journalInterop and calls
// handleKey and handlePaste from its editor listeners.
//
// Browsers have no commands for editing rows and columns, so each change is made on a copy of the table and put back
// with insertHTML. That keeps the whole change as one step for the editor's undo.
(function () {
    const caretMarker = 'data-caret';
    const blockTags = new Set(['P', 'DIV', 'LI', 'UL', 'OL', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'TR', 'BLOCKQUOTE', 'PRE']);

    function elementOf(node) {
        return node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
    }

    function escapeHtml(text) {
        return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function currentCell(editor) {
        const selection = document.getSelection();
        const cell = selection && selection.anchorNode ? elementOf(selection.anchorNode)?.closest('td, th') : null;
        return cell && editor.contains(cell) ? cell : null;
    }

    function placeCaret(element) {
        const range = document.createRange();
        range.selectNodeContents(element);
        range.collapse(true);
        const selection = document.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    }

    function select(node) {
        const range = document.createRange();
        range.selectNode(node);
        const selection = document.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    }

    function newCell(tag) {
        const cell = document.createElement(tag);
        cell.appendChild(document.createElement('br'));
        return cell;
    }

    function convertCell(cell, tag) {
        if (cell.tagName.toLowerCase() === tag) {
            return cell;
        }

        const replacement = document.createElement(tag);
        for (const attribute of cell.attributes) {
            replacement.setAttribute(attribute.name, attribute.value);
        }
        replacement.append(...cell.childNodes);
        cell.replaceWith(replacement);
        return replacement;
    }

    // Header and footer sections are folded into one body so row operations never have to care which section a row is in.
    function flattenSections(table) {
        const rows = [...table.rows];
        table.querySelectorAll(':scope > thead, :scope > tbody, :scope > tfoot').forEach(section => section.remove());
        const body = document.createElement('tbody');
        body.append(...rows);
        table.appendChild(body);
    }

    function markCaret(cell) {
        if (cell) {
            cell.setAttribute(caretMarker, '');
        }
    }

    function cellAt(table, rowIndex, cellIndex) {
        const row = table.rows[Math.min(rowIndex, table.rows.length - 1)];
        return row ? row.cells[Math.min(cellIndex, row.cells.length - 1)] : null;
    }

    function addRow(context, above) {
        const reference = context.table.rows[context.rowIndex];
        const isHeader = [...reference.cells].every(cell => cell.tagName === 'TH');
        const row = reference.cloneNode(false);
        const becomesHeader = above && isHeader;

        for (let index = 0; index < reference.cells.length; index++) {
            row.appendChild(newCell(becomesHeader ? 'th' : 'td'));
        }

        if (becomesHeader) {
            [...reference.cells].forEach(cell => convertCell(cell, 'td'));
        }

        if (above) {
            reference.before(row);
        } else {
            reference.after(row);
        }

        markCaret(row.cells[Math.min(context.cellIndex, row.cells.length - 1)]);
    }

    function addColumn(context, left) {
        const at = left ? context.cellIndex : context.cellIndex + 1;

        for (const row of context.table.rows) {
            const neighbor = row.cells[Math.min(context.cellIndex, row.cells.length - 1)];
            const cell = newCell(neighbor ? neighbor.tagName.toLowerCase() : 'td');
            const before = row.cells[at];
            if (before) {
                before.before(cell);
            } else {
                row.appendChild(cell);
            }

            if (row.rowIndex === context.rowIndex) {
                markCaret(cell);
            }
        }
    }

    function deleteRow(context) {
        context.table.rows[context.rowIndex].remove();
        markCaret(cellAt(context.table, context.rowIndex, context.cellIndex));
    }

    function deleteColumn(context) {
        for (const row of context.table.rows) {
            row.cells[context.cellIndex]?.remove();
        }

        markCaret(cellAt(context.table, context.rowIndex, context.cellIndex));
    }

    function toggleHeader(context) {
        const first = context.table.rows[0];
        const isHeader = [...first.cells].every(cell => cell.tagName === 'TH');
        [...first.cells].forEach(cell => convertCell(cell, isHeader ? 'td' : 'th'));
        markCaret(cellAt(context.table, context.rowIndex, context.cellIndex));
    }

    function shade(context, color) {
        for (const [rowIndex, cellIndex] of context.affected) {
            const cell = context.table.rows[rowIndex]?.cells[cellIndex];
            if (!cell) {
                continue;
            }

            cell.style.backgroundColor = color || '';
            if (cell.getAttribute('style') === '') {
                cell.removeAttribute('style');
            }
        }

        markCaret(cellAt(context.table, context.rowIndex, context.cellIndex));
    }

    // With a range selected, every cell the selection touches is shaded; otherwise just the cell with the caret.
    function affectedCells(table, cell) {
        const selection = document.getSelection();
        const range = selection.rangeCount > 0 ? selection.getRangeAt(0) : null;
        const affected = [];

        [...table.rows].forEach((row, rowIndex) => {
            [...row.cells].forEach((candidate, cellIndex) => {
                const isTouched = range && !range.collapsed ? range.intersectsNode(candidate) : candidate === cell;
                if (isTouched) {
                    affected.push([rowIndex, cellIndex]);
                }
            });
        });

        return affected;
    }

    function removeTable(editor, table) {
        select(table);
        document.execCommand('delete');
        if (editor.childNodes.length === 0) {
            const paragraph = document.createElement('p');
            paragraph.appendChild(document.createElement('br'));
            editor.appendChild(paragraph);
            placeCaret(paragraph);
        }
    }

    function replaceTable(editor, table, change) {
        const copy = table.cloneNode(true);
        flattenSections(copy);
        change(copy);

        select(table);
        document.execCommand('insertHTML', false, copy.outerHTML);

        const target = editor.querySelector(`[${caretMarker}]`);
        if (target) {
            target.removeAttribute(caretMarker);
            placeCaret(target);
        }
    }

    function removesTable(action, table, cell) {
        return action === 'deleteTable'
            || (action === 'deleteRow' && table.rows.length === 1)
            || (action === 'deleteColumn' && cell.parentElement.cells.length === 1);
    }

    const changes = {
        addRowAbove: context => addRow(context, true),
        addRowBelow: context => addRow(context, false),
        addColumnLeft: context => addColumn(context, true),
        addColumnRight: context => addColumn(context, false),
        deleteRow,
        deleteColumn,
        toggleHeader,
        shade: (context, color) => shade(context, color)
    };

    function execute(editor, action, argument) {
        const cell = currentCell(editor);
        if (!cell) {
            return;
        }

        const table = cell.closest('table');
        if (removesTable(action, table, cell)) {
            removeTable(editor, table);
            return;
        }

        const change = changes[action];
        if (!change) {
            return;
        }

        const context = {
            rowIndex: cell.parentElement.rowIndex,
            cellIndex: cell.cellIndex,
            affected: action === 'shade' ? affectedCells(table, cell) : [],
            table: null
        };
        replaceTable(editor, table, copy => {
            context.table = copy;
            change(context, argument);
        });
    }

    function buildTableHtml(rows, columns) {
        const cells = tag => `<${tag}><br></${tag}>`.repeat(columns);
        const body = Array.from({ length: rows }, (_, index) => `<tr>${cells(index === 0 ? 'th' : 'td')}</tr>`).join('');
        return `<table data-new><tbody>${body}</tbody></table><p><br></p>`;
    }

    // A table cannot go inside another table or a code block, so the new one is placed after it.
    function moveCaretOutOfContainer(editor) {
        const selection = document.getSelection();
        const container = selection.anchorNode ? elementOf(selection.anchorNode)?.closest('table, pre') : null;
        if (!container || !editor.contains(container)) {
            return;
        }

        let next = container.nextElementSibling;
        if (!next) {
            next = document.createElement('p');
            next.appendChild(document.createElement('br'));
            container.after(next);
        }

        const range = document.createRange();
        range.setStart(next, 0);
        range.collapse(true);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    function insert(editor, rows, columns) {
        window.journalInterop.rte.restoreSelection(editor);
        moveCaretOutOfContainer(editor);
        document.execCommand('insertHTML', false, buildTableHtml(rows, columns));

        const table = editor.querySelector('table[data-new]');
        if (table) {
            table.removeAttribute('data-new');
            placeCaret(table.rows[0].cells[0]);
        }
    }

    // Tab walks the cells, and Tab in the last cell adds a row. Lists inside a cell keep their own indent behavior.
    function handleKey(event) {
        if (event.key !== 'Tab' || event.ctrlKey || event.altKey || event.metaKey) {
            return false;
        }

        const editor = event.currentTarget;
        const cell = currentCell(editor);
        const inList = cell && elementOf(document.getSelection().anchorNode)?.closest('li');
        if (!cell || (inList && cell.contains(inList))) {
            return false;
        }

        event.preventDefault();
        const cells = [...cell.closest('table').rows].flatMap(row => [...row.cells]);
        const index = cells.indexOf(cell);

        if (event.shiftKey) {
            if (index > 0) {
                placeCaret(cells[index - 1]);
            }
        } else if (index < cells.length - 1) {
            placeCaret(cells[index + 1]);
        } else {
            execute(editor, 'addRowBelow');
            const current = currentCell(editor);
            if (current) {
                placeCaret(current.parentElement.cells[0]);
            }
        }

        return true;
    }

    function textOf(node) {
        let text = '';
        node.childNodes.forEach(child => {
            if (child.nodeType === Node.TEXT_NODE) {
                text += child.data.replace(/\s+/g, ' ');
            } else if (child.nodeName === 'BR') {
                text += '\n';
            } else if (child.nodeType === Node.ELEMENT_NODE) {
                const inner = textOf(child);
                text += blockTags.has(child.nodeName) ? `\n${inner}\n` : inner;
            }
        });
        return text;
    }

    function cellHtml(cell) {
        const lines = textOf(cell).split('\n').map(line => line.trim()).filter(line => line !== '');
        return lines.length === 0 ? '<br>' : lines.map(escapeHtml).join('<br>');
    }

    function spanAttributes(cell) {
        return ['colspan', 'rowspan']
            .filter(name => Number(cell.getAttribute(name)) > 1)
            .map(name => ` ${name}="${Number(cell.getAttribute(name))}"`)
            .join('');
    }

    // Pasted tables (Excel, Word, web pages) carry a lot of styling, so only the structure and the text are kept.
    function cleanTable(table) {
        const rows = [...table.rows].map(row => {
            const cells = [...row.cells].map(cell => {
                const tag = cell.tagName.toLowerCase();
                return `<${tag}${spanAttributes(cell)}>${cellHtml(cell)}</${tag}>`;
            });
            return `<tr>${cells.join('')}</tr>`;
        });
        return `<table><tbody>${rows.join('')}</tbody></table>`;
    }

    function collect(node, output) {
        node.childNodes.forEach(child => {
            if (child.nodeName === 'TABLE') {
                output.push(cleanTable(child));
            } else if (child.nodeType === Node.ELEMENT_NODE && child.querySelector('table')) {
                collect(child, output);
            } else {
                const text = child.nodeType === Node.TEXT_NODE ? child.data : textOf(child);
                const lines = text.split('\n').map(line => line.trim()).filter(line => line !== '');
                lines.forEach(line => output.push(`<p>${escapeHtml(line)}</p>`));
            }
        });
    }

    function buildPasteHtml(html) {
        const parsed = new DOMParser().parseFromString(html, 'text/html');
        const output = [];
        collect(parsed.body, output);

        // The caret needs somewhere to go after a table that ends the pasted content.
        if (output.length > 0 && output[output.length - 1].startsWith('<table')) {
            output.push('<p><br></p>');
        }

        return output.join('');
    }

    function handlePaste(event) {
        const html = event.clipboardData.getData('text/html');
        if (!html || !/<table/i.test(html)) {
            return false;
        }

        event.preventDefault();
        const editor = event.currentTarget;

        if (currentCell(editor)) {
            const text = (event.clipboardData.getData('text/plain') || '').replace(/\t/g, ' ').replace(/\r\n?/g, '\n');
            document.execCommand('insertText', false, text);
            return true;
        }

        moveCaretOutOfContainer(editor);
        document.execCommand('insertHTML', false, buildPasteHtml(html));
        return true;
    }

    window.journalInterop.table = {
        insert,
        exec: (editor, action, argument) => {
            window.journalInterop.rte.restoreSelection(editor);
            execute(editor, action, argument);
        },
        handleKey,
        handlePaste
    };
})();
