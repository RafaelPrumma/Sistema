(function () {
    'use strict';

    const $ = window.jQuery;
    if (!$) {
        console.error('jQuery é obrigatório para site.js');
        return;
    }

    window.$ = $;
    window.jQuery = $;

    const brinde = () => window.iziToast;

    function notificar(tipo, titulo, mensagem) {
        const instancia = brinde();
        if (instancia && typeof instancia[tipo] === 'function') {
            instancia[tipo]({ title: titulo, message: mensagem });
        } else {
            const prefixo = titulo ? `${titulo}: ` : '';
            console.log(`${prefixo}${mensagem}`);
        }
    }

    window.mostrarSucesso = function (mensagem) {
        notificar('success', 'Sucesso', mensagem);
    };

    window.mostrarErro = function (mensagem) {
        notificar('error', 'Erro', mensagem);
    };

    window.mostrarAlerta = function (mensagem) {
        notificar('warning', 'Alerta', mensagem);
    };

    window.mostrarInfo = function (mensagem) {
        notificar('info', 'Info', mensagem);
    };

    // Helpers de modal no padrão iziModal (init sob demanda, lendo título/ícone do data-attribute).
    window.abrirModal = function (id, opts) {
        var $m = $('#' + id);
        if (!$m.length || typeof $m.iziModal !== 'function') return;
        if (!$m.data('uiInit')) {
            $m.iziModal(opts || {
                title: $m.data('titulo') || '',
                icon: $m.data('icone') || '',
                width: $m.data('largura') || 600
            });
            $m.data('uiInit', true);
        }
        $m.iziModal('open');
    };

    window.fecharModal = function (id) {
        var $m = $('#' + id);
        if ($m.length && typeof $m.iziModal === 'function') $m.iziModal('close');
    };

    // Abre modais por delegação: qualquer elemento com data-abrir-modal="idDoModal".
    $(document).on('click', '[data-abrir-modal]', function (e) {
        e.preventDefault();
        window.abrirModal(this.getAttribute('data-abrir-modal'));
    });

    $(function () {
        const splash = document.getElementById('appSplash');
        const hideSplash = () => {
            if (!splash || splash.classList.contains('app-splash--hide')) return;
            splash.classList.add('app-splash--hide');
            try {
                window.sessionStorage.setItem('appSplashSeen', 'true');
            } catch {
                // A aplicação continua funcional quando o storage estiver indisponível.
            }
            document.documentElement.classList.add('splash-seen');
            window.setTimeout(() => splash.remove(), 320);
        };
        const sidebarMenu = document.getElementById('sidebarMenu');
        const initialMenuExpanded = sidebarMenu?.dataset.menuExpanded === 'true';
        const menuController = typeof window.initSistemaMenu === 'function'
            ? window.initSistemaMenu({
                menuSelector: '#sidebarMenu',
                hamburgerSelector: '.menu-hamburger',
                expandedCheckboxSelector: '#MenuLateralExpandido'
            })
            : null;

        if (sidebarMenu && !menuController) {
            console.warn('initSistemaMenu não foi encontrado. O menu lateral não foi inicializado.');
        }

        if (typeof window.initSistemaTheme === 'function') {
            window.initSistemaTheme({
                menuController,
                initialMenuExpanded,
                notify: notificar
            });
        } else {
            menuController?.sync(initialMenuExpanded);
            document.body.classList.add('theme-ready');
            console.warn('initSistemaTheme não foi encontrado. O tema não foi inicializado.');
        }

        if (window.AOS && typeof window.AOS.init === 'function') {
            window.AOS.init({
                duration: 320,
                easing: 'ease-out',
                once: true,
            });
        }

        if (document.documentElement.classList.contains('splash-seen')) {
            splash?.remove();
        } else {
            window.addEventListener('load', () => window.setTimeout(hideSplash, 120), { once: true });
            window.setTimeout(hideSplash, 1400);
        }
    });
})();
