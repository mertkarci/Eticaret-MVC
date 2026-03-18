document.addEventListener('DOMContentLoaded', function () {
    const filterContainer = document.getElementById('filter-container');
    if (!filterContainer) return;

    // Değişkenler
    const filterUrl = filterContainer.getAttribute('data-filter-url');
    const categoryIdInput = document.getElementById('categoryIdHidden');
    const minInput = document.getElementById('minPriceInput');
    const maxInput = document.getElementById('maxPriceInput');
    const searchInput = document.getElementById('searchInput');
    const sortSelect = document.getElementById('sortSelect');
    const productContainer = document.getElementById('productListContainer');
    const productCountText = document.getElementById('productCount');
    const clearBtn = document.querySelector('.filter-clear');

    let typingTimer;
    const doneTypingInterval = 500;

    // Filtreleme
    function applyFilters() {
        let params = new URLSearchParams();
        
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElement) params.append('__RequestVerificationToken', tokenElement.value);

        if (categoryIdInput && categoryIdInput.value) params.append('categoryId', categoryIdInput.value);
        else params.append('categoryId', '0'); 

        if (minInput && minInput.value) params.append('minPrice', minInput.value);
        if (maxInput && maxInput.value) params.append('maxPrice', maxInput.value);
        if (searchInput && searchInput.value) params.append('searchTerm', searchInput.value);
        if (sortSelect && sortSelect.value) params.append('sort', sortSelect.value);

        document.querySelectorAll('input[name="SelectedBrands"]:checked').forEach(cb => {
            params.append('selectedBrands', cb.value);
        });

        document.querySelectorAll('input[name="SelectedCategories"]:checked').forEach(cb => {
            params.append('selectedCategories', cb.value);
        });

        if (productContainer) productContainer.style.opacity = "0.5";

        fetch(filterUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
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
                    
                    // AJAX sonrası kaç ürün döndüğünü say ve metni güncelle
                    if (productCountText) {
                        productCountText.innerText = productContainer.querySelectorAll('.col').length;
                    }
                }
            })
            .catch(error => {
                console.error('Filtreleme hatası:', error);
                if (productContainer) productContainer.style.opacity = "1";
            });
    }

    // Fiyat
    if (minInput) {
        minInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        minInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    if (maxInput) {
        maxInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        maxInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    // Arama
    if (searchInput) {
        searchInput.addEventListener('keyup', () => { clearTimeout(typingTimer); typingTimer = setTimeout(applyFilters, doneTypingInterval); });
        searchInput.addEventListener('change', () => { clearTimeout(typingTimer); applyFilters(); });
    }

    // Sıralama
    if (sortSelect) {
        sortSelect.addEventListener('change', applyFilters);
    }

    // Kategoriler ve Markalar
    document.querySelectorAll('.filter-checkbox').forEach(box => {
        box.addEventListener('change', function() {
            // Ana kategori seçildiğinde/kaldırıldığında alt kategorileri göster/gizle
            if (this.classList.contains('parent-category-checkbox')) {
                const subContainer = document.getElementById('subcats_' + this.dataset.categoryId);
                if (subContainer) {
                    if (this.checked) {
                        subContainer.classList.remove('hidden');
                    } else {
                        subContainer.classList.add('hidden');
                        // Ana kategori işareti kaldırılınca alt kategorilerin işaretlerini de kaldır
                        subContainer.querySelectorAll('.sub-category-checkbox').forEach(subCb => subCb.checked = false);
                    }
                }
            }
            applyFilters();
        });
    });

    // Temizle
    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            if (minInput) minInput.value = '';
            if (maxInput) maxInput.value = '';
            if (searchInput) searchInput.value = '';
            if (sortSelect) sortSelect.value = 'recommended';
            document.querySelectorAll('input[name="SelectedBrands"], input[name="SelectedCategories"]')
                .forEach(cb => cb.checked = false);
                
            // Alt kategori konteynerlerini tamamen gizle
            document.querySelectorAll('.sub-categories-container').forEach(container => {
                container.classList.add('hidden');
            });

            applyFilters();
        });
    }
});