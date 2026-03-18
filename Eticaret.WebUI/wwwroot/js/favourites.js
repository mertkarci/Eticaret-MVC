document.addEventListener('click', function (e) {
    const button = e.target.closest('.fav-toggle-btn');

    if (button) {
        e.preventDefault();

        const productId = button.dataset.productId;
        const icon = button.querySelector('i');

        button.disabled = true;

        const params = new URLSearchParams();
        params.append('productId', productId);

        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElement) {
            params.append('__RequestVerificationToken', tokenElement.value);
        }

        fetch('/favorilerim/toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: params.toString()
        })
        .then(response => {
            if (!response.ok) throw new Error("Sunucu Hatası: " + response.status);
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // 1. Tıklanan Butonun İkonunu Güncelle
                if (data.isFavorite) {
                    icon.classList.remove('bi-heart');
                    icon.classList.add('bi-heart-fill', 'text-red-500');
                    button.setAttribute('title', 'Favorilerden Çıkar');
                } else {
                    icon.classList.remove('bi-heart-fill', 'text-red-500');
                    icon.classList.add('bi-heart');
                    button.setAttribute('title', 'Favorilere Ekle');
                }

                // 2. KUSURSUZ NAVBAR GÜNCELLEMESİ (Tasarımı bozmadan arka planda HTML'i yeniler)
                fetch(window.location.href)
                    .then(res => res.text())
                    .then(html => {
                        const parser = new DOMParser();
                        const doc = parser.parseFromString(html, 'text/html');
                        
                        const newFavLinks = doc.querySelectorAll('a[href="/favorilerim"]');
                        const oldFavLinks = document.querySelectorAll('a[href="/favorilerim"]');
                        
                        oldFavLinks.forEach((oldLink, index) => {
                            if (newFavLinks[index]) {
                                // 1. Linkin kendisini (ikon ve sayı) güncelle
                                oldLink.innerHTML = newFavLinks[index].innerHTML;
                                
                                // 2. Eğer bu linke bağlı bir açılır pencere (dropdown-menu) varsa, onun da içini güncelle
                                const oldDropdown = oldLink.parentElement.querySelector('.dropdown-menu') || oldLink.nextElementSibling;
                                const newDropdown = newFavLinks[index].parentElement.querySelector('.dropdown-menu') || newFavLinks[index].nextElementSibling;
                                
                                if (oldDropdown && newDropdown && oldDropdown.classList?.contains('dropdown-menu')) {
                                    oldDropdown.innerHTML = newDropdown.innerHTML;
                                }
                            }
                        });
                    });
            } 
        })
        .catch(error => {
            console.error('Favori işlemi sırasında ağ hatası:', error);
        })
        .finally(() => {
            button.disabled = false;
        });
    }
});

// --- TÜM SEPET İŞLEMLERİNİ (Ekle, Sil, Güncelle) AJAX'A ÇEVİRME ---
document.addEventListener('submit', function (e) {
    const form = e.target;
    // URL encoding'i (%c3%b6deme gibi) çözmek için decodeURIComponent kullanıyoruz
    const actionUrl = form.action ? decodeURIComponent(form.action).toLowerCase() : '';
    
    // Eğer form action sepet işlemiyse (ekle, sil, guncelle)
    if (actionUrl.includes('/sepetim') || actionUrl.includes('/cart')) {
        // Ödeme adımlarını (checkout) bu arka plan işleminden hariç tut
        if (actionUrl.includes('checkout') || actionUrl.includes('odeme') || actionUrl.includes('ödeme')) return;

        e.preventDefault(); // Sayfanın hata vermesini/yönlenmesini tamamen durdur, arka planda çözelim!
        
        const submitBtn = form.querySelector('button[type="submit"]');
        let originalHtml = '';
        
        if (submitBtn) {
            submitBtn.disabled = true;
            originalHtml = submitBtn.innerHTML;
            
            // Eğer ekleme işlemiyse yazıyı değiştir, güncelleme/silme ise sadece yükleniyor ikonu koy
            if (actionUrl.includes('ekle') || actionUrl.includes('add')) {
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split animate-spin"></i> Ekleniyor...';
            } else {
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split animate-spin"></i>';
            }
        }

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            redirect: 'follow' // İşlem bitince sunucunun yönlendirdiği güncel sayfayı çeker
        })
        .then(response => response.text())
        .then(html => {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            // 1. NAVBAR SEPET GÜNCELLEMESİ (Yüzen Sepet Hariç Tutuldu)
            const newCartLinks = doc.querySelectorAll('a[href="/sepetim"]:not(#floating-cart-btn), a[href="/Cart"]:not(#floating-cart-btn)');
            const oldCartLinks = document.querySelectorAll('a[href="/sepetim"]:not(#floating-cart-btn), a[href="/Cart"]:not(#floating-cart-btn)');
            
            oldCartLinks.forEach((oldLink, index) => {
                if (newCartLinks[index]) {
                    oldLink.innerHTML = newCartLinks[index].innerHTML;
                    
                    const oldDropdown = oldLink.parentElement.querySelector('.dropdown-menu, .cart-dropdown-menu') || oldLink.nextElementSibling;
                    const newDropdown = newCartLinks[index].parentElement.querySelector('.dropdown-menu, .cart-dropdown-menu') || newCartLinks[index].nextElementSibling;
                    
                    if (oldDropdown && newDropdown && (oldDropdown.classList?.contains('dropdown-menu') || oldDropdown.classList?.contains('cart-dropdown-menu'))) {
                        oldDropdown.innerHTML = newDropdown.innerHTML;
                    }
                }
            });

            // 1.1 YÜZEN SEPET GÜNCELLEMESİ (Sadece ID ile nokta atışı günceller)
            const oldFloating = document.getElementById('floating-cart-btn');
            const newFloating = doc.getElementById('floating-cart-btn');
            if (oldFloating && newFloating) {
                oldFloating.className = newFloating.className;
                oldFloating.innerHTML = newFloating.innerHTML;
            }

            // 2. EĞER ŞU AN "SEPETİM" SAYFASINDAYSAK ANA İÇERİĞİ DE (TABLOYU) GÜNCELLE
            if (window.location.pathname.toLowerCase().includes('/sepetim') || window.location.pathname.toLowerCase().includes('/cart')) {
                const mainPage = document.querySelector('main');
                const newMainPage = doc.querySelector('main');
                if (mainPage && newMainPage) {
                    mainPage.innerHTML = newMainPage.innerHTML;
                }
            }

            // 3. BUTON EFEKTLERİ
            if (submitBtn && (actionUrl.includes('ekle') || actionUrl.includes('add'))) {
                submitBtn.innerHTML = '<i class="bi bi-check2-circle text-lg"></i> Sepette';
                submitBtn.classList.add('!bg-emerald-500', '!text-white', '!border-emerald-500');
                
                setTimeout(() => {
                    submitBtn.innerHTML = originalHtml;
                    submitBtn.disabled = false;
                    submitBtn.classList.remove('!bg-emerald-500', '!text-white', '!border-emerald-500');
                }, 2000);
            } else if (submitBtn) {
                // Silme veya Güncelleme işlemi ise butonu sadece eski haline getir
                submitBtn.innerHTML = originalHtml;
                submitBtn.disabled = false;
            }
        })
        .catch(error => {
            console.error('Sepet işlemi sırasında hata oluştu:', error);
            if (submitBtn) {
                submitBtn.innerHTML = originalHtml;
                submitBtn.disabled = false;
            }
        });
    }
});