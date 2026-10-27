document.addEventListener('DOMContentLoaded', function () {

    // Cập nhật số lượng badge
    function updateBadge(count) {
        var badge = document.getElementById('cart-count');
        if (badge) badge.textContent = count;
    }

    // Hiển thị toast
    function showToast(msg, type) {
        const t = document.createElement('div');
        t.className = 'toast' + (type ? ' toast-' + type : '');
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(() => t.classList.add('show'), 10);
        setTimeout(() => {
            t.classList.remove('show');
            setTimeout(() => t.remove(), 300);
        }, 2000);
    }

    // Hộp thoại xác nhận
    function showConfirm(msg, onYes) {
        if (window.confirm(msg)) {
            onYes();
        }
    }

    // Nút thêm vào giỏ
    document.querySelectorAll('.btn-add-cart').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.preventDefault();
            const productId = this.dataset.productId;
            const qtyInput = document.getElementById('productQty');
            const quantity = qtyInput ? parseInt(qtyInput.value) : 1;

            try {
                const res = await fetch('/Cart/Add', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `productId=${productId}&quantity=${quantity}`
                });

                const data = await res.json();
                if (data.success) {
                    updateBadge(data.cartCount);
                    showToast('Đã thêm vào giỏ hàng thành công', 'success');
                } else {
                    showToast(data.message || 'Không thể thêm vào giỏ hàng', 'error');
                }
            } catch (err) {
                showToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
            }
        });
    });

    // Tăng số lượng
    document.querySelectorAll('.qty-plus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            input.value = parseInt(input.value) + 1;
            input.dispatchEvent(new Event('change'));
        });
    });

    // Giảm số lượng
    document.querySelectorAll('.qty-minus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            // Cho phép giảm xuống 0 để kích hoạt hỏi xóa
            if (parseInt(input.value) > 0) {
                input.value = parseInt(input.value) - 1;
                input.dispatchEvent(new Event('change'));
            }
        });
    });

    // Cập nhật số lượng khi input thay đổi
    document.querySelectorAll('.qty-input').forEach(input => {
        input.addEventListener('change', async function () {
            const row = this.closest('tr');
            const productId = row.dataset.productId;
            let quantity = parseInt(this.value);

            // Xử lý khi số lượng <= 0
            if (quantity <= 0) {
                showConfirm('Bạn có chắc muốn xoá sản phẩm này khỏi giỏ hàng không?', async () => {
                    try {
                        const res = await fetch('/Cart/Remove', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/x-www-form-urlencoded',
                                'X-Requested-With': 'XMLHttpRequest'
                            },
                            body: `productId=${productId}`
                        });
                        const data = await res.json();
                        if (data.success) {
                            row.remove();
                            if (data.totalAmount !== undefined) {
                                document.getElementById('cartTotal').textContent = data.totalAmount.toLocaleString('vi-VN') + ' ₫';
                            }
                            if (data.totalQuantity !== undefined) {
                                updateBadge(data.totalQuantity);
                            }
                            showToast('Đã xóa sản phẩm', 'success');
                            if (data.totalQuantity === 0) location.reload();
                        }
                    } catch (err) {
                        showToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
                    }
                });
                
                // Nếu người dùng ấn Cancel (Hủy) trong hộp thoại confirm, trả lại số lượng 1
                // (Lưu ý: hàm confirm mặc định của trình duyệt là blocking, nên ta set lại sau khi showConfirm chạy xong)
                this.value = 1; 
                return;
            }

            // Nếu số lượng > 0, cập nhật bình thường
            try {
                const res = await fetch('/Cart/UpdateQuantity', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `productId=${productId}&quantity=${quantity}`
                });

                const data = await res.json();
                if (data.success) {
                    if (data.itemSubTotal !== undefined) {
                        row.querySelector('.cart-subtotal').textContent =
                            data.itemSubTotal.toLocaleString('vi-VN') + ' ₫';
                    }
                    if (data.totalAmount !== undefined) {
                        document.getElementById('cartTotal').textContent =
                            data.totalAmount.toLocaleString('vi-VN') + ' ₫';
                    }
                    if (data.totalQuantity !== undefined) {
                        updateBadge(data.totalQuantity);
                    }
                }
            } catch (err) {
                showToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
            }
        });
    });

    // Xóa sản phẩm (bằng nút X)
    document.querySelectorAll('.btn-remove').forEach(btn => {
        btn.addEventListener('click', function () {
            const row = this.closest('tr');
            const productId = row.dataset.productId;

            showConfirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ?', async () => {
                try {
                    const res = await fetch('/Cart/Remove', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'X-Requested-With': 'XMLHttpRequest'
                        },
                        body: `productId=${productId}`
                    });

                    const data = await res.json();
                    if (data.success) {
                        row.remove();
                        if (data.totalAmount !== undefined) {
                            document.getElementById('cartTotal').textContent =
                                data.totalAmount.toLocaleString('vi-VN') + ' ₫';
                        }
                        if (data.totalQuantity !== undefined) {
                            updateBadge(data.totalQuantity);
                        }
                        showToast('Đã xóa sản phẩm', 'success');
                        if (data.totalQuantity === 0) location.reload();
                    } else {
                        showToast(data.message || 'Không thể xóa sản phẩm', 'error');
                    }
                } catch (err) {
                    showToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
                }
            });
        });
    });
});