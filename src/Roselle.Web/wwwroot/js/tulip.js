document.addEventListener('DOMContentLoaded', function () {

  const yearEl = document.getElementById('site-year');
  if (yearEl) yearEl.textContent = new Date().getFullYear();

  const cards = Array.from(document.querySelectorAll('.tl-card'));
  const pageItems = Array.from(document.querySelectorAll('.tl-pagination .page'));
  const prevBtn = document.querySelector('.tl-pagination .prev');
  const nextBtn = document.querySelector('.tl-pagination .next');

  if (!cards.length) return;

  const pagesAvailable = pageItems.map(p => Number(p.dataset.page) || 1);
  const maxPage = Math.max(...pagesAvailable);
  let current = 1;

  function showPage(n){

    if(n < 1) n = 1;
    if(n > maxPage) n = maxPage;

    current = n;

    cards.forEach(c=>{
      const p = Number(c.dataset.page || 1);
      c.style.display = (p === n) ? '' : 'none';
    });

    pageItems.forEach(p=>{
      p.classList.toggle('active', Number(p.dataset.page) === n);
    });

    if(prevBtn) prevBtn.disabled = (n===1);
    if(nextBtn) nextBtn.disabled = (n===maxPage);
  }

  pageItems.forEach(item=>{
    const btn = item.querySelector('button');

    if(btn){
      btn.addEventListener('click',()=>{
        showPage(Number(item.dataset.page));
      });
    }

    item.addEventListener('click',()=>{
      showPage(Number(item.dataset.page));
    });

  });

  if(prevBtn){
    prevBtn.addEventListener('click',()=>showPage(current-1));
  }

  if(nextBtn){
    nextBtn.addEventListener('click',()=>showPage(current+1));
  }

  showPage(1);

});