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

        const $sidebarToggle = $('#sidebarToggle');
        const sidebarSelector = '#sidebarMenu';
        const sidebarElement = document.querySelector(sidebarSelector);
        const desktopQuery = window.matchMedia('(min-width: 992px)');

        if ($sidebarToggle.length && sidebarElement && window.Mmenu) {
            const resolveInitialState = () => {
                const stored = window.sessionStorage?.getItem('mmenuExpandedState');
                if (stored === 'open' || stored === 'closed') {
                    return stored;
                }

                return $(sidebarElement).data('expanded') !== false ? 'open' : 'closed';
            };

            const menu = new window.Mmenu(sidebarSelector, {
                extensions: ['border-none', 'shadow-page', 'pagedim-black', 'position-front'],
                setSelected: true,
                slidingSubmenus: true,
                navbar: {
                    title: 'Menu',
                },
                navbars: [{
                    position: 'top',
                    content: ['prev', 'title', 'close'],
                }],
            }, {
                offCanvas: {
                    position: 'left',
                },
                sidebar: {
                    collapsed: {
                        use: true,
                    },
                    expanded: {
                        use: desktopQuery.media,
                        initial: resolveInitialState(),
                    },
                },
            });

            const sidebarApi = menu.API;
            const wrapperElement = menu.node.wrpr || document.body;

            const persistExpandedState = (state) => {
                try {
                    if (desktopQuery.matches) {
                        window.sessionStorage?.setItem('mmenuExpandedState', state);
                    }
                } catch (e) {
                    console.warn('Não foi possível persistir o estado do menu', e);
                }
            };

            const syncToggleState = () => {
                const isExpanded = wrapperElement.classList.contains('mm-wrapper--sidebar-expanded');
                $sidebarToggle.attr('aria-expanded', isExpanded.toString());
                persistExpandedState(isExpanded ? 'open' : 'closed');
            };

            syncToggleState();

            sidebarApi.bind('open:after', function () {
                syncToggleState();
            });

            sidebarApi.bind('close:after', function () {
                syncToggleState();
            });

            $sidebarToggle.on('click', function (e) {
                e.preventDefault();
                const isExpanded = wrapperElement.classList.contains('mm-wrapper--sidebar-expanded');
                const action = isExpanded && desktopQuery.matches ? 'close' : 'open';
                sidebarApi[action]();
            });

            desktopQuery.addEventListener('change', (event) => {
                window.requestAnimationFrame(syncToggleState);
            });
        }
    });
})();
