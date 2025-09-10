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
    var theme = savedTheme || (document.body.getAttribute('data-bs-theme') || 'light');
    document.body.setAttribute('data-bs-theme', theme);
    root.style.colorScheme = theme;

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

    AOS.init();

    $('input[name="ModoEscuro"]').on('change', function () {
        var theme = $(this).val() === 'true' ? 'dark' : 'light';
        document.body.setAttribute('data-bs-theme', theme);
        root.style.colorScheme = theme;
        localStorage.setItem('theme', theme);
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

    var $sidebarToggle = $('#sidebarToggle');
    var $sidebar = $('#sidebar');
    if ($sidebarToggle.length && $sidebar.length) {
        var themeExpanded = $sidebar.data('expanded');

        function applySidebarState() {
            if ($(window).width() < 768) {
                $sidebar.addClass('collapsed');
            } else {
                $sidebar.toggleClass('collapsed', !themeExpanded);
            }
        }

        applySidebarState();
        $(window).on('resize', applySidebarState);

        $sidebarToggle.on('click', function (e) {
            e.stopPropagation();
            $sidebar.addClass('expanded').removeClass('collapsed');
        });

        $(document).on('click', function (e) {
            if ($sidebar.hasClass('expanded') && !$sidebar.is(e.target) && $sidebar.has(e.target).length === 0 && !$sidebarToggle.is(e.target) && $sidebarToggle.has(e.target).length === 0) {
                $sidebar.removeClass('expanded');
                applySidebarState();
            }
        });
    }

});
