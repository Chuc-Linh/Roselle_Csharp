function register(){

let username=document.getElementById("username").value
let fullname=document.getElementById("fullname").value
let phone=document.getElementById("phone").value
let address=document.getElementById("address").value
let email=document.getElementById("email").value
let password=document.getElementById("password").value
let confirm=document.getElementById("confirm").value

// kiểm tra mật khẩu
if(password!==confirm){
alert("Mật khẩu xác nhận không khớp!")
return
}

// lấy danh sách user
let users=JSON.parse(localStorage.getItem("users")) || []

// kiểm tra trùng
for(let i=0;i<users.length;i++){

if(users[i].username===username){
alert("Tên người dùng đã tồn tại!")
return
}

if(users[i].email===email){
alert("Email đã được sử dụng!")
return
}

if(users[i].phone===phone){
alert("Số điện thoại đã được sử dụng!")
return
}

}

// tạo user mới
let newUser = {

username: username,
email: email,
password: password,
name: fullname,
phone: phone,
address: address

}

// thêm vào danh sách
users.push(newUser)

// lưu lại
localStorage.setItem("users",JSON.stringify(users))

// đăng nhập luôn
localStorage.setItem("loginUser",username)

alert("Đăng ký thành công!")

// quay về trang chủ
window.location.href="../index.html"

}

function checkRegister(){

let username=document.getElementById("username").value
let fullname=document.getElementById("fullname").value
let phone=document.getElementById("phone").value
let address=document.getElementById("address").value
let email=document.getElementById("email").value
let password=document.getElementById("password").value
let confirm=document.getElementById("confirm").value

let btn=document.getElementById("registerBtn")

if(
username!=="" &&
fullname!=="" &&
phone!=="" &&
address!=="" &&
email!=="" &&
password!=="" &&
confirm!==""
){
btn.disabled=false
}else{
btn.disabled=true
}

}