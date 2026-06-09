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

    $(function () {
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
                duration: 750,
                easing: 'ease-out-quart',
                once: true,
            });
        }
    });
})();
