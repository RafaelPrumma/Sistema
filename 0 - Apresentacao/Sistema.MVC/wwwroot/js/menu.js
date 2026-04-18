(function (window) {
    'use strict';

    const $ = window.jQuery;
    if (!$) {
        console.error('jQuery é obrigatório para menu.js');
        return;
    }

    window.initSistemaMenu = function initSistemaMenu(options) {
        const config = {
            sidebarSelector: '#sidebarMenu',
            mobileToggleSelector: '.mobile-menu-toggle',
            iconbarSelector: '.app-iconbar',
            appShellSelector: '.app-shell',
            expandedCheckboxSelector: '#MenuLateralExpandido',
            onMobileStateChange: null,
            ...options
        };

        const $sidebar = $(config.sidebarSelector);
        if (!$sidebar.length) return null;

        const $mobileToggle = $(config.mobileToggleSelector);
        const $iconbar = $(config.iconbarSelector);
        const $appShell = $(config.appShellSelector);
        const $expandedCheckbox = $(config.expandedCheckboxSelector);
        const desktopMedia = window.matchMedia('(min-width: 992px)');

        let menuMobileAberto = false;

        const atualizarAriaMenu = (aberto) => {
            if (!$mobileToggle.length) return;
            $mobileToggle.attr('aria-controls', 'sidebarMenu');
            $mobileToggle.attr('aria-expanded', aberto ? 'true' : 'false');
        };

        const closeSubmenus = () => {
            $sidebar.removeClass('submenu-open');
            $sidebar.find('.submenu-active').removeClass('submenu-active');
            $sidebar.find('.submenu-toggle').attr('aria-expanded', 'false');
        };

        const notifyMobileState = () => {
            if (typeof config.onMobileStateChange === 'function') {
                config.onMobileStateChange(menuMobileAberto);
            }
        };

        const setMobileOpen = (aberto) => {
            menuMobileAberto = aberto;
            document.body.classList.toggle('menu-mobile-open', aberto);
            atualizarAriaMenu(aberto);
            if (!aberto) {
                closeSubmenus();
            }
            notifyMobileState();
        };

        const setDesktopExpanded = (expandido) => {
            document.body.classList.toggle('menu-desktop-collapsed', !expandido);
            if (expandido) {
                document.body.classList.remove('menu-hover-open');
            }
        };

        const syncResponsiveState = (desktopExpanded) => {
            if (desktopMedia.matches) {
                $expandedCheckbox.prop('disabled', false);
                setDesktopExpanded(desktopExpanded);
                setMobileOpen(false);
                return;
            }

            // Em telas pequenas não pode ficar expandido persistentemente.
            $expandedCheckbox.prop('disabled', true);
            setDesktopExpanded(true);
            setMobileOpen(false);
        };

        const ensureBackdrop = () => {
            if (document.getElementById('sidebarMenuBackdrop')) return;
            const backdrop = document.createElement('div');
            backdrop.id = 'sidebarMenuBackdrop';
            backdrop.className = 'sidebar-menu-backdrop';
            backdrop.setAttribute('aria-hidden', 'true');
            document.body.append(backdrop);
        };

        const buildSlidingSubmenus = () => {
            const $root = $sidebar.children('ul.nav').first();
            if (!$root.length) return;
            $root.addClass('menu-root');

            $root.children('li.nav-item').each(function (index) {
                const $item = $(this);
                const $submenu = $item.children('ul').first();
                const $link = $item.children('a').first();
                if (!$submenu.length || !$link.length) return;

                const submenuId = `submenu-panel-${index + 1}`;
                const titulo = ($link.text() || 'Submenu').trim();

                $submenu.attr('id', submenuId).addClass('menu-subpanel');
                const header = `
                    <li class="submenu-header">
                        <button type="button" class="submenu-back" aria-label="Voltar para menu principal">
                            <i class="bi bi-chevron-left"></i>
                            <span>Voltar</span>
                        </button>
                        <span class="submenu-title">${titulo}</span>
                    </li>`;
                $submenu.prepend(header);

                const $toggle = $(`
                    <button type="button" class="submenu-toggle" aria-label="Abrir submenu ${titulo}" aria-expanded="false" aria-controls="${submenuId}">
                        <i class="bi bi-chevron-right"></i>
                    </button>
                `);
                $item.append($toggle);
            });
        };

        const bindEvents = () => {
            $sidebar.on('click', '.submenu-toggle', function (e) {
                e.preventDefault();
                const $item = $(this).closest('.nav-item');
                const $submenu = $item.children('.menu-subpanel').first();
                if (!$submenu.length) return;

                closeSubmenus();
                $sidebar.addClass('submenu-open');
                $submenu.addClass('submenu-active');
                $(this).attr('aria-expanded', 'true');
            });

            $sidebar.on('click', '.submenu-back', function (e) {
                e.preventDefault();
                closeSubmenus();
            });

            $mobileToggle.on('click', function (e) {
                e.preventDefault();
                if (desktopMedia.matches) return;
                setMobileOpen(!menuMobileAberto);
            });

            $(document).on('click', '#sidebarMenuBackdrop', function () {
                setMobileOpen(false);
            });

            $(document).on('keydown', function (e) {
                if (e.key === 'Escape' && menuMobileAberto) {
                    setMobileOpen(false);
                }
            });

            // Quando recolhido no desktop, hover na barra de ícones abre menu completo.
            $iconbar.on('mouseenter', function () {
                if (!desktopMedia.matches) return;
                if (document.body.classList.contains('menu-desktop-collapsed')) {
                    document.body.classList.add('menu-hover-open');
                }
            });

            $appShell.on('mouseleave', function () {
                if (!desktopMedia.matches) return;
                document.body.classList.remove('menu-hover-open');
                closeSubmenus();
            });

            desktopMedia.addEventListener('change', () => {
                closeSubmenus();
                document.body.classList.remove('menu-hover-open');
            });
        };

        ensureBackdrop();
        buildSlidingSubmenus();
        bindEvents();
        atualizarAriaMenu(false);

        return {
            sync(desktopExpanded) {
                syncResponsiveState(Boolean(desktopExpanded));
            },
            closeMobile() {
                setMobileOpen(false);
            }
        };
    };
})(window);
