// ---- People drag-and-drop (native HTML5 DnD), shared by the map and to-do pages ----
// Chips carry data-person-id; drop zones carry the .dropzone class and optionally data-drop-id.
// On drop, the active page's DropPerson(personId, fromId, toId) is called, where the ids are the
// data-drop-id of the zone the chip came from and the zone it was dropped on (null when absent).

let currentDropZone = null;
let dotnetRef = null;
let generation = 0;
let dragFromId = null;

function clearDropZone() {
    currentDropZone?.classList.remove('drop-target');
    currentDropZone = null;
}

function elementOf(target) {
    return target?.nodeType === 1 ? target : target?.parentElement;
}

function dropIdOf(zone) {
    return zone?.dataset.dropId ? parseInt(zone.dataset.dropId) : null;
}

document.addEventListener('dragstart', e => {
    const chip = elementOf(e.target)?.closest('[data-person-id]');
    if (chip) {
        // Firefox will not start a drag unless some data is set.
        e.dataTransfer.setData('text/plain', chip.dataset.personId);
        e.dataTransfer.effectAllowed = 'move';
        dragFromId = dropIdOf(chip.closest('.dropzone'));
    }
});

// Highlight the drop zone under the cursor client-side, without a server round trip per event.
document.addEventListener('dragover', e => {
    const zone = elementOf(e.target)?.closest('.dropzone') ?? null;
    if (zone) {
        // Cancelling dragover is what allows a drop on this element.
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    }
    if (zone !== currentDropZone) {
        clearDropZone();
        if (zone) {
            zone.classList.add('drop-target');
            currentDropZone = zone;
        }
    }
});
document.addEventListener('drop', e => {
    clearDropZone();
    const zone = elementOf(e.target)?.closest('.dropzone');
    const personId = parseInt(e.dataTransfer.getData('text/plain'));
    if (!zone || isNaN(personId) || !dotnetRef) return;

    e.preventDefault();
    dotnetRef.invokeMethodAsync('DropPerson', personId, dragFromId, dropIdOf(zone));
});
document.addEventListener('dragend', clearDropZone);

// Returns a token to pass to disposePeopleDragDrop, so a page being torn down after the next
// page has registered does not unregister the new one.
export function initPeopleDragDrop(dotnet) {
    dotnetRef = dotnet;
    return ++generation;
}

export function disposePeopleDragDrop(token) {
    if (token === generation) dotnetRef = null;
}
