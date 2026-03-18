// Adres Kartı Seçim Efekti
function selectAddress(element) {
    document.querySelectorAll('input[name="DeliveryAddressGuid"]').forEach(input => {
        input.closest('.address-card').classList.remove('selected');
    });
    element.classList.add('selected');

    const radio = element.querySelector('input[type="radio"]');
    if (radio) {
        radio.checked = true;
    }

    updateBillingInput();
}

function selectBillingAddress(element) {
    document.querySelectorAll('input[name="BillingAddressGuid"][type="radio"]').forEach(input => {
        input.closest('.address-card').classList.remove('selected');
    });
    element.classList.add('selected');
    const radio = element.querySelector('input[type="radio"]');
    if (radio) {
        radio.checked = true;
    }
}

function toggleBillingSection() {
    const checkbox = document.getElementById('sameAddress');
    const section = document.getElementById('billingSection');

    if (checkbox && section) {
        if (checkbox.checked) {
            section.style.display = 'none';
            // Radyo butonlarındaki seçimleri kaldır ki çakışma olmasın
            document.querySelectorAll('input[name="BillingAddressGuid"][type="radio"]').forEach(r => r.checked = false);
            document.querySelectorAll('#billingSection .address-card').forEach(c => c.classList.remove('selected'));
        } else {
            section.style.display = 'block';
        }
        updateBillingInput();
    }
}

function updateBillingInput() {
    const checkbox = document.getElementById('sameAddress');
    const hiddenInput = document.getElementById('billingAddressDefault');
    const selectedDelivery = document.querySelector('input[name="DeliveryAddressGuid"]:checked');

    if (checkbox && checkbox.checked) {
        if (hiddenInput) {
            hiddenInput.name = "BillingAddressGuid"; // MVC'nin beklediği isimle aktif et
            if (selectedDelivery) {
                hiddenInput.value = selectedDelivery.value;
            }
        }
    } else {
        if (hiddenInput) {
            hiddenInput.name = ""; // Fatura adresi farklıysa hidden input gönderilmesin
        }
    }
}

// Sayfa yüklendiğinde ve form gönderilmeden önce çalıştır
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('checkoutForm');
    if(form) {
        // Sayfa ilk açıldığında seçili bir teslimat adresi yoksa ilkini otomatik seç
        let selectedDelivery = document.querySelector('input[name="DeliveryAddressGuid"]:checked');
        if (!selectedDelivery) {
            const firstDelivery = document.querySelector('input[name="DeliveryAddressGuid"]');
            if (firstDelivery) {
                firstDelivery.checked = true;
                firstDelivery.closest('.address-card').classList.add('selected');
            }
        }

        updateBillingInput();

        // Form gönderimi sırasında son bir kez daha değerin doğruluğundan emin ol
        form.addEventListener('submit', function(e) {
            updateBillingInput();
        });
    }
});