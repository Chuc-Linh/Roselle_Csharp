// page-specific JS for Hoa Cẩm Tú: cập nhật năm và phân trang đơn giản

document.addEventListener('DOMContentLoaded', function () {
  // cập nhật năm footer
  const yearEl = document.getElementById('site-year');
  if (yearEl) yearEl.textContent = new Date().getFullYear();

  // pagination: show only cards matching data-page
  const cards = Array.from(document.querySelectorAll('.baby-card'));
  const pageItems = Array.from(document.querySelectorAll('.baby-pagination .page'));
  const prevBtn = document.querySelector('.baby-pagination .prev');
  const nextBtn = document.querySelector('.baby-pagination .next');

  if (!cards.length || !pageItems.length || !prevBtn || !nextBtn) return;

  const pagesAvailable = pageItems.map(p => Number(p.dataset.page) || 1);
  const maxPage = Math.max(...pagesAvailable);
  let current = 1;

  function showPage(n) {
    if (n < 1) n = 1;
    if (n > maxPage) n = maxPage;
    current = n;
    cards.forEach(c => {
      const p = Number(c.dataset.page || 1);
      c.style.display = (p === n) ? '' : 'none';
    });
    pageItems.forEach(p => p.classList.toggle('active', Number(p.dataset.page) === n));
    prevBtn.disabled = (n === 1);
    nextBtn.disabled = (n === maxPage);
  }

  // attach events to page buttons (the li.page wraps a button)
  pageItems.forEach(item => {
    const btn = item.querySelector('button');
    if (btn) btn.addEventListener('click', () => showPage(Number(item.dataset.page)));
    // allow clicking the li itself
    item.addEventListener('click', () => {
      const p = Number(item.dataset.page);
      if (p) showPage(p);
    });
  });

  prevBtn.addEventListener('click', () => showPage(current - 1));
  nextBtn.addEventListener('click', () => showPage(current + 1));

  // initial render
  showPage(1);
});