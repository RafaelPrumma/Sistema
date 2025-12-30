(function () {
    'use strict';

    const $ = window.jQuery;
    if (!$) {
        console.error('jQuery é obrigatório para site.js');
        return;
    }

    window.$ = $;
    window.jQuery = $;

    const toast = () => window.iziToast;

    function notify(type, title, message) {
        const instance = toast();
        if (instance && typeof instance[type] === 'function') {
            instance[type]({ title, message });
        } else {
            const prefix = title ? `${title}: ` : '';
            console.log(`${prefix}${message}`);
        }
    }

    window.showSuccess = function (message) {
        notify('success', 'Sucesso', message);
    };

    window.showError = function (message) {
        notify('error', 'Erro', message);
    };

    window.showWarning = function (message) {
        notify('warning', 'Alerta', message);
    };

    window.showInfo = function (message) {
        notify('info', 'Info', message);
    };

    $(function () {
        const root = document.documentElement;
        const serverTheme = document.body.getAttribute('data-bs-theme') || 'light';
        const initialTheme = serverTheme;
        document.body.setAttribute('data-bs-theme', initialTheme);
        root.style.colorScheme = initialTheme;
        localStorage.setItem('theme', initialTheme);

        const $temaToggle = $('#temaToggle');
        const $temaSidebar = $('#temaSidebar');
        if ($temaToggle.length && $temaSidebar.length) {
            $temaToggle.on('click', function (e) {
                e.preventDefault();
                $temaSidebar.addClass('show');
            });

            $(document).on('click', function (e) {
                if (
                    !$temaSidebar.is(e.target) &&
                    $temaSidebar.has(e.target).length === 0 &&
                    !$temaToggle.is(e.target) &&
                    $temaToggle.has(e.target).length === 0
                ) {
                    $temaSidebar.removeClass('show');
                }
            });
        }

        if (window.AOS && typeof window.AOS.init === 'function') {
            window.AOS.init({
                duration: 750,
                easing: 'ease-out-quart',
                once: true,
            });
        }

        $('input[name="ModoEscuro"]').on('change', function () {
            const theme = $(this).val() === 'true' ? 'dark' : 'light';
            document.body.setAttribute('data-bs-theme', theme);
            root.style.colorScheme = theme;
            localStorage.setItem('theme', theme);
        });

        const $headerInput = $('#CorHeader');
        if ($headerInput.length) {
            $headerInput.on('input', function () {
                root.style.setProperty('--header-bg', $(this).val());
            });
        }

        const $leftInput = $('#CorBarraEsquerda');
        if ($leftInput.length) {
            $leftInput.on('input', function () {
                root.style.setProperty('--sidebar-bg', $(this).val());
            });
        }

        const $rightInput = $('#CorBarraDireita');
        if ($rightInput.length) {
            $rightInput.on('input', function () {
                root.style.setProperty('--rightbar-bg', $(this).val());
            });
        }

        const $footerInput = $('#CorFooter');
        if ($footerInput.length) {
            $footerInput.on('input', function () {
                root.style.setProperty('--footer-bg', $(this).val());
            });
        }

        const $headerEl = $('header.navbar');
        const $footerEl = $('footer.footer');

        const $headerFix = $('#HeaderFixo');
        if ($headerFix.length && $headerEl.length) {
            $headerFix.on('change', function () {
                $headerEl.toggleClass('fixed-top', this.checked);
                $('body').toggleClass('pt-5', this.checked);
            });
        }

        const $footerFix = $('#FooterFixo');
        if ($footerFix.length && $footerEl.length) {
            $footerFix.on('change', function () {
                $footerEl.toggleClass('fixed-bottom', this.checked).toggleClass('mt-auto', !this.checked);
                $('body').toggleClass('pb-5', this.checked);
            });
        }

    });
})();
