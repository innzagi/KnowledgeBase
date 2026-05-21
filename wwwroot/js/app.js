const API_URL = "http://localhost:5237";

document.addEventListener("DOMContentLoaded", async () => {
    await loadHomePage();
    await loadAssistant();
});

async function loadComponent(containerId, path) {
    const container = document.getElementById(containerId);

    if (!container) {
        console.error(`Контейнер ${containerId} не найден`);
        return;
    }

    const response = await fetch(path);

    if (!response.ok) {
        container.innerHTML = `<p>Не удалось загрузить компонент: ${path}</p>`;
        return;
    }

    container.innerHTML = await response.text();
}

async function loadHomePage() {
    await loadComponent("pageContent", "/components/home.html");
    initHomeEvents();
}

async function loadBotanyPage() {
    await loadComponent("pageContent", "/components/botany.html");
    initBotanyEvents();
}

async function loadAssistant() {
    await loadComponent("assistantContainer", "/components/assistant.html");
    initAssistantEvents();
}

function initHomeEvents() {
    const botanyCard = document.querySelector('[data-section="botany"]');

    if (botanyCard) {
        botanyCard.addEventListener("click", loadBotanyPage);
    }
}

function initBotanyEvents() {
    const backButton = document.getElementById("backToSections");

    if (backButton) {
        backButton.addEventListener("click", loadHomePage);
    }

    document.querySelectorAll("[data-article]").forEach(card => {
        card.addEventListener("click", () => {
            loadArticle(card.dataset.article);
        });
    });
}

async function loadArticle(articlePath) {
    const response = await fetch(articlePath);

    if (!response.ok) {
        document.getElementById("pageContent").innerHTML = `
            <article class="article-page">
                <button class="back-button" id="backToBotany" type="button">
                    ← Назад к ботанике
                </button>
                <p>Статья не найдена: ${articlePath}</p>
            </article>
        `;

        document.getElementById("backToBotany").addEventListener("click", loadBotanyPage);
        return;
    }

    const markdown = await response.text();

    document.getElementById("pageContent").innerHTML = `
        <article class="article-page">
            <button class="back-button" id="backToBotany" type="button">
                ← Назад к ботанике
            </button>

<div class="article-content">
    <div class="article-columns">
        ${markdownToHtml(markdown)}
    </div>
</div>
        </article>
    `;

    document.getElementById("backToBotany").addEventListener("click", loadBotanyPage);
}

function markdownToHtml(markdown) {
    return marked.parse(markdown);
}

function initAssistantEvents() {
    const questionInput = document.getElementById("questionInput");
    const askButton = document.getElementById("askButton");
    const answerBox = document.getElementById("answerBox");

    if (!questionInput || !askButton || !answerBox) {
        return;
    }

    async function askQuestion() {
        const question = questionInput.value.trim();

        if (question === "") {
            showAnswer("Введите вопрос.");
            return;
        }

        try {
            const response = await fetch(`${API_URL}/ask`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ question })
            });

            const answer = await response.text();
            showAnswer(answer);
        } catch {
            showAnswer("Не удалось подключиться к серверу.");
        }
    }

    function showAnswer(text) {
        answerBox.style.display = "block";
        answerBox.textContent = text;
    }

    askButton.addEventListener("click", askQuestion);

    questionInput.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            askQuestion();
        }
    });
}