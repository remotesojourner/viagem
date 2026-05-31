window.travelBoard = {
    _map: null,

    initMap: function (containerId, pins) {
        if (this._map) {
            this._map.remove();
            this._map = null;
        }

        const el = document.getElementById(containerId);
        if (!el) return;

        const map = L.map(containerId, { scrollWheelZoom: false });
        this._map = map;

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 18
        }).addTo(map);

        if (!pins || pins.length === 0) {
            map.setView([20, 0], 2);
            return;
        }

        const icon = L.divIcon({
            className: '',
            html: '<div style="width:12px;height:12px;background:#6366f1;border:2px solid white;border-radius:50%;box-shadow:0 1px 4px rgba(0,0,0,.4)"></div>',
            iconSize: [12, 12],
            iconAnchor: [6, 6]
        });

        const group = L.featureGroup();
        pins.forEach(p => {
            const lat = parseFloat(p.latitude);
            const lng = parseFloat(p.longitude);
            if (isNaN(lat) || isNaN(lng)) return;
            L.marker([lat, lng], { icon })
                .bindPopup(`<strong>${p.name}</strong>`)
                .addTo(group);
        });

        group.addTo(map);
        map.fitBounds(group.getBounds().pad(0.15));
    }
};
