document.querySelectorAll(".translate-btn").forEach(button => {
    button.addEventListener("click", async function () {
        const row = this.closest("tr");

        if (!row) {
            return;
        }

        const text = row.querySelector(".dish-name")?.innerText.trim();
        const language = row.querySelector(".language-select")?.value;
        const output = row.querySelector(".translated-text");

        const restaurantId = row.dataset.restaurantId;
        const menuId = row.dataset.menuId;
        const menuItemId = row.dataset.menuItemId;

        if (!text || !language || !output) {
            return;
        }

        output.textContent = "Translating...";

        try {
            const response = await fetch("/Menu/TranslateItem", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    restaurantId,
                    menuId,
                    menuItemId,
                    text,
                    targetLanguage: language
                })
            });

            let data;

            try {
                data = await response.json();
            } catch {
                output.textContent = "Translation failed.";
                return;
            }

            if (!response.ok) {
                output.textContent = data.error || "Translation failed.";
                return;
            }

            output.textContent = data.translatedText;
        } catch (error) {
            output.textContent = "Translation failed.";
            console.error(error);
        }
    });
});