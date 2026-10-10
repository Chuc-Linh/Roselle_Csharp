// ============================================
// SITE.JS — Loading Spinner, Toast, Modal
// ============================================

// === 1. TOAST — Thông báo nổi ===
function showToast(message, type = 'success') {
    // Xóa toast cũ nếu có
    const oldToast = document.querySelector('.toast');
    if (oldToast) oldToast.remove();

    // Tạo toast mới
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    toast.textContent = message;

    document.body.appendChild(toast);

    // Hiện
    setTimeout(() => toast.classList.add('show'), 10);

    // Tự động ẩn sau 3s
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// === 2. LOADING SPINNER ===
function showLoading(message = 'Đang tải...') {
    let overlay = document.getElementById('loadingOverlay');
    if (overlay) return;

    overlay = document.createElement('div');
    overlay.id = 'loadingOverlay';
    overlay.className = 'loading-overlay';
    overlay.innerHTML = `
        <div class="loading-content">
            <div class="spinner"></div>
            <p>${message}</p>
        </div>
    `;
    document.body.appendChild(overlay);
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => overlay.remove(), 300);
    }
}

// === 3. MODAL — Hộp thoại xác nhận ===
function showConfirm(message, onConfirm, onCancel) {
    // Xóa modal cũ
    const old = document.getElementById('confirmModal');
    if (old) old.remove();

    const modal = document.createElement('div');
    modal.id = 'confirmModal';
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-content">
            <p class="modal-message">${message}</p>
            <div class="modal-actions">
                <button class="btn btn-outline" id="modalCancel">Hủy</button>
                <button class="btn btn-danger" id="modalConfirm">Xác nhận</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    setTimeout(() => modal.classList.add('show'), 10);

    // Nút Hủy
    modal.querySelector('#modalCancel').addEventListener('click', () => {
        modal.classList.remove('show');
        setTimeout(() => modal.remove(), 300);
        if (onCancel) onCancel();
    });

    // Nút Xác nhận
    modal.querySelector('#modalConfirm').addEventListener('click', () => {
        modal.classList.remove('show');
        setTimeout(() => modal.remove(), 300);
        if (onConfirm) onConfirm();
    });

    // Nhấn ngoài modal → đóng
    modal.addEventListener('click', (e) => {
        if (e.target === modal) {
            modal.classList.remove('show');
            setTimeout(() => modal.remove(), 300);
            if (onCancel) onCancel();
        }
    });
}

// === Export functions ra global ===
window.showToast = showToast;
window.showLoading = showLoading;
window.hideLoading = hideLoading;
window.showConfirm = showConfirm;

// === Auto-init: Thông báo nếu có TempData ===
document.addEventListener('DOMContentLoaded', function () {
    // Hiện loading khi submit form
    document.querySelectorAll('form[data-loading]').forEach(form => {
        form.addEventListener('submit', () => {
            showLoading('Đang xử lý...');
        });
    });
});