// Version: 1.0.2

// Parse JSON if available, else return plain text
async function parseResponse(resp) {
    const ct = resp.headers.get('content-type') || '';
    return ct.includes('application/json') ? await resp.json() : await resp.text();
}

// Generic POST request
async function apiPostRequest(url, payload) {
    const resp = await fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: payload ? JSON.stringify(payload) : null
    });
    return { status: resp.status, body: await parseResponse(resp) };
}

window.apiLogin = (url, payload) => apiPostRequest(url, payload);
window.apiLogout = (url, payload) => apiPostRequest(url, payload);
window.apiSubmitBarcode = (url, payload) => apiPostRequest(url, payload);