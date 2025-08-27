// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function showSuccess(message) {
    iziToast.success({ title: 'Sucesso', message });
}

function showError(message) {
    iziToast.error({ title: 'Erro', message });
}

function showWarning(message) {
    iziToast.warning({ title: 'Alerta', message });
}

function showInfo(message) {
    iziToast.info({ title: 'Info', message });
}

document.addEventListener('DOMContentLoaded', function () {
    var temaToggle = document.getElementById('temaToggle');
    var temaSidebar = document.getElementById('temaSidebar');
    if (temaToggle && temaSidebar) {
        temaToggle.addEventListener('click', function (e) {
            e.preventDefault();
            temaSidebar.classList.add('show');
        });
        document.addEventListener('click', function (e) {
            if (!temaSidebar.contains(e.target) && e.target !== temaToggle && !temaToggle.contains(e.target)) {
                temaSidebar.classList.remove('show');
            }
        });
    }
    var menuToggle = document.getElementById('menuToggle');
    var leftSidebar = document.querySelector('aside.sidebar');
    var menuCheckbox = document.getElementById('MenuLateralExpandido');
    if (menuToggle && leftSidebar) {
        menuToggle.addEventListener('click', function () {
            leftSidebar.classList.toggle('collapsed');
            if (menuCheckbox) {
                menuCheckbox.checked = !leftSidebar.classList.contains('collapsed');
            }
        });
    }
    if (typeof AOS !== 'undefined') {
        AOS.init();
    }

    function getTextClass(hex) {
        if (!hex) return 'text-dark';
        hex = hex.replace('#', '');
        if (hex.length === 6) {
            var r = parseInt(hex.substring(0, 2), 16);
            var g = parseInt(hex.substring(2, 4), 16);
            var b = parseInt(hex.substring(4, 6), 16);
            var lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            return lum > 0.5 ? 'text-dark' : 'text-white';
        }
        return 'text-dark';
    }

    function applyBackground(element, color) {
        if (!element) return;
        element.style.backgroundColor = color;
        var textClass = getTextClass(color);
        element.classList.remove('text-dark', 'text-white');
        element.classList.add(textClass);
        return textClass;
    }

    document.querySelectorAll('input[name="ModoEscuro"]').forEach(function (radio) {
        radio.addEventListener('change', function () {
            if (this.value === 'true') {
                document.body.classList.remove('bg-light', 'text-dark');
                document.body.classList.add('bg-dark', 'text-white');
            } else {
                document.body.classList.remove('bg-dark', 'text-white');
                document.body.classList.add('bg-light', 'text-dark');
            }
        });
    });

    var headerInput = document.getElementById('CorHeader');
    var headerEl = document.querySelector('header.navbar');
    if (headerInput && headerEl) {
        headerInput.addEventListener('input', function () {
            var textClass = applyBackground(headerEl, this.value);
            headerEl.classList.remove('navbar-dark', 'navbar-light');
            headerEl.classList.add(textClass === 'text-white' ? 'navbar-dark' : 'navbar-light');
            headerEl.querySelectorAll('.navbar-brand, #userMenu').forEach(function (el) {
                el.classList.remove('text-dark', 'text-white');
                el.classList.add(textClass);
            });
        });
    }

    var leftInput = document.getElementById('CorBarraEsquerda');
    var leftEl = document.querySelector('aside.sidebar');
    if (leftInput && leftEl) {
        leftInput.addEventListener('input', function () {
            var textClass = applyBackground(leftEl, this.value);
            leftEl.querySelectorAll('.nav-link').forEach(function (a) {
                a.classList.remove('text-dark', 'text-white');
                a.classList.add(textClass);
            });
        });
    }

    var rightInput = document.getElementById('CorBarraDireita');
    var rightEl = document.getElementById('temaSidebar');
    if (rightInput && rightEl) {
        rightInput.addEventListener('input', function () {
            var textClass = applyBackground(rightEl, this.value);
            rightEl.classList.remove('text-dark', 'text-white');
            rightEl.classList.add(textClass);
        });
    }

    var footerInput = document.getElementById('CorFooter');
    var footerEl = document.querySelector('footer.footer');
    if (footerInput && footerEl) {
        footerInput.addEventListener('input', function () {
            var textClass = applyBackground(footerEl, this.value);
            footerEl.querySelectorAll('a').forEach(function (a) {
                a.classList.remove('text-dark', 'text-white');
                a.classList.add(textClass);
            });
        });
    }

    var headerFix = document.getElementById('HeaderFixo');
    if (headerFix && headerEl) {
        headerFix.addEventListener('change', function () {
            headerEl.classList.toggle('fixed-top', this.checked);
            document.body.classList.toggle('pt-5', this.checked);
        });
    }

    var footerFix = document.getElementById('FooterFixo');
    if (footerFix && footerEl) {
        footerFix.addEventListener('change', function () {
            footerEl.classList.toggle('fixed-bottom', this.checked);
            footerEl.classList.toggle('mt-auto', !this.checked);
            document.body.classList.toggle('pb-5', this.checked);
        });
    }

    if (menuCheckbox && leftSidebar) {
        menuCheckbox.addEventListener('change', function () {
            leftSidebar.classList.toggle('collapsed', !this.checked);
        });
    }
});
