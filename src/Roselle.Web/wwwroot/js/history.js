let history = JSON.parse(localStorage.getItem("orderHistory")) || []

let container = document.getElementById("history-list")

if(history.length === 0){

container.innerHTML = "Bạn chưa có đơn hàng nào"

}

history.slice().reverse().forEach((order,index)=>{

let div = document.createElement("div")

div.className = "order-card"

let itemsHTML=""

order.items.forEach(i=>{

itemsHTML+=`

<div class="order-item">

<img src="${i.image}">

<div class="order-name">
${i.name} x${i.qty}
</div>

<div class="order-price">
${(i.price*i.qty).toLocaleString()} ₫
</div>

</div>

`

})

div.innerHTML=`

<div class="order-date">
Ngày đặt: ${order.date}
</div>

${itemsHTML}

<div class="order-footer">

<div>
Thanh toán: ${order.pay}<br>
<b>Tổng: ${order.total.toLocaleString()} ₫</b>
</div>

<button class="buy-again" onclick="buyAgain(${index})">
Mua lại
</button>

</div>

`

container.appendChild(div)

})

function buyAgain(index){

let history = JSON.parse(localStorage.getItem("orderHistory")) || []
let cart = JSON.parse(localStorage.getItem("cart")) || []

let order = history[index]

order.items.forEach(item=>{

let exist = cart.find(c => c.name === item.name)

if(exist){

exist.qty += item.qty

}else{

cart.push({...item})

}

})

localStorage.setItem("cart",JSON.stringify(cart))

alert("Đã thêm sản phẩm vào giỏ hàng")

window.location.href="cart.html"

}