(function () {
  "use strict";

  const app = document.getElementById("app");
  if (!app) return;

  const localeSel = document.getElementById("locale");
  const seedInput = document.getElementById("seed");
  const seedRandomBtn = document.getElementById("seed-random");
  const likesInput = document.getElementById("likes");
  const likesOut = document.getElementById("likes-out");
  const tableBtn = document.getElementById("view-table");
  const galleryBtn = document.getElementById("view-gallery");
  const tableView = document.getElementById("table-view");
  const galleryView = document.getElementById("gallery-view");
  const tableBody = document.getElementById("table-body");
  const prevBtn = document.getElementById("prev-page");
  const nextBtn = document.getElementById("next-page");
  const pageLabel = document.getElementById("page-label");
  const galleryGrid = document.getElementById("gallery-grid");
  const sentinel = document.getElementById("gallery-sentinel");
  const statusEl = document.getElementById("status");

  const state = { locale: "", seed: "", likes: 5, view: "table" };
  const table = { page: 0, pageSize: 20 };
  const gallery = { page: 0, pageSize: 20, loading: false };

  const PLACEHOLDER =
    "data:image/svg+xml," +
    encodeURIComponent(
      "<svg xmlns='http://www.w3.org/2000/svg' width='300' height='300'>" +
        "<rect width='300' height='300' fill='#1f2430'/>" +
        "<circle cx='150' cy='150' r='118' fill='#11141c' stroke='#3a4256' stroke-width='2'/>" +
        "<circle cx='150' cy='150' r='80' fill='none' stroke='#2a3142'/>" +
        "<circle cx='150' cy='150' r='54' fill='none' stroke='#2a3142'/>" +
        "<circle cx='150' cy='150' r='18' fill='#b5446e'/>" +
        "<circle cx='150' cy='150' r='5' fill='#11141c'/></svg>",
    );

  const enc = encodeURIComponent;

  function debounce(fn, ms) {
    let t;
    return function () {
      clearTimeout(t);
      t = setTimeout(fn, ms);
    };
  }

  function randomSeed() {
    const b = new Uint32Array(2);
    crypto.getRandomValues(b);
    return ((BigInt(b[0]) << 32n) | BigInt(b[1])).toString();
  }

  function buildUrl(page) {
    return (
      "/api/songs?locale=" +
      enc(state.locale) +
      "&seed=" +
      enc(state.seed) +
      "&page=" +
      page +
      "&likes=" +
      state.likes
    );
  }

  async function fetchBatch(page) {
    const res = await fetch(buildUrl(page));
    if (!res.ok) throw new Error("Request failed: " + res.status);
    return res.json();
  }

  function esc(s) {
    const d = document.createElement("div");
    d.textContent = s == null ? "" : String(s);
    return d.innerHTML;
  }

  function coverError(e) {
    const img = e.target;
    if (
      img.tagName === "IMG" &&
      img.classList.contains("cover") &&
      img.src !== PLACEHOLDER
    ) {
      img.src = PLACEHOLDER;
    }
  }
  tableBody.addEventListener("error", coverError, true);
  galleryGrid.addEventListener("error", coverError, true);

  async function loadTable() {
    statusEl.textContent = "";
    try {
      const data = await fetchBatch(table.page);
      table.pageSize = data.pageSize;
      renderTableRows(data.items);
      pageLabel.textContent = "Page " + (table.page + 1);
      prevBtn.disabled = table.page === 0;
    } catch (err) {
      tableBody.innerHTML = "";
      statusEl.textContent =
        "Couldn't load songs. Check that the server is running, then try again.";
    }
  }

  function renderTableRows(items) {
    tableBody.innerHTML = items
      .map(function (it) {
        const detailId = "detail-" + it.index;
        return (
          '<tr class="song-row" data-target="' +
          detailId +
          '" tabindex="0" role="button" aria-expanded="false">' +
          '<td class="text-muted">' +
          it.index +
          "</td>" +
          '<td class="fw-semibold">' +
          esc(it.title) +
          "</td>" +
          "<td>" +
          esc(it.artist) +
          "</td>" +
          "<td>" +
          esc(it.album) +
          "</td>" +
          '<td><span class="badge text-bg-light">' +
          esc(it.genre) +
          "</span></td>" +
          '<td><span class="likes">&hearts; ' +
          it.likes +
          "</span></td>" +
          "</tr>" +
          '<tr id="' +
          detailId +
          '" class="detail-row d-none">' +
          '<td colspan="6">' +
          '<div class="detail d-flex flex-wrap gap-3">' +
          '<img class="cover" src="' +
          esc(it.coverUrl) +
          '" alt="Cover for ' +
          esc(it.title) +
          '" loading="lazy">' +
          '<div class="detail-body flex-grow-1">' +
          '<div class="detail-title">' +
          esc(it.title) +
          ' &mdash; <span class="text-muted">' +
          esc(it.artist) +
          "</span></div>" +
          '<audio class="w-100 my-2" controls preload="none" src="' +
          esc(it.audioUrl) +
          '"></audio>' +
          '<p class="review mb-0">' +
          esc(it.review) +
          "</p>" +
          "</div>" +
          "</div>" +
          "</td>" +
          "</tr>"
        );
      })
      .join("");
  }

  function toggleDetail(row) {
    const detail = document.getElementById(row.dataset.target);
    if (!detail) return;
    const open = !detail.classList.toggle("d-none");
    row.setAttribute("aria-expanded", String(open));
    row.classList.toggle("expanded", open);
  }

  tableBody.addEventListener("click", function (e) {
    const row = e.target.closest(".song-row");
    if (row) toggleDetail(row);
  });
  tableBody.addEventListener("keydown", function (e) {
    if (e.key !== "Enter" && e.key !== " ") return;
    const row = e.target.closest(".song-row");
    if (row) {
      e.preventDefault();
      toggleDetail(row);
    }
  });

  function resetGallery() {
    gallery.page = 0;
    gallery.loading = false;
    galleryGrid.innerHTML = "";
  }

  async function loadMoreGallery() {
    if (gallery.loading) return;
    gallery.loading = true;
    try {
      const data = await fetchBatch(gallery.page);
      gallery.pageSize = data.pageSize;
      appendGalleryCards(data.items);
      gallery.page += 1;
    } catch (err) {
      statusEl.textContent = "Couldn't load more songs.";
    } finally {
      gallery.loading = false;
    }
  }

  function appendGalleryCards(items) {
    const html = items
      .map(function (it) {
        return (
          '<div class="col">' +
          '<div class="card h-100 song-card">' +
          '<div class="cover-wrap">' +
          '<img class="card-img-top cover" src="' +
          esc(it.coverUrl) +
          '" alt="Cover for ' +
          esc(it.title) +
          '" loading="lazy">' +
          '<span class="idx">#' +
          it.index +
          "</span>" +
          "</div>" +
          '<div class="card-body">' +
          '<div class="card-title fw-semibold mb-1">' +
          esc(it.title) +
          "</div>" +
          '<div class="text-muted small mb-1">' +
          esc(it.artist) +
          "</div>" +
          '<div class="small mb-2">' +
          esc(it.album) +
          ' &middot; <span class="badge text-bg-light">' +
          esc(it.genre) +
          "</span></div>" +
          '<audio class="w-100" controls preload="none" src="' +
          esc(it.audioUrl) +
          '"></audio>' +
          "</div>" +
          '<div class="card-footer"><span class="likes">&hearts; ' +
          it.likes +
          "</span></div>" +
          "</div>" +
          "</div>"
        );
      })
      .join("");
    galleryGrid.insertAdjacentHTML("beforeend", html);
  }

  const observer = new IntersectionObserver(
    function (entries) {
      if (state.view !== "gallery") return;
      if (
        entries.some(function (e) {
          return e.isIntersecting;
        })
      )
        loadMoreGallery();
    },
    { rootMargin: "300px" },
  );
  observer.observe(sentinel);

  function showView(view) {
    state.view = view;
    const isTable = view === "table";
    tableView.classList.toggle("d-none", !isTable);
    galleryView.classList.toggle("d-none", isTable);
    tableBtn.classList.toggle("active", isTable);
    galleryBtn.classList.toggle("active", !isTable);
    tableBtn.setAttribute("aria-pressed", String(isTable));
    galleryBtn.setAttribute("aria-pressed", String(!isTable));

    if (isTable) {
      if (!tableBody.children.length) loadTable();
    } else {
      if (!galleryGrid.children.length) loadMoreGallery();
    }
  }

  function onParamsChanged() {
    statusEl.textContent = "";
    table.page = 0;
    tableBody.innerHTML = "";
    resetGallery();
    window.scrollTo({ top: 0 });
    if (state.view === "table") loadTable();
    else loadMoreGallery();
  }

  localeSel.addEventListener("change", function () {
    state.locale = localeSel.value;
    onParamsChanged();
  });

  const seedChanged = debounce(function () {
    state.seed = seedInput.value.trim();
    onParamsChanged();
  }, 300);
  seedInput.addEventListener("input", seedChanged);

  seedRandomBtn.addEventListener("click", function () {
    seedInput.value = randomSeed();
    state.seed = seedInput.value;
    onParamsChanged();
  });

  const likesChanged = debounce(function () {
    state.likes = parseFloat(likesInput.value);
    onParamsChanged();
  }, 200);
  likesInput.addEventListener("input", function () {
    likesOut.textContent = parseFloat(likesInput.value).toFixed(1);
    likesChanged();
  });

  tableBtn.addEventListener("click", function () {
    showView("table");
  });
  galleryBtn.addEventListener("click", function () {
    showView("gallery");
  });

  prevBtn.addEventListener("click", function () {
    if (table.page > 0) {
      table.page -= 1;
      loadTable();
      window.scrollTo({ top: 0 });
    }
  });
  nextBtn.addEventListener("click", function () {
    table.page += 1;
    loadTable();
    window.scrollTo({ top: 0 });
  });

  async function init() {
    try {
      const res = await fetch("/api/locales");
      const locales = await res.json();
      localeSel.innerHTML = locales
        .map(function (l) {
          return (
            '<option value="' +
            esc(l.code) +
            '">' +
            esc(l.display) +
            "</option>"
          );
        })
        .join("");
      state.locale = locales.length ? locales[0].code : "en-US";
      localeSel.value = state.locale;
    } catch (err) {
      state.locale = "en-US";
    }

    state.seed = randomSeed();
    seedInput.value = state.seed;

    state.likes = parseFloat(likesInput.value);
    likesOut.textContent = state.likes.toFixed(1);

    showView("table");
  }

  init();
})();
