function login(){

let username=document.getElementById("username").value
let password=document.getElementById("password").value

let users=JSON.parse(localStorage.getItem("users"))

if(!users){
alert("Chưa có tài khoản nào đăng ký!")
return
}

for(let i=0;i<users.length;i++){

if(users[i].username===username && users[i].password===password){

localStorage.setItem("loginUser",users[i].fullname)

alert("Đăng nhập thành công")

window.location.href="../index.html"

return

}

}

alert("Sai tên đăng nhập hoặc mật khẩu")

}



function checkLogin(){

let username = document.getElementById("username").value
let password = document.getElementById("password").value

let btn = document.getElementById("loginBtn")

if(username !== "" && password !== ""){
btn.disabled = false
}else{
btn.disabled = true
}

}