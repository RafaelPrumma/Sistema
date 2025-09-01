import $ from 'jquery';
import 'bootstrap-icons/font/bootstrap-icons.css';
import 'izitoast/dist/css/iziToast.min.css';
import 'izimodal/css/iziModal.min.css';
import 'izimodal/js/iziModal.min.js';
import AOS from 'aos';
import 'aos/dist/aos.css';
import '../css/site.scss';
import iziToast from 'izitoast';

window.$ = $;
window.jQuery = $;

window.showSuccess = function (message) {
    iziToast.success({ title: 'Sucesso', message });
};

window.showError = function (message) {
    iziToast.error({ title: 'Erro', message });
};

window.showWarning = function (message) {
    iziToast.warning({ title: 'Alerta', message });
};

window.showInfo = function (message) {
    iziToast.info({ title: 'Info', message });
};

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
    AOS.init();

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
