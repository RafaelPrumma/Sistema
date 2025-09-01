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

    var root = document.documentElement;

    var headerInput = document.getElementById('CorHeader');
    if (headerInput) {
        headerInput.addEventListener('input', function () {
            root.style.setProperty('--header-bg', this.value);
        });
    }

    var leftInput = document.getElementById('CorBarraEsquerda');
    if (leftInput) {
        leftInput.addEventListener('input', function () {
            root.style.setProperty('--sidebar-bg', this.value);
        });
    }

    var rightInput = document.getElementById('CorBarraDireita');
    if (rightInput) {
        rightInput.addEventListener('input', function () {
            root.style.setProperty('--rightbar-bg', this.value);
        });
    }

    var footerInput = document.getElementById('CorFooter');
    if (footerInput) {
        footerInput.addEventListener('input', function () {
            root.style.setProperty('--footer-bg', this.value);
        });
    }

    var headerEl = document.querySelector('header.navbar');
    var footerEl = document.querySelector('footer.footer');

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
