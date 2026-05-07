document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("uploadForm");

    if (!form) {
        return;
    }

    form.addEventListener("submit", async function (e) {
        e.preventDefault();
        await uploadFiles();
    });
});

async function uploadFiles() {
    const restaurantNameInput = document.getElementById("restaurantName");
    const fileInput = document.getElementById("fileInput");
    const container = document.getElementById("progressContainer");
    const submitButton = document.querySelector('#uploadForm button[type="submit"]');

    const restaurantName = restaurantNameInput.value.trim();
    const files = fileInput.files;

    container.innerHTML = "";

    if (!restaurantName) {
        alert("Restaurant name is required.");
        return;
    }

    if (!files || files.length === 0) {
        alert("Please select at least one file.");
        return;
    }

    if (submitButton) {
        submitButton.disabled = true;
    }

    let restaurantId;
    let menuId;

    try {
        const startResponse = await fetch("/Menu/StartUpload", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
            },
            body: new URLSearchParams({
                restaurantName: restaurantName
            })
        });

        if (!startResponse.ok) {
            const errorText = await startResponse.text();
            alert("Failed to initialize upload: " + errorText);

            if (submitButton) {
                submitButton.disabled = false;
            }

            return;
        }

        const data = await startResponse.json();
        restaurantId = data.restaurantId;
        menuId = data.menuId;
    } catch (error) {
        alert("Failed to initialize upload.");

        if (submitButton) {
            submitButton.disabled = false;
        }

        return;
    }

    let completedCount = 0;

    for (const file of files) {
        const bar = createProgressBar(file.name);
        container.appendChild(bar.element);

        const formData = new FormData();
        formData.append("restaurantId", restaurantId);
        formData.append("menuId", menuId);
        formData.append("file", file);

        const xhr = new XMLHttpRequest();

        xhr.upload.addEventListener("progress", (e) => {
            if (e.lengthComputable) {
                const pct = Math.round((e.loaded / e.total) * 100);
                bar.fill.style.width = pct + "%";
                bar.label.textContent = pct + "%";
            }
        });

        xhr.addEventListener("load", () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                bar.fill.style.width = "100%";
                bar.label.textContent = "✓ Done";
                bar.fill.style.background = "#22c55e";
            } else {
                bar.label.textContent = "✗ Failed";
                bar.fill.style.background = "#ef4444";
            }

            completedCount++;

            if (completedCount === files.length && submitButton) {
                submitButton.disabled = false;
            }
        });

        xhr.addEventListener("error", () => {
            bar.label.textContent = "✗ Failed";
            bar.fill.style.background = "#ef4444";

            completedCount++;

            if (completedCount === files.length && submitButton) {
                submitButton.disabled = false;
            }
        });

        xhr.open("POST", "/Menu/UploadSingle");
        xhr.send(formData);
    }
}

function createProgressBar(name) {
    const el = document.createElement("div");
    el.className = "upload-item";
    el.style.marginBottom = "12px";

    el.innerHTML = `
        <div style="margin-bottom:4px;">${escapeHtml(name)}</div>
        <div style="width:300px;height:20px;background:#e5e7eb;border-radius:8px;overflow:hidden;display:inline-block;vertical-align:middle;">
            <div class="progress-fill" style="height:100%;width:0%;background:#3b82f6;transition:width 0.2s;"></div>
        </div>
        <span class="progress-pct" style="margin-left:10px;">0%</span>
    `;

    return {
        element: el,
        fill: el.querySelector(".progress-fill"),
        label: el.querySelector(".progress-pct")
    };
}

function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}