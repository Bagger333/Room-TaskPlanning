import { initPeopleDragDrop, disposePeopleDragDrop } from './people-dnd.js?v=1';

// ---- Edit mode: draw, move and resize buckets ----

const MIN_SIZE = 2; // percent of the map

const clamp = (v, min, max) => Math.min(Math.max(v, min), max);

export function initMap(map, dotnet) {
    const dndToken = initPeopleDragDrop(dotnet);
    const onPointerDown = e => {
        if (!map.classList.contains('editing') || e.button !== 0) return;
        // Let people chips use native drag-and-drop even while editing buckets.
        if (e.target.closest('.chip')) return;

        const bounds = map.getBoundingClientRect();
        const toPercent = ev => ({
            x: clamp((ev.clientX - bounds.left) / bounds.width * 100, 0, 100),
            y: clamp((ev.clientY - bounds.top) / bounds.height * 100, 0, 100),
        });
        const start = toPercent(e);

        const bucketEl = e.target.closest('.bucket');
        let el, mode, orig;
        if (bucketEl) {
            el = bucketEl;
            mode = e.target.closest('.resize-handle') ? 'resize' : 'move';
            orig = {
                x: parseFloat(el.style.left), y: parseFloat(el.style.top),
                w: parseFloat(el.style.width), h: parseFloat(el.style.height),
            };
        } else {
            el = document.createElement('div');
            el.className = 'bucket-draft';
            map.appendChild(el);
            mode = 'draw';
            orig = { x: start.x, y: start.y, w: 0, h: 0 };
        }

        let rect = { ...orig };
        let moved = false;

        const onPointerMove = ev => {
            const p = toPercent(ev);
            const dx = p.x - start.x, dy = p.y - start.y;
            if (Math.abs(dx) + Math.abs(dy) > 0.3) moved = true;

            if (mode === 'move') {
                rect = { ...orig, x: clamp(orig.x + dx, 0, 100 - orig.w), y: clamp(orig.y + dy, 0, 100 - orig.h) };
            } else if (mode === 'resize') {
                rect = { ...orig, w: clamp(orig.w + dx, MIN_SIZE, 100 - orig.x), h: clamp(orig.h + dy, MIN_SIZE, 100 - orig.y) };
            } else {
                rect = { x: Math.min(start.x, p.x), y: Math.min(start.y, p.y), w: Math.abs(dx), h: Math.abs(dy) };
            }

            el.style.left = rect.x + '%';
            el.style.top = rect.y + '%';
            el.style.width = rect.w + '%';
            el.style.height = rect.h + '%';
        };

        const onPointerUp = async () => {
            map.removeEventListener('pointermove', onPointerMove);
            map.removeEventListener('pointerup', onPointerUp);
            map.removeEventListener('pointercancel', onPointerUp);

            if (mode === 'draw') {
                el.remove();
                if (rect.w >= MIN_SIZE && rect.h >= MIN_SIZE) {
                    await dotnet.invokeMethodAsync('CreateBucket', rect.x, rect.y, rect.w, rect.h);
                } else {
                    await dotnet.invokeMethodAsync('SelectBucket', null);
                }
            } else {
                await dotnet.invokeMethodAsync('BucketPointerUp',
                    parseInt(el.dataset.bucketId), rect.x, rect.y, rect.w, rect.h, moved);
            }
        };

        map.setPointerCapture(e.pointerId);
        map.addEventListener('pointermove', onPointerMove);
        map.addEventListener('pointerup', onPointerUp);
        map.addEventListener('pointercancel', onPointerUp);
        e.preventDefault();
    };

    map.addEventListener('pointerdown', onPointerDown);
    map._plannerDispose = () => {
        map.removeEventListener('pointerdown', onPointerDown);
        disposePeopleDragDrop(dndToken);
    };
}

export function dispose(map) {
    map?._plannerDispose?.();
}
