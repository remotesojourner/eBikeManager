window.ebikeManagerMaps = (() => {
    const views = new Map();
    const chartHeight = 180;
    const digits = { distanceKm: 2, elevation: 0, speed: 1, cadence: 0, power: 0, heartRate: 0 };
    const routeKeys = ['speed', 'power', 'cadence', 'elevation', 'heartRate'];
    const heat = ['#fed976', '#feb24c', '#fd8d3c', '#fc4e2a', '#e31a1c'];
    const heatCasing = '#1a1a19';
    const ridePadding = { top: 72, bottom: 96, left: 40, right: 56 };
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

    function scaled(series, factors) {
        const result = { ...series };
        for (const [key, factor] of Object.entries(factors)) {
            if (series[key]) result[key] = series[key].map(value => value == null ? null : value * factor);
        }
        return result;
    }

    function ranges(values) {
        const result = {};
        for (const key of routeKeys) {
            const sorted = (values?.[key] ?? []).filter(value => value != null).sort((a, b) => a - b);
            if (sorted.length < 2) continue;

            const low = sorted[Math.floor((sorted.length - 1) * 0.05)];
            const high = sorted[Math.ceil((sorted.length - 1) * 0.95)];
            result[key] = { low, high: high > low ? high : low + 1 };
        }
        return result;
    }

    function segments(route, values) {
        const features = [];
        for (let index = 1; index < route.length; index++) {
            const properties = {};
            for (const key of routeKeys) {
                const before = values[key]?.[index - 1];
                const after = values[key]?.[index];
                if (before != null && after != null) properties[key] = (before + after) / 2;
            }
            features.push({ type: 'Feature', geometry: { type: 'LineString', coordinates: [route[index - 1], route[index]] }, properties });
        }
        return { type: 'FeatureCollection', features };
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
        if (view.routeValues) {
            map.addSource('route-colour', { type: 'geojson', data: segments(route, view.routeValues), tolerance: 0 });
            map.addLayer({ id: 'route-colour', type: 'line', source: 'route-colour', layout: { ...lineLayout, visibility: 'none' }, paint: { 'line-color': heat[0], 'line-width': 4 } });
        }
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
        applyColouring(view);
    }

    function applyColouring(view) {
        const map = view.map;
        if (!map?.getLayer('route-line')) return;

        const key = view.colourBy;
        const range = key ? view.ranges?.[key] : undefined;
        if (map.getLayer('route-colour')) {
            map.setLayoutProperty('route-colour', 'visibility', range ? 'visible' : 'none');
            if (range) {
                const stops = heat.flatMap((colour, index) => [range.low + (range.high - range.low) * index / (heat.length - 1), colour]);
                map.setPaintProperty('route-colour', 'line-color', ['case', ['has', key], ['interpolate', ['linear'], ['get', key], ...stops], heat[0]]);
            }
        }
        map.setLayoutProperty('route-line', 'visibility', range ? 'none' : 'visible');
        map.setPaintProperty('route-casing', 'line-color', range ? heatCasing : palette().surface);
        showLegend(view, key, range);
    }

    function showLegend(view, key, range) {
        if (!range) {
            view.legend?.remove();
            view.legend = undefined;
            return;
        }

        view.legend ??= view.mapElement.appendChild(document.createElement('div'));
        view.legend.className = 'em-map-legend';
        view.legend.replaceChildren();
        const title = document.createElement('div');
        title.className = 'em-map-legend-title';
        title.textContent = `${view.texts.labels[key]} · ${view.units.labels[key]}`;
        const bar = document.createElement('div');
        bar.className = 'em-map-legend-bar';
        bar.style.background = `linear-gradient(to right, ${heat.join(', ')})`;
        const scale = document.createElement('div');
        scale.className = 'em-map-legend-scale';
        for (const value of [range.low, range.high]) {
            const label = document.createElement('span');
            label.textContent = format(value, digits[key]);
            scale.appendChild(label);
        }
        view.legend.append(title, bar, scale);
    }

    function fit(view) {
        const route = view.route;
        if (route.length < 2) {
            view.map.jumpTo({ center: [0, 30], zoom: 1 });
            return;
        }

        const bounds = route.reduce((extent, position) => extent.extend(position), new maplibregl.LngLatBounds(route[0], route[0]));
        view.map.fitBounds(bounds, { padding: view.padding ?? 40, animate: false, maxZoom: 16 });
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

        const series = view.series;
        for (const key of ['distanceKm', ...view.keys]) {
            const value = series[key][index];
            const item = document.createElement('span');
            item.className = 'em-readout-item';
            const strong = document.createElement('strong');
            strong.textContent = value == null ? '–' : `${format(value, digits[key])} ${view.units.labels[key]}`;
            const label = document.createElement('span');
            label.className = 'em-muted';
            label.textContent = view.texts.labels[key];
            item.append(strong, ' ', label);
            readout.appendChild(item);
        }
    }

    function createCharts(view) {
        const colours = palette();
        const series = view.series;
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
                    { ...axis, values: (chart, splits, axisIndex, space, step) => splits.map(value => `${format(value, decimalsOf(step))} ${view.units.labels.distanceKm}`) },
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
        view.legend?.remove();
        view.map?.remove();
        views.delete(id);
    }

    return {
        showRide: async function (id, mapElement, chartsElement, readoutElement, data, source, texts, units, colourBy) {
            dispose(id);
            await ensureLibraries();
            const routeValues = scaled(data.routeValues, units.factors);
            const view = {
                id, mapElement, chartsElement, readoutElement, data, source, texts, units, colourBy, routeValues,
                padding: ridePadding,
                ranges: ranges(routeValues),
                series: scaled(data.series, units.factors),
                route: data.route ?? []
            };
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

        setColour: function (id, colourBy) {
            const view = views.get(id);
            if (!view) return;

            view.colourBy = colourBy;
            if (view.styleReady) applyColouring(view);
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
