const cards = document.querySelectorAll(".sun-card")

const perPage = 6
let currentPage = 1

const pagesContainer = document.querySelector(".pages")
const prevBtn = document.querySelector(".prev")
const nextBtn = document.querySelector(".next")

const totalPages = Math.ceil(cards.length / perPage)

function showPage(page){

currentPage = page

let start = (page-1)*perPage
let end = start + perPage

cards.forEach((card,i)=>{
card.style.display = (i>=start && i<end) ? "block" : "none"
})

renderPages()

}

function renderPages(){

pagesContainer.innerHTML = ""

for(let i=1;i<=totalPages;i++){

let li = document.createElement("li")

li.className = "page"

if(i===currentPage){
li.classList.add("active")
}

li.innerHTML = `<button>${i}</button>`

li.onclick = ()=> showPage(i)

pagesContainer.appendChild(li)

}

prevBtn.disabled = currentPage===1
nextBtn.disabled = currentPage===totalPages

}

prevBtn.onclick = ()=>{ if(currentPage>1) showPage(currentPage-1) }
nextBtn.onclick = ()=>{ if(currentPage<totalPages) showPage(currentPage+1) }

showPage(1)