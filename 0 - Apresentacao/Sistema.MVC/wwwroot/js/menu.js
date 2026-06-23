(function (window) {
  'use strict';

  const $ = window.jQuery;
  if (!$) {
    console.error('menu.js: jQuery é obrigatório');
    return;
  }

  window.initSistemaMenu = function initSistemaMenu(options) {

    const config = {
      menuSelector: '#sidebarMenu',
      hamburgerSelector: '.menu-hamburger',
      backdropSelector: '#menuBackdrop',
      expandedCheckboxSelector: '#MenuLateralExpandido',
      collapsedClass: 'menu-collapsed',
      mobileOpenClass: 'menu-mobile-open',
      ...options
    };

    const $menu = $(config.menuSelector);
    if (!$menu.length) return null;

    const $hamburger = $(config.hamburgerSelector);
    const $backdrop = $(config.backdropSelector);
    const $checkbox = $(config.expandedCheckboxSelector);
    const desktop = window.matchMedia('(min-width: 992px)');

    let activePanelId = null;

    const findCurrentRoutePanelId = () => {
      const $activeSubLink = $menu.find('.menu-panel--sub .menu-sublink.active').first();

      if ($activeSubLink.length) {
        return $activeSubLink.closest('.menu-panel--sub').attr('id') || null;
      }

      const fallbackPanelId = $menu.find('.menu-panel--root .menu-toggle.active')
        .first()
        .closest('.menu-item')
        .data('panel');

      return fallbackPanelId || null;
    };

    const rememberExpandedState = (expanded) => {
      const value = expanded ? 'true' : 'false';
      $menu.data('menu-expanded', value);
      $menu.attr('data-menu-expanded', value);
      try {
        window.sessionStorage.setItem('menuExpandedState', expanded ? 'open' : 'closed');
      } catch {
        // Storage can be unavailable in private or locked-down browser contexts.
      }
    };

    const showPanel = (panelId) => {
      const $root = $menu.find('.menu-panel--root');
      const $sub = $menu.find('#' + panelId);
      if (!$sub.length) return;

      $menu.find('.menu-panel--sub').each(function () {
        $(this).removeClass('panel-slide-in');
      });

      $root.addClass('panel-slide-out');
      $sub.addClass('panel-slide-in');
      activePanelId = panelId;
    };

    const showRoot = () => {
      $menu.find('.menu-panel--root').removeClass('panel-slide-out');
      $menu.find('.menu-panel--sub').removeClass('panel-slide-in');
      activePanelId = null;
    };

    const showCurrentRoutePanel = () => {
      const panelId = findCurrentRoutePanelId();

      if (panelId) {
        showPanel(panelId);
      }
    };

    const setDesktopExpanded = (expanded) => {
      document.body.classList.toggle(config.collapsedClass, !expanded);
      $hamburger.attr('aria-expanded', expanded ? 'true' : 'false');
      $checkbox.prop('checked', expanded);
      rememberExpandedState(expanded);
      if (!expanded) showRoot();
    };

    const setMobileOpen = (open) => {
      document.body.classList.toggle(config.mobileOpenClass, open);
      $hamburger.attr('aria-expanded', open ? 'true' : 'false');

      if (open) {
        showCurrentRoutePanel();
      } else {
        showRoot();
      }
    };

    const sync = (desktopExpanded) => {
      if (desktop.matches) {
        $checkbox.prop('disabled', false);
        const expanded = Boolean(desktopExpanded);
        setDesktopExpanded(expanded);
        document.body.classList.remove(config.mobileOpenClass);

        if (expanded) {
          showCurrentRoutePanel();
        }
      } else {
        $checkbox.prop('disabled', true);
        setMobileOpen(false);
        document.body.classList.remove(config.collapsedClass);
      }
    };

    const bindEvents = () => {
      $hamburger.on('click', function (e) {
        e.preventDefault();

        if (desktop.matches) {
          const isCollapsed = document.body.classList.contains(config.collapsedClass);
          const willExpand = isCollapsed;
          setDesktopExpanded(willExpand);
        } else {
          const isOpen = document.body.classList.contains(config.mobileOpenClass);
          setMobileOpen(!isOpen);
        }
      });

      $menu.on('click', '.menu-arrow, .menu-toggle', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const panelId = $(this).closest('.menu-item').data('panel');
        if (!panelId) return;

        if (activePanelId === panelId) {
          showRoot();
        } else {
          showPanel(panelId);
        }
      });

      $menu.on('click', '.menu-link:not(.menu-toggle)', function (e) {
        const $item = $(this).closest('.menu-item');
        const panelId = $item.data('panel');
        if (!panelId) return;
        e.preventDefault();
        if (activePanelId === panelId) {
          showRoot();
        } else {
          showPanel(panelId);
        }
      });

      $menu.on('click', '.menu-back', function (e) {
        e.preventDefault();
        showRoot();
      });

      $backdrop.on('click', function () {
        setMobileOpen(false);
      });

      $(document).on('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if (!desktop.matches && document.body.classList.contains(config.mobileOpenClass)) {
          setMobileOpen(false);
          return;
        }
        if (activePanelId) {
          showRoot();
        }
      });

      $checkbox.on('change', function () {
        if (!desktop.matches) return;
        setDesktopExpanded(this.checked);
      });

      desktop.addEventListener('change', () => {
        showRoot();
        document.body.classList.remove(config.mobileOpenClass);
        document.body.classList.remove(config.collapsedClass);
        const saved = $menu.data('menu-expanded');
        sync(saved === true || saved === 'true');
      });
    };

    bindEvents();
    showCurrentRoutePanel();

    return {
      sync,
      showRoot,
      closeMobile() { setMobileOpen(false); }
    };
  };

})(window);
