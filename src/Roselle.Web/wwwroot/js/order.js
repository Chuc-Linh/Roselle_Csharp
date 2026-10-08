let orders = JSON.parse(localStorage.getItem("orderItems")) || []
let list = document.getElementById("order-list")

let total = 0

orders.forEach(p=>{

let div=document.createElement("div")
div.className="order-item"

div.innerHTML=`

<img src="${p.image}">

<div class="order-info">

<div>${p.name}</div>

<div>Số lượng: ${p.qty}</div>

</div>

<div class="order-price">
${(p.price*p.qty).toLocaleString()} ₫
</div>

`

list.appendChild(div)

total+=p.price*p.qty

})

document.getElementById("order-total").innerText =
total.toLocaleString()+" ₫"



/* lấy thông tin user */

let username = localStorage.getItem("loginUser")
let users = JSON.parse(localStorage.getItem("users")) || []

let user = users.find(u => u.username === username)

if(user){

document.getElementById("account-info").innerHTML = `

<p><b>Họ tên:</b> ${user.name}</p>
<p><b>SĐT:</b> ${user.phone}</p>
<p><b>Địa chỉ:</b> ${user.address}</p>

`

}



/* toggle nhập thông tin */

function toggleInfo(){

let type=document.querySelector('input[name="infoType"]:checked').value

if(type==="account"){

document.getElementById("other-info").style.display="none"

}else{

document.getElementById("other-info").style.display="block"

}

}



/* toggle bank */

function toggleBank(){

let pay=document.querySelector('input[name="pay"]:checked').value

document.getElementById("bank-info").style.display =
pay==="BANK" ? "block" : "none"

}



/* đặt hàng */

function placeOrder(){

let infoType=document.querySelector('input[name="infoType"]:checked').value

let name,phone,address

if(infoType==="account"){

name = user?.name
phone = user?.phone
address = user?.address

}else{

name=document.getElementById("name").value
phone=document.getElementById("phone").value
address=document.getElementById("address").value

}

let pay=document.querySelector('input[name="pay"]:checked').value

if(!name || !phone || !address){

alert("Vui lòng nhập đầy đủ thông tin")
return

}

let history = JSON.parse(localStorage.getItem("orderHistory")) || []

let order={

date:new Date().toLocaleString(),
name:name,
phone:phone,
address:address,
pay:pay,
items:orders,
total:total

}

history.push(order)

localStorage.setItem("orderHistory",JSON.stringify(history))

/* xóa sản phẩm đã mua khỏi cart */

let cart = JSON.parse(localStorage.getItem("cart")) || []

orders.forEach(orderItem=>{

cart = cart.filter(cartItem => cartItem.name !== orderItem.name)

})

localStorage.setItem("cart",JSON.stringify(cart))

localStorage.removeItem("orderItems")

alert("Đặt hàng thành công!")

window.location.href="history.html"

if(typeof updateCartCount === "function"){
updateCartCount()
}
}