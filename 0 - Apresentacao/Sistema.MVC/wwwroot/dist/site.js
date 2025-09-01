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

$(function () {
    var root = document.documentElement;
    var savedTheme = localStorage.getItem('theme');
    var theme = savedTheme || (root.dataset.theme || ($('body').hasClass('bg-dark') ? 'dark' : 'light'));
    root.dataset.theme = theme;
    root.style.colorScheme = theme;
    $('body')
        .toggleClass('bg-dark text-white', theme === 'dark')
        .toggleClass('bg-light text-dark', theme === 'light');

    var $temaToggle = $('#temaToggle');
    var $temaSidebar = $('#temaSidebar');
    if ($temaToggle.length && $temaSidebar.length) {
        $temaToggle.on('click', function (e) {
            e.preventDefault();
            $temaSidebar.addClass('show');
        });
        $(document).on('click', function (e) {
            if (!$temaSidebar.is(e.target) && $temaSidebar.has(e.target).length === 0 && !$temaToggle.is(e.target) && $temaToggle.has(e.target).length === 0) {
                $temaSidebar.removeClass('show');
            }
        });
    }

    var $menuToggle = $('#menuToggle');
    var $leftSidebar = $('aside.sidebar');
    var $menuCheckbox = $('#MenuLateralExpandido');
    var syncMenu = function () {
        if (!$leftSidebar.length) return;
        if (window.innerWidth <= 768) {
            $leftSidebar.addClass('collapsed');
            if ($menuCheckbox.length) {
                $menuCheckbox.prop('checked', false);
            }
        } else if ($menuCheckbox.length) {
            $leftSidebar.toggleClass('collapsed', !$menuCheckbox.prop('checked'));
        } else {
            $leftSidebar.removeClass('collapsed');
        }
    };

    if ($menuToggle.length && $leftSidebar.length) {
        $menuToggle.on('click', function () {
            $leftSidebar.toggleClass('collapsed');
            if ($menuCheckbox.length) {
                $menuCheckbox.prop('checked', !$leftSidebar.hasClass('collapsed'));
            }
            syncMenu();
        });
    }

    $(window).on('resize', syncMenu);
    syncMenu();

    AOS.init();

    $('input[name="ModoEscuro"]').on('change', function () {
        var theme = $(this).val() === 'true' ? 'dark' : 'light';
        root.dataset.theme = theme;
        root.style.colorScheme = theme;
        localStorage.setItem('theme', theme);
        $('body')
            .toggleClass('bg-dark text-white', theme === 'dark')
            .toggleClass('bg-light text-dark', theme === 'light');
    });

    var $headerInput = $('#CorHeader');
    if ($headerInput.length) {
        $headerInput.on('input', function () {
            root.style.setProperty('--header-bg', $(this).val());
        });
    }

    var $leftInput = $('#CorBarraEsquerda');
    if ($leftInput.length) {
        $leftInput.on('input', function () {
            root.style.setProperty('--sidebar-bg', $(this).val());
        });
    }

    var $rightInput = $('#CorBarraDireita');
    if ($rightInput.length) {
        $rightInput.on('input', function () {
            root.style.setProperty('--rightbar-bg', $(this).val());
        });
    }

    var $footerInput = $('#CorFooter');
    if ($footerInput.length) {
        $footerInput.on('input', function () {
            root.style.setProperty('--footer-bg', $(this).val());
        });
    }

    var $headerEl = $('header.navbar');
    var $footerEl = $('footer.footer');

    var $headerFix = $('#HeaderFixo');
    if ($headerFix.length && $headerEl.length) {
        $headerFix.on('change', function () {
            $headerEl.toggleClass('fixed-top', this.checked);
            $('body').toggleClass('pt-5', this.checked);
        });
    }

    var $footerFix = $('#FooterFixo');
    if ($footerFix.length && $footerEl.length) {
        $footerFix.on('change', function () {
            $footerEl.toggleClass('fixed-bottom', this.checked).toggleClass('mt-auto', !this.checked);
            $('body').toggleClass('pb-5', this.checked);
        });
    }

    if ($menuCheckbox.length && $leftSidebar.length) {
        $menuCheckbox.on('change', syncMenu);
    }
});
