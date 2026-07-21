(function () {
  const PRESET_AMOUNTS = [1000, 5000, 21000];

  function sendMessage(type, payload) {
    return new Promise((resolve, reject) => {
      chrome.runtime.sendMessage({ type, payload }, (response) => {
        if (chrome.runtime.lastError) return reject(new Error(chrome.runtime.lastError.message));
        if (!response.ok) return reject(new Error(response.error));
        resolve(response.data);
      });
    });
  }

  function closeModal() {
    document.querySelector(".zs-modal-overlay")?.remove();
  }

  function renderQr(container, text) {
    const img = document.createElement("img");
    img.className = "zs-qr";
    img.alt = "Lightning invoice QR code";
    img.src = `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=lightning:${encodeURIComponent(text)}`;
    container.appendChild(img);
  }

  async function handleAmountSubmit(target, amountSats, modalBody) {
    modalBody.innerHTML = `<p class="zs-status">Requesting invoice…</p>`;

    try {
      const result = await sendMessage("GET_INVOICE", {
        address: target.address,
        amountSats,
        comment: `Zap via Zapstar for ${target.displayName}`
      });

      modalBody.innerHTML = "";

      const title = document.createElement("p");
      title.className = "zs-status";
      title.textContent = `${amountSats.toLocaleString()} sats to ${target.displayName}`;
      modalBody.appendChild(title);

      renderQr(modalBody, result.invoice);

      const openWalletLink = document.createElement("a");
      openWalletLink.href = `lightning:${result.invoice}`;
      openWalletLink.className = "zs-open-wallet";
      openWalletLink.textContent = "Open in Wallet";
      modalBody.appendChild(openWalletLink);

      const copyBtn = document.createElement("button");
      copyBtn.className = "zs-copy-btn";
      copyBtn.textContent = "Copy Invoice";
      copyBtn.addEventListener("click", () => {
        navigator.clipboard.writeText(result.invoice);
        copyBtn.textContent = "Copied!";
        setTimeout(() => (copyBtn.textContent = "Copy Invoice"), 1500);
      });
      modalBody.appendChild(copyBtn);

      const note = document.createElement("p");
      note.className = "zs-note";
      note.textContent = "Scan or open in your Lightning wallet to complete the zap.";
      modalBody.appendChild(note);
    } catch (err) {
      modalBody.innerHTML = `<p class="zs-status zs-error">${err.message}</p>`;
    }
  }

  function openZapModal(target) {
    closeModal();

    const overlay = document.createElement("div");
    overlay.className = "zs-modal-overlay";
    overlay.addEventListener("click", (e) => {
      if (e.target === overlay) closeModal();
    });

    const modal = document.createElement("div");
    modal.className = "zs-modal";

    const header = document.createElement("div");
    header.className = "zs-modal-header";
    header.innerHTML = `<span>⚡ Zap ${target.displayName}</span>`;
    const closeBtn = document.createElement("button");
    closeBtn.className = "zs-close-btn";
    closeBtn.textContent = "×";
    closeBtn.addEventListener("click", closeModal);
    header.appendChild(closeBtn);
    modal.appendChild(header);

    const body = document.createElement("div");
    body.className = "zs-modal-body";

    const presetRow = document.createElement("div");
    presetRow.className = "zs-preset-row";
    for (const amount of PRESET_AMOUNTS) {
      const btn = document.createElement("button");
      btn.className = "zs-preset-btn";
      btn.textContent = `${amount.toLocaleString()} sats`;
      btn.addEventListener("click", () => handleAmountSubmit(target, amount, body));
      presetRow.appendChild(btn);
    }
    body.appendChild(presetRow);

    const customRow = document.createElement("div");
    customRow.className = "zs-custom-row";
    const input = document.createElement("input");
    input.type = "number";
    input.min = "1";
    input.placeholder = "Custom amount (sats)";
    const customBtn = document.createElement("button");
    customBtn.className = "zs-preset-btn";
    customBtn.textContent = "Zap";
    customBtn.addEventListener("click", () => {
      const value = parseInt(input.value, 10);
      if (value > 0) handleAmountSubmit(target, value, body);
    });
    customRow.appendChild(input);
    customRow.appendChild(customBtn);
    body.appendChild(customRow);

    modal.appendChild(body);
    overlay.appendChild(modal);
    document.body.appendChild(overlay);
  }

  window.Zapstar = { openZapModal };
})();