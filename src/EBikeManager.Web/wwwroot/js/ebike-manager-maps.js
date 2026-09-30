window.ebikeManagerMaps = (() => {
    const views = new Map();
    const chartHeight = 180;
    const units = { distanceKm: 'km', elevation: 'm', speed: 'km/h', cadence: 'rpm', power: 'W', heartRate: 'bpm' };
    const digits = { distanceKm: 2, elevation: 0, speed: 1, cadence: 0, power: 0, heartRate: 0 };
    const emptyPoint = { type: 'FeatureCollection', features: [] };
    let libraries;

    function load(tag, attributes) {
        return new Promise((resolve, reject) => {
            const element = document.createElement(tag);
            Object.assign(element, attributes);
            element.onload = () => resolve();
            element.onerror = () => reject(new Error(`Could not load ${attributes.src ?? attributes.href}`));
            document.head.appendChild(element);
        });
    }

    function ensureLibraries() {
        libraries ??= Promise.all([
            load('link', { rel: 'stylesheet', href: 'lib/maplibre-gl/maplibre-gl.css' }),
            load('link', { rel: 'stylesheet', href: 'lib/uplot/uPlot.min.css' }),
            load('script', { src: 'lib/maplibre-gl/maplibre-gl.js' }),
            load('script', { src: 'lib/uplot/uPlot.iife.min.js' })
        ]).catch(error => {
            libraries = undefined;
            throw error;
        });
        return libraries;
    }

    function colour(value, fallback) {
        const context = document.createElement('canvas').getContext('2d');
        context.fillStyle = fallback;
        context.fillStyle = value || fallback;
        return context.fillStyle;
    }

    function palette() {
        const style = getComputedStyle(document.documentElement);
        const read = (name, fallback) => colour(style.getPropertyValue(name).trim(), fallback);
        return {
            primary: read('--mud-palette-primary', '#0799e8'),
            surface: read('--mud-palette-surface', '#ffffff'),
            text: read('--mud-palette-text-secondary', '#52525b'),
            grid: read('--mud-palette-lines-default', '#e4e4e7'),
            font: `12px ${getComputedStyle(document.body).fontFamily}`
        };
    }

    function withAlpha(value, alpha) {
        const context = document.createElement('canvas').getContext('2d');
        context.fillStyle = value;
        context.fillRect(0, 0, 1, 1);
        const [red, green, blue] = context.getImageData(0, 0, 1, 1).data;
        return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
    }

    function decimalsOf(step) {
        return (String(+step.toFixed(6)).split('.')[1] ?? '').length;
    }

    function format(value, fractionDigits) {
        return value.toLocaleString(document.documentElement.lang || undefined, { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits });
    }

    function mapStyle(source) {
        if (source.styleUrl) return source.styleUrl;

        const paint = source.dark
            ? { 'raster-brightness-min': 1, 'raster-brightness-max': 0, 'raster-hue-rotate': 180, 'raster-saturation': -0.7, 'raster-contrast': 0.1 }
            : {};
        return {
            version: 8,
            sources: { base: { type: 'raster', tiles: [source.tiles], tileSize: 256, maxzoom: 19, attribution: source.attribution } },
            layers: [{ id: 'base', type: 'raster', source: 'base', paint }]
        };
    }

    function showProblem(view, text) {
        if (view.problem) return;
        view.problem = document.createElement('div');
        view.problem.className = 'em-map-problem';
        view.problem.textContent = text;
        view.mapElement.appendChild(view.problem);
    }

    function clearProblem(view) {
        view.problem?.remove();
        view.problem = undefined;
    }

    function point(coordinates, kind) {
        return { type: 'Feature', geometry: { type: 'Point', coordinates }, properties: { kind } };
    }

    function addRoute(view) {
        const map = view.map;
        const colours = palette();
        const route = view.route;
        if (route.length < 2) return;

        const lineLayout = { 'line-join': 'round', 'line-cap': 'round' };
        map.addSource('route', { type: 'geojson', data: { type: 'Feature', geometry: { type: 'LineString', coordinates: route }, properties: {} } });
        map.addLayer({ id: 'route-casing', type: 'line', source: 'route', layout: lineLayout, paint: { 'line-color': colours.surface, 'line-width': 8, 'line-opacity': 0.9 } });
        map.addLayer({ id: 'route-line', type: 'line', source: 'route', layout: lineLayout, paint: { 'line-color': colours.primary, 'line-width': 4 } });
        map.addSource('ends', { type: 'geojson', data: { type: 'FeatureCollection', features: [point(route[0], 'start'), point(route[route.length - 1], 'end')] } });
        map.addLayer({
            id: 'ends', type: 'circle', source: 'ends',
            paint: {
                'circle-radius': 6,
                'circle-color': ['match', ['get', 'kind'], 'start', colours.surface, colours.primary],
                'circle-stroke-width': 3,
                'circle-stroke-color': ['match', ['get', 'kind'], 'start', colours.primary, colours.surface]
            }
        });
        map.addSource('cursor', { type: 'geojson', data: emptyPoint });
        map.addLayer({ id: 'cursor', type: 'circle', source: 'cursor', paint: { 'circle-radius': 7, 'circle-color': colours.primary, 'circle-stroke-width': 2, 'circle-stroke-color': colours.surface } });
    }

    function fit(view) {
        const route = view.route;
        if (route.length < 2) {
            view.map.jumpTo({ center: [0, 30], zoom: 1 });
            return;
        }

        const bounds = route.reduce((extent, position) => extent.extend(position), new maplibregl.LngLatBounds(route[0], route[0]));
        view.map.fitBounds(bounds, { padding: 40, animate: false, maxZoom: 16 });
    }

    function createMap(view) {
        let map;
        try {
            map = new maplibregl.Map({
                container: view.mapElement,
                style: mapStyle(view.source),
                attributionControl: { compact: true },
                canvasContextAttributes: { preserveDrawingBuffer: true },
                cooperativeGestures: true,
                center: [0, 30],
                zoom: 1
            });
        } catch (error) {
            showProblem(view, view.texts.mapUnavailable);
            return;
        }

        view.map = map;
        view.styleReady = false;
        map.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');
        map.addControl(new maplibregl.FullscreenControl(), 'top-right');
        map.on('style.load', () => {
            view.styleReady = true;
            clearProblem(view);
            addRoute(view);
        });
        map.once('load', () => fit(view));
        map.on('error', event => {
            if (!view.styleReady && !event.sourceId) showProblem(view, view.texts.mapFailed);
        });
    }

    function setCursor(view, index) {
        const series = view.data?.series;
        const source = view.map?.getSource('cursor');
        if (!source) return;

        const longitude = index == null ? null : series.longitude[index];
        const latitude = index == null ? null : series.latitude[index];
        source.setData(longitude == null || latitude == null ? emptyPoint : point([longitude, latitude], 'cursor'));
    }

    function showReadout(view, index) {
        if (view.shownIndex === index) return;
        view.shownIndex = index;
        const readout = view.readoutElement;
        readout.replaceChildren();
        setCursor(view, index);
        if (index == null) {
            readout.textContent = view.texts.hint;
            return;
        }

        const series = view.data.series;
        for (const key of ['distanceKm', ...view.keys]) {
            const value = series[key][index];
            const item = document.createElement('span');
            item.className = 'em-readout-item';
            const strong = document.createElement('strong');
            strong.textContent = value == null ? '–' : `${format(value, digits[key])} ${units[key]}`;
            const label = document.createElement('span');
            label.className = 'em-muted';
            label.textContent = view.texts.labels[key];
            item.append(strong, ' ', label);
            readout.appendChild(item);
        }
    }

    function createCharts(view) {
        const colours = palette();
        const series = view.data.series;
        const axis = { stroke: colours.text, font: colours.font, grid: { stroke: colours.grid, width: 1 }, ticks: { stroke: colours.grid, width: 1 } };
        view.charts?.forEach(chart => chart.destroy());
        view.keys = [];
        view.charts = [];
        view.shownIndex = undefined;

        for (const element of view.chartsElement.querySelectorAll('[data-series]')) {
            const key = element.dataset.series;
            view.keys.push(key);
            element.replaceChildren();
            const options = {
                width: element.clientWidth,
                height: chartHeight,
                legend: { show: false },
                cursor: {
                    y: false,
                    sync: { key: view.id },
                    drag: { x: false, y: false },
                    points: { size: 8, width: 2, stroke: colours.surface, fill: colours.primary }
                },
                scales: { x: { time: false } },
                axes: [
                    { ...axis, values: (chart, splits, axisIndex, space, step) => splits.map(value => `${format(value, decimalsOf(step))} km`) },
                    { ...axis, size: 52, space: 24, values: (chart, splits, axisIndex, space, step) => splits.map(value => format(value, decimalsOf(step))) }
                ],
                series: [
                    {},
                    {
                        stroke: colours.primary,
                        width: 2,
                        fill: key === 'elevation' ? withAlpha(colours.primary, 0.1) : undefined,
                        points: { show: false },
                        spanGaps: true
                    }
                ],
                hooks: { setCursor: [chart => showReadout(view, chart.cursor.idx)] }
            };
            view.charts.push(new uPlot(options, [series.distanceKm, series[key]], element));
        }

        showReadout(view, null);
    }

    function watchSize(view) {
        view.resize = new ResizeObserver(() => {
            for (const chart of view.charts ?? []) {
                const width = chart.root.parentElement?.clientWidth;
                if (width && width !== chart.width) chart.setSize({ width, height: chartHeight });
            }
        });
        view.resize.observe(view.chartsElement);
    }

    function dispose(id) {
        const view = views.get(id);
        if (!view) return;

        view.resize?.disconnect();
        view.charts?.forEach(chart => chart.destroy());
        view.map?.remove();
        views.delete(id);
    }

    return {
        showRide: async function (id, mapElement, chartsElement, readoutElement, data, source, texts) {
            dispose(id);
            await ensureLibraries();
            const view = { id, mapElement, chartsElement, readoutElement, data, source, texts, route: data.route ?? [] };
            views.set(id, view);
            if (mapElement) createMap(view);
            if (chartsElement && readoutElement) {
                createCharts(view);
                watchSize(view);
            }
        },

        showMap: async function (id, mapElement, route, source, texts) {
            dispose(id);
            await ensureLibraries();
            const view = { id, mapElement, source, texts, route: route ?? [] };
            views.set(id, view);
            createMap(view);
        },

        setSource: function (id, source) {
            const view = views.get(id);
            if (!view) return;

            view.source = source;
            requestAnimationFrame(() => {
                if (view.map) {
                    view.styleReady = false;
                    clearProblem(view);
                    view.map.setStyle(mapStyle(source), { diff: false });
                }
                if (view.charts) createCharts(view);
            });
        },

        dispose
    };
})();
