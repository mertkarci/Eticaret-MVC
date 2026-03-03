document.addEventListener('DOMContentLoaded', function () {
    // Filtreleme kapsayıcısını ve URL'ini al
    const filterContainer = document.getElementById('filter-container');
    if (!filterContainer) return; // Eğer sayfada filtre alanı yoksa çalışmayı durdur

    const filterUrl = filterContainer.getAttribute('data-filter-url');
    const categoryIdInput = document.getElementById('categoryIdHidden');

    const minInput = document.getElementById('minPriceInput');
    const maxInput = document.getElementById('maxPriceInput');
    const searchInput = document.getElementById('searchInput');
    const productContainer = document.getElementById('productListContainer');

    let typingTimer;
    const doneTypingInterval = 500;

    function applyFilters() {
        let formData = new FormData();

        // Kategori ID varsa ekle (Categories/Index için), yoksa (Products/Index) boş geç veya 0
        if (categoryIdInput) {
            formData.append('categoryId', categoryIdInput.value);
        }

        formData.append('minPrice', minInput ? (minInput.value || 0) : 0);
        formData.append('maxPrice', maxInput ? (maxInput.value || 999999) : 999999);
        formData.append('searchTerm', searchInput ? (searchInput.value || "") : "");

        // Seçili markaları topla
        document.querySelectorAll('input[name="SelectedBrands"]:checked').forEach(cb => {
            formData.append('selectedBrands', cb.value);
        });

        // Seçili kategorileri topla
        document.querySelectorAll('input[name="SelectedCategories"]:checked').forEach(cb => {
            formData.append('selectedCategories', cb.value);
        });

        // Opaklığı düşür (yükleniyor efekti)
        if (productContainer) productContainer.style.opacity = "0.5";

        fetch(filterUrl, {
            method: 'POST',
            body: formData
        })
            .then(response => {
                if (!response.ok) throw new Error("Ağ hatası");
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

    // Input dinleyicileri
    [minInput, maxInput, searchInput].forEach(input => {
        if (!input) return;

        input.addEventListener('keyup', () => {
            clearTimeout(typingTimer);
            typingTimer = setTimeout(applyFilters, doneTypingInterval);
        });

        input.addEventListener('change', () => {
            clearTimeout(typingTimer);
            applyFilters();
        });
    });

    // Checkbox dinleyicileri
    document.querySelectorAll('.filter-checkbox').forEach(box => {
        box.addEventListener('change', applyFilters);
    });

    //Filtre Temizleme
    document.querySelector('.filter-clear').addEventListener('click', () => {
        if (minInput) minInput.value = '';
        if (maxInput) maxInput.value = '';
        if (searchInput) searchInput.value = '';
        document.querySelectorAll('input[name="SelectedBrands"], input[name="SelectedCategories"]')
            .forEach(cb => cb.checked = false);

        applyFilters();
    });
});


