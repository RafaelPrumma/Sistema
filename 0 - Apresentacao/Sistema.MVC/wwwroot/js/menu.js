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
      openClass: 'open',
      openStateKey: 'menuOpenBranches',
      ...options
    };

    const $menu = $(config.menuSelector);
    if (!$menu.length) return null;

    const $hamburger = $(config.hamburgerSelector);
    const $backdrop = $(config.backdropSelector);
    const $checkbox = $(config.expandedCheckboxSelector);
    const desktop = window.matchMedia('(min-width: 992px)');

    // ---- Identidade estável de cada ramo (para persistir estado aberto) -----
    const branchKey = ($branch) => {
      const $toggle = $branch.children('.menu-toggle').first();
      const label = $toggle.find('> span').first().text().trim();
      const depth = $branch.attr('data-menu-depth') || '0';
      // Inclui os rótulos dos ancestrais para desambiguar rótulos repetidos.
      const trail = $branch.parents('.menu-branch')
        .map(function () { return $(this).children('.menu-toggle').first().find('> span').first().text().trim(); })
        .get()
        .reverse()
        .join(' / ');
      return (trail ? trail + ' / ' : '') + label + '#' + depth;
    };

    const readOpenState = () => {
      try {
        const raw = window.sessionStorage.getItem(config.openStateKey);
        return raw ? new Set(JSON.parse(raw)) : null;
      } catch {
        return null;
      }
    };

    const writeOpenState = () => {
      try {
        const keys = [];
        $menu.find('.menu-branch.' + config.openClass).each(function () {
          keys.push(branchKey($(this)));
        });
        window.sessionStorage.setItem(config.openStateKey, JSON.stringify(keys));
      } catch {
        // Storage indisponível em contextos privados/bloqueados; segue sem persistir.
      }
    };

    // ---- Expansão/recolhimento de um ramo (accordion recursivo) -------------
    const setBranchOpen = ($branch, open) => {
      const $toggle = $branch.children('.menu-toggle').first();
      $branch.toggleClass(config.openClass, open);
      $toggle.attr('aria-expanded', open ? 'true' : 'false');
    };

    const toggleBranch = ($branch) => {
      const willOpen = !$branch.hasClass(config.openClass);
      setBranchOpen($branch, willOpen);
      // Ao fechar um ramo, recolhe também os descendentes para um reabrir limpo.
      if (!willOpen) {
        $branch.find('.menu-branch.' + config.openClass).each(function () {
          setBranchOpen($(this), false);
        });
      }
      writeOpenState();
    };

    // Garante que a trilha do item ativo esteja aberta (ancestrais do ativo).
    const expandActiveTrail = () => {
      const $active = $menu.find('.menu-leaf .menu-link.active').first();
      if (!$active.length) return;
      $active.parents('.menu-branch').each(function () {
        setBranchOpen($(this), true);
      });
    };

    // Restaura ramos abertos da sessão anterior; se não houver estado salvo,
    // mantém apenas o que veio do servidor (trilha ativa já marcada com .open).
    const restoreOpenState = () => {
      const saved = readOpenState();
      if (saved) {
        $menu.find('.menu-branch').each(function () {
          const $branch = $(this);
          setBranchOpen($branch, saved.has(branchKey($branch)));
        });
      }
      expandActiveTrail();
    };

    const collapseAllBranches = () => {
      $menu.find('.menu-branch.' + config.openClass).each(function () {
        setBranchOpen($(this), false);
      });
    };

    // ---- Estado desktop (recolhido/expandido) e mobile (aberto/fechado) -----
    const rememberExpandedState = (expanded) => {
      const value = expanded ? 'true' : 'false';
      $menu.data('menu-expanded', value);
      $menu.attr('data-menu-expanded', value);
      try {
        window.sessionStorage.setItem('menuExpandedState', expanded ? 'open' : 'closed');
      } catch {
        // Storage pode estar indisponível.
      }
    };

    const setDesktopExpanded = (expanded) => {
      document.body.classList.toggle(config.collapsedClass, !expanded);
      $hamburger.attr('aria-expanded', expanded ? 'true' : 'false');
      $checkbox.prop('checked', expanded);
      rememberExpandedState(expanded);
    };

    const setMobileOpen = (open) => {
      document.body.classList.toggle(config.mobileOpenClass, open);
      $hamburger.attr('aria-expanded', open ? 'true' : 'false');
    };

    const sync = (desktopExpanded) => {
      if (desktop.matches) {
        $checkbox.prop('disabled', false);
        setDesktopExpanded(Boolean(desktopExpanded));
        document.body.classList.remove(config.mobileOpenClass);
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
          setDesktopExpanded(isCollapsed);
        } else {
          const isOpen = document.body.classList.contains(config.mobileOpenClass);
          setMobileOpen(!isOpen);
        }
      });

      // Clique num toggle de ramo: expande/recolhe o próprio ramo (qualquer nível).
      $menu.on('click', '.menu-toggle', function (e) {
        e.preventDefault();
        e.stopPropagation();
        toggleBranch($(this).closest('.menu-branch'));
      });

      // Ao seguir um link folha no mobile, fecha o off-canvas.
      $menu.on('click', '.menu-leaf .menu-link', function () {
        if (!desktop.matches) {
          setMobileOpen(false);
        }
      });

      $backdrop.on('click', function () {
        setMobileOpen(false);
      });

      $(document).on('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if (!desktop.matches && document.body.classList.contains(config.mobileOpenClass)) {
          setMobileOpen(false);
        }
      });

      $checkbox.on('change', function () {
        if (!desktop.matches) return;
        setDesktopExpanded(this.checked);
      });

      desktop.addEventListener('change', () => {
        document.body.classList.remove(config.mobileOpenClass);
        document.body.classList.remove(config.collapsedClass);
        const saved = $menu.data('menu-expanded');
        sync(saved === true || saved === 'true');
      });
    };

    bindEvents();
    restoreOpenState();

    return {
      sync,
      closeMobile() { setMobileOpen(false); },
      collapseAll: collapseAllBranches
    };
  };

})(window);
