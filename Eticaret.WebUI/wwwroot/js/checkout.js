// Adres Kartı Seçim Efekti
function selectAddress(element) {
    // Tüm kartlardan selected sınıfını kaldır
    document.querySelectorAll('input[name="DeliveryAddress"]').forEach(input => {
        input.closest('.address-card').classList.remove('selected');
    });
    // Seçilene ekle
    element.classList.add('selected');

    // Radyo butonunu işaretle
    const radio = element.querySelector('input[type="radio"]');
    if (radio) radio.checked = true;

    // Eğer fatura adresi aynı ise hidden inputu güncelle
    updateBillingInput();
}

function selectBillingAddress(element) {
    document.querySelectorAll('input[name="BillingAddress"]').forEach(input => {
        input.closest('.address-card').classList.remove('selected');
    });
    element.classList.add('selected');
    const radio = element.querySelector('input[type="radio"]');
    if (radio) radio.checked = true;
}

function toggleBillingSection() {
    const checkbox = document.getElementById('sameAddress');
    const section = document.getElementById('billingSection');
    const billingDefault = document.getElementById('billingAddressDefault');

    if (checkbox && section) {
        if (checkbox.checked) {
            section.style.display = 'none';
            // Fatura adresi seçimlerini temizle
            document.querySelectorAll('input[name="BillingAddress"]').forEach(r => r.checked = false);
            updateBillingInput();
        } else {
            section.style.display = 'block';
            // Hidden inputu devre dışı bırak, artık radyo butonları geçerli
            if (billingDefault) billingDefault.name = "";
        }
    }
}

function updateBillingInput() {
    const checkbox = document.getElementById('sameAddress');
    if (checkbox && checkbox.checked) {
        const selectedDelivery = document.querySelector('input[name="DeliveryAddress"]:checked');
        const hiddenInput = document.getElementById('billingAddressDefault');

        if (hiddenInput) {
            // Hidden inputu aktif et ve değerini güncelle
            hiddenInput.name = "BillingAddress";
            if (selectedDelivery) {
                hiddenInput.value = selectedDelivery.value;
            }
        }
    }
}

// Sayfa yüklendiğinde çalıştır
document.addEventListener('DOMContentLoaded', function () {
    // Sadece checkout sayfasında çalışması için kontrol
    if(document.getElementById('checkoutForm')) {
        updateBillingInput();
    }
});