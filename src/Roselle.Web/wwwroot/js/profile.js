let username = localStorage.getItem("loginUser")

if(!username){

alert("Vui lòng đăng nhập")
window.location.href="/Account/Login"

}

let users = JSON.parse(localStorage.getItem("users")) || []

let user = users.find(u => u.username === username)

if(user){

document.getElementById("email").value = user.email || ""
document.getElementById("name").value = user.name || ""
document.getElementById("phone").value = user.phone || ""
document.getElementById("address").value = user.address || ""

}

function saveProfile(){

let name = document.getElementById("name").value
let phone = document.getElementById("phone").value
let address = document.getElementById("address").value

user.name = name
user.phone = phone
user.address = address

localStorage.setItem("users",JSON.stringify(users))

alert("Cập nhật thông tin thành công")

}