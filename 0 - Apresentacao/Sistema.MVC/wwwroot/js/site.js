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
        const savedTheme = localStorage.getItem('theme');
        const initialTheme = savedTheme || (document.body.getAttribute('data-bs-theme') || 'light');
        document.body.setAttribute('data-bs-theme', initialTheme);
        root.style.colorScheme = initialTheme;

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
            window.AOS.init();
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

        const $sidebarToggle = $('#sidebarToggle');
        const $sidebar = $('#sidebar');
        if ($sidebarToggle.length && $sidebar.length) {
            const themeExpanded = $sidebar.data('expanded');
            const $sidebarBackdrop = $('<div class="sidebar-backdrop" aria-hidden="true"></div>');

            function attachBackdrop() {
                if (!$sidebarBackdrop.parent().length) {
                    $('body').append($sidebarBackdrop);
                }
            }

            function showBackdrop() {
                attachBackdrop();
                $sidebarBackdrop.addClass('show');
                $('body').addClass('sidebar-open');
            }

            function hideBackdrop() {
                $sidebarBackdrop.removeClass('show');
                $('body').removeClass('sidebar-open');
            }

            function applySidebarState() {
                const isMobile = $(window).width() < 768;
                if (isMobile) {
                    $sidebar.addClass('collapsed');
                    if (!$sidebar.hasClass('expanded')) {
                        hideBackdrop();
                    }
                } else {
                    hideBackdrop();
                    $sidebar.removeClass('expanded');
                    $sidebar.toggleClass('collapsed', !themeExpanded);
                }
            }

            applySidebarState();
            $(window).on('resize', applySidebarState);

            $sidebarToggle.on('click', function (e) {
                e.stopPropagation();
                const isMobile = $(window).width() < 768;
                if (isMobile) {
                    $sidebar.addClass('expanded').removeClass('collapsed');
                    showBackdrop();
                } else {
                    $sidebar.toggleClass('collapsed');
                }
            });

            $(document).on('click', function (e) {
                if (
                    $sidebar.hasClass('expanded') &&
                    !$sidebar.is(e.target) &&
                    $sidebar.has(e.target).length === 0 &&
                    !$sidebarToggle.is(e.target) &&
                    $sidebarToggle.has(e.target).length === 0
                ) {
                    $sidebar.removeClass('expanded');
                    hideBackdrop();
                    applySidebarState();
                }
            });

            $(document).on('keydown', function (e) {
                if (e.key === 'Escape' && $sidebar.hasClass('expanded')) {
                    $sidebar.removeClass('expanded');
                    hideBackdrop();
                    applySidebarState();
                }
            });

            $sidebarBackdrop.on('click', function () {
                $sidebar.removeClass('expanded');
                hideBackdrop();
                applySidebarState();
            });
        }
    });
})();
