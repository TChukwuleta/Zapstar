function sendMessage(type, payload) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage({ type, payload }, (response) => {
      if (chrome.runtime.lastError) return reject(new Error(chrome.runtime.lastError.message));
      if (!response.ok) return reject(new Error(response.error));
      resolve(response.data);
    });
  });
}

function parseRepoFromUrl() {
  const match = window.location.pathname.match(/^\/([^/]+)\/([^/]+)\/?$/);
  if (!match) return null;
  const [, owner, repo] = match;
  if (["settings", "notifications", "marketplace", "explore"].includes(owner)) return null;
  return { owner, repo };
}

function parseUserFromUrl() {
  const match = window.location.pathname.match(/^\/([^/]+)\/?$/);
  if (!match) return null;
  const [, username] = match;
  if (["settings", "notifications", "marketplace", "explore", "sponsors"].includes(username)) return null;
  return { username };
}

function makeZapButton(target, label) {
  const btn = document.createElement("button");
  btn.className = "zs-zap-btn";
  btn.type = "button";
  btn.innerHTML = `⚡ Zap ${label}`;
  btn.addEventListener("click", () => window.Zapstar.openZapModal(target));
  return btn;
}

async function injectRepoButton() {
  const repoInfo = parseRepoFromUrl();
  if (!repoInfo) return;

  try {
    const target = await sendMessage("RESOLVE_REPO", repoInfo);
    if (!target.hasLightning) return;

    if (document.querySelector(".zs-zap-btn")) return;
    
    const sidebar =
      document.querySelector(".Layout-sidebar") ||
      document.querySelector("#repository-details-container");
    if (!sidebar) return;

    const wrapper = document.createElement("div");
    wrapper.className = "zs-zap-wrapper-sidebar";
    wrapper.appendChild(makeZapButton(target, repoInfo.repo));
    sidebar.appendChild(wrapper);
  } catch (err) {
    console.debug("[Zapstar] repo resolve failed:", err.message);
  }
}

async function injectProfileButton() {
  const userInfo = parseUserFromUrl();
  if (!userInfo) return;

  try {
    const target = await sendMessage("RESOLVE_USER", userInfo);
    if (!target.hasLightning) return;

    const sidebar = document.querySelector(".vcard-names-container, .h-card");
    if (!sidebar || document.querySelector(".zs-zap-btn")) return;

    const wrapper = document.createElement("div");
    wrapper.className = "zs-zap-wrapper";
    wrapper.appendChild(makeZapButton(target, userInfo.username));
    sidebar.after(wrapper);
  } catch (err) {
    console.debug("[Zapstar] user resolve failed:", err.message);
  }
}

async function injectContributorButtons() {
  if (!window.location.pathname.endsWith("/graphs/contributors")) return;

  const contributorCards = document.querySelectorAll("li.wrapper a.avatar");
  for (const avatar of contributorCards) {
    const username = avatar.getAttribute("href")?.replace("/", "");
    if (!username || avatar.dataset.zsChecked) continue;
    avatar.dataset.zsChecked = "1";

    try {
      const target = await sendMessage("RESOLVE_USER", { username });
      if (!target.hasLightning) continue;

      const icon = document.createElement("span");
      icon.className = "zs-zap-icon";
      icon.title = `Zap ${username}`;
      icon.textContent = "⚡";
      icon.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        window.Zapstar.openZapModal(target);
      });
      avatar.parentElement?.appendChild(icon);
    } catch (err) {
      console.debug(`[Zapstar] contributor resolve failed for ${username}:`, err.message);
    }
  }
}

function run() {
  injectRepoButton();
  injectProfileButton();
  injectContributorButtons();
}

run();

document.addEventListener("turbo:load", run);

let lastPath = window.location.pathname;
new MutationObserver(() => {
  if (window.location.pathname !== lastPath) {
    lastPath = window.location.pathname;
    setTimeout(run, 500);
  }
}).observe(document.body, { childList: true, subtree: true });