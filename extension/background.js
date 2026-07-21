const API_BASE = "http://localhost:5183"; // swap to https://api.zapstar.app in production

async function apiGet(path) {
  const res = await fetch(`${API_BASE}${path}`);
  if (!res.ok) throw new Error(`API error ${res.status}`);
  return res.json();
}

async function apiPost(path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.error || `API error ${res.status}`);
  return data;
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  (async () => {
    try {
      switch (message.type) {
        case "RESOLVE_REPO": {
          const { owner, repo } = message.payload;
          const data = await apiGet(`/repo/${owner}/${repo}`);
          sendResponse({ ok: true, data });
          break;
        }
        case "RESOLVE_USER": {
          const { username } = message.payload;
          const data = await apiGet(`/user/${username}`);
          sendResponse({ ok: true, data });
          break;
        }
        case "GET_INVOICE": {
          const { address, amountSats, comment } = message.payload;
          const data = await apiPost("/invoice", { address, amountSats, comment });
          sendResponse({ ok: true, data });
          break;
        }
        default:
          sendResponse({ ok: false, error: `Unknown message type: ${message.type}` });
      }
    } catch (err) {
      sendResponse({ ok: false, error: err.message });
    }
  })();

  return true;
});