
    const API_URL = "http://localhost:5237";

    const questionInput = document.getElementById("questionInput");
    const askButton = document.getElementById("askButton");
    const answerBox = document.getElementById("answerBox");

    async function askQuestion() {
    const question = questionInput.value.trim();

    if (question === "") {
    answerBox.style.display = "block";
    answerBox.textContent = "Введите вопрос.";
    return;
}

    try {
    const response = await fetch(`${API_URL}/ask`, {
    method: "POST",
    headers: {
    "Content-Type": "application/json"
},
    body: JSON.stringify({
    question: question
})
});

    const answer = await response.text();

    answerBox.style.display = "block";
    answerBox.textContent = answer;
} catch (error) {
    answerBox.style.display = "block";
    answerBox.textContent = "Не удалось подключиться к серверу. Проверь, запущен ли бэкенд.";
    console.error(error);
}
}

    askButton.addEventListener("click", askQuestion);

    questionInput.addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
    askQuestion();
}
});
