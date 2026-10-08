document.addEventListener('DOMContentLoaded', function () {

    // Cập nhật số lượng badge
    function updateBadge(count) {
        var badge = document.getElementById('cart-count');
        if (badge) badge.textContent = count;
    }

    // Nút thêm vào giỏ
    document.querySelectorAll('.btn-add-cart').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.preventDefault();
            const productId = this.dataset.productId;
            const qtyInput = document.getElementById('productQty');
            const quantity = qtyInput ? parseInt(qtyInput.value) : 1;

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
                showToast('Đã thêm vào giỏ hàng 🌸');
            }
        });
    });

    // Tăng/giảm số lượng trong giỏ
    document.querySelectorAll('.qty-plus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            input.value = parseInt(input.value) + 1;
            input.dispatchEvent(new Event('change'));
        });
    });

    document.querySelectorAll('.qty-minus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            if (parseInt(input.value) > 1) {
                input.value = parseInt(input.value) - 1;
                input.dispatchEvent(new Event('change'));
            }
        });
    });

    document.querySelectorAll('.qty-input').forEach(input => {
        input.addEventListener('change', async function () {
            const row = this.closest('tr');
            const id = row.dataset.id;
            const quantity = parseInt(this.value);
            if (quantity < 1) { this.value = 1; return; }

            const res = await fetch('/Cart/Update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `cartItemId=${id}&quantity=${quantity}`
            });

            const data = await res.json();
            if (data.success) {
                row.querySelector('.cart-subtotal').textContent =
                    data.itemSubTotal.toLocaleString('vi-VN') + ' ₫';
                document.getElementById('cartTotal').textContent =
                    data.totalAmount.toLocaleString('vi-VN') + ' ₫';
            }
        });
    });

    // Xóa sản phẩm
    document.querySelectorAll('.btn-remove').forEach(btn => {
        btn.addEventListener('click', async function () {
            if (!confirm('Xóa sản phẩm này khỏi giỏ?')) return;
            const row = this.closest('tr');
            const id = row.dataset.id;

            const res = await fetch('/Cart/Remove', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `cartItemId=${id}`
            });

            const data = await res.json();
            if (data.success) {
                row.remove();
                document.getElementById('cartTotal').textContent =
                    data.totalAmount.toLocaleString('vi-VN') + ' ₫';
                updateBadge(data.totalQuantity);
                if (data.totalQuantity === 0) location.reload();
            }
        });
    });

    function showToast(msg) {
        const t = document.createElement('div');
        t.className = 'toast';
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(() => t.classList.add('show'), 10);
        setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 300); }, 2000);
    }
});