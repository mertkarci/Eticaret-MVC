document.addEventListener('DOMContentLoaded', function () {
    const filterContainer = document.getElementById('filter-container');
    if (!filterContainer) return; // Sayfada filtre yoksa dur

    const filterUrl = filterContainer.getAttribute('data-filter-url');
    const categoryIdInput = document.getElementById('categoryIdHidden');

    const minInput = document.getElementById('minPriceInput');
    const maxInput = document.getElementById('maxPriceInput');
    const searchInput = document.getElementById('searchInput');
    const productContainer = document.getElementById('productListContainer');
    const clearBtn = document.querySelector('.filter-clear');

    let typingTimer;
    const doneTypingInterval = 500;

    function applyFilters() {
        let params = new URLSearchParams();
        
        // 1. GÜVENLİK (Sadece Body'de gönderiyoruz, C# MVC böyle sever)
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElement) {
            params.append('__RequestVerificationToken', tokenElement.value);
        }

        // 2. MODEL BINDING GÜVENLİĞİ (400 Bad Request'i çözer)
        // Kategori ID boşsa C# patlamasın diye '0' gönderiyoruz
        if (categoryIdInput && categoryIdInput.value) {
            params.append('categoryId', categoryIdInput.value);
        } else {
            params.append('categoryId', '0'); 
        }

        if (minInput && minInput.value) params.append('minPrice', minInput.value);
        if (maxInput && maxInput.value) params.append('maxPrice', maxInput.value);
        if (searchInput && searchInput.value) params.append('searchTerm', searchInput.value);

        document.querySelectorAll('input[name="SelectedBrands"]:checked').forEach(cb => {
            params.append('selectedBrands', cb.value);
        });

        document.querySelectorAll('input[name="SelectedCategories"]:checked').forEach(cb => {
            params.append('selectedCategories', cb.value);
        });

        if (productContainer) productContainer.style.opacity = "0.5";

        fetch(filterUrl, {
            method: 'POST',
            credentials: 'same-origin', // Çerezleri de gönder
            headers: {
                // Header'dan token'ı kaldırdık, sadece veri tipini belirtiyoruz
                'Content-Type': 'application/x-www-form-urlencoded' 
            },
            body: params.toString()
        })
            .then(response => {
                if (!response.ok) throw new Error("Ağ hatası: " + response.status);
                return response.text();
            })
            .then(html => {
                if (productContainer) {
                    productContainer.innerHTML = html;
                    productContainer.style.opacity = "1";
                }
            })
            .catch(error => {
                console.error('Filtreleme hatası:', error);
                if (productContainer) productContainer.style.opacity = "1";
            });
    }

    // ====================================================
    // HATA 1 (Satır 83) İÇİN KESİN ÇÖZÜM (Null Kontrollü)
    // ====================================================

    if (minInput) {
        minInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        minInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    if (maxInput) {
        maxInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        maxInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    if (searchInput) {
        searchInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        searchInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    document.querySelectorAll('.filter-checkbox').forEach(box => {
        box.addEventListener('change', applyFilters);
    });

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            if (minInput) minInput.value = '';
            if (maxInput) maxInput.value = '';
            if (searchInput) searchInput.value = '';
            document.querySelectorAll('input[name="SelectedBrands"], input[name="SelectedCategories"]')
                .forEach(cb => cb.checked = false);

            applyFilters();
        });
    }
});