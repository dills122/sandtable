const commandDialog = document.querySelector("#command-palette");
const commandButton = document.querySelector("#open-command");
const commandInput = document.querySelector("#command-input");
const commandCount = document.querySelector("#command-count");
const commandLinks = [...document.querySelectorAll("[data-search]")];
const typedLine = document.querySelector("[data-type-line]");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

let activeIndex = 0;

function visibleCommands() {
  return commandLinks.filter((link) => !link.hidden);
}

function setActive(index) {
  const commands = visibleCommands();

  if (commands.length === 0) {
    activeIndex = 0;
    return;
  }

  activeIndex = (index + commands.length) % commands.length;
  commandLinks.forEach((link) => link.classList.remove("is-active"));
  commands[activeIndex].classList.add("is-active");
}

function filterCommands() {
  const query = commandInput.value.trim().toLocaleLowerCase();

  commandLinks.forEach((link) => {
    const label = link.textContent.toLocaleLowerCase();
    const keywords = link.dataset.search.toLocaleLowerCase();
    link.hidden = query.length > 0 && !`${label} ${keywords}`.includes(query);
  });

  const count = visibleCommands().length;
  commandCount.textContent = `${count} ${count === 1 ? "destination" : "destinations"}`;
  setActive(0);
}

function openCommands() {
  if (!commandDialog.open) {
    commandDialog.showModal();
  }

  commandInput.value = "";
  filterCommands();
  commandInput.focus({ preventScroll: true });
}

function closeCommands() {
  if (commandDialog.open) {
    commandDialog.close();
  }
}

commandButton.addEventListener("click", openCommands);
commandInput.addEventListener("input", filterCommands);

document.addEventListener("keydown", (event) => {
  const commandShortcut = (event.metaKey || event.ctrlKey) && event.key.toLocaleLowerCase() === "k";

  if (commandShortcut) {
    event.preventDefault();
    commandDialog.open ? closeCommands() : openCommands();
    return;
  }

  if (!commandDialog.open) {
    return;
  }

  if (event.key === "Escape") {
    event.preventDefault();
    closeCommands();
    return;
  }

  if (event.key === "ArrowDown") {
    event.preventDefault();
    setActive(activeIndex + 1);
  }

  if (event.key === "ArrowUp") {
    event.preventDefault();
    setActive(activeIndex - 1);
  }

  if (event.key === "Enter" && document.activeElement === commandInput) {
    const command = visibleCommands()[activeIndex];

    if (command) {
      event.preventDefault();
      command.click();
    }
  }
});

commandDialog.addEventListener("click", (event) => {
  if (event.target === commandDialog) {
    closeCommands();
  }
});

commandDialog.addEventListener("close", () => {
  commandButton.focus({ preventScroll: true });
});

commandLinks.forEach((link) => {
  link.addEventListener("click", () => closeCommands());
});

function typeVerified() {
  const text = "verified";

  if (reducedMotion.matches) {
    typedLine.textContent = text;
    return;
  }

  typedLine.textContent = "";
  let cursor = 0;

  const tick = window.setInterval(() => {
    cursor += 1;
    typedLine.textContent = text.slice(0, cursor);

    if (cursor >= text.length) {
      window.clearInterval(tick);
    }
  }, 90);
}

typeVerified();
