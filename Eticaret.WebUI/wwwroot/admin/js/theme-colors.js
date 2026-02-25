// Sayfanın HTML'i tamamen yüklendikten sonra çalışmaya başla
document.addEventListener('DOMContentLoaded', function () {
    
    // Sayfadaki tüm 'color-wrapper' class'ına sahip div'leri bul
    const colorWrappers = document.querySelectorAll('.color-wrapper');

    // Her bir wrapper (kutu) için tek tek dön
    colorWrappers.forEach(function (wrapper) {
        // Kutunun içindeki input'u ve span'i bul
        const input = wrapper.querySelector('.color-input');
        const display = wrapper.querySelector('.color-display');

        // Eğer ikisi de sayfada varsa işlemi başlat
        if (input && display) {
            // 1. Sayfa ilk açıldığında inputtaki rengi span'e yazdır
            display.textContent = input.value;

            // 2. Kullanıcı paletten renk seçtikçe (anlık olarak) span'i güncelle
            input.addEventListener('input', function () {
                display.textContent = this.value;
            });
        }
    });

});