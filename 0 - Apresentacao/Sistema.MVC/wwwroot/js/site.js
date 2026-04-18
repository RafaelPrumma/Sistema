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
        const root = document.documentElement;
        const serverTheme = document.body.getAttribute('data-bs-theme') || 'light';
        const persistedTheme = localStorage.getItem('theme');
        const $headerEl = $('header.navbar');
        const $footerEl = $('footer.footer');
        const expandedStateKey = 'menuExpandedState';

        const classesTexto = ['text-white', 'text-dark'];
        const obterPreferenciaMenuExpandido = () => $('#MenuLateralExpandido').is(':checked');

        const atualizarEspacamentoHeader = () => {
            if (!$headerEl.length) return;

            const headerFixo = $headerEl.hasClass('fixed-top');
            const alturaHeader = headerFixo ? Math.ceil($headerEl.outerHeight() || $headerEl[0]?.offsetHeight || 0) : 0;

            document.body.style.paddingTop = headerFixo && alturaHeader ? `${alturaHeader}px` : '';
            root.style.setProperty('--layout-offset-top', headerFixo && alturaHeader ? `${alturaHeader}px` : '0px');
        };

        const atualizarEspacamentoFooter = () => {
            if (!$footerEl.length) return;

            const footerFixo = $footerEl.hasClass('fixed-bottom');
            const alturaFooter = footerFixo ? Math.ceil($footerEl.outerHeight() || $footerEl[0]?.offsetHeight || 0) : 0;

            document.body.style.paddingBottom = footerFixo && alturaFooter ? `${alturaFooter}px` : '';
            root.style.setProperty('--layout-offset-bottom', footerFixo && alturaFooter ? `${alturaFooter}px` : '0px');
        };

        const normalizarHex = (valor) => {
            if (!valor) return null;
            const cor = valor.trim();

            if (cor.startsWith('#')) {
                let hex = cor.slice(1);
                if (hex.length === 3) {
                    hex = hex.split('').map((c) => c + c).join('');
                }
                return hex.length === 6 ? hex : null;
            }

            const rgb = cor.match(/^rgba?\((\d+),\s*(\d+),\s*(\d+)/i);
            if (rgb) {
                const [r, g, b] = rgb.slice(1, 4).map((v) => parseInt(v, 10));
                const toHex = (v) => v.toString(16).padStart(2, '0');
                return `${toHex(r)}${toHex(g)}${toHex(b)}`;
            }

            return null;
        };

        const obterClasseTexto = (valor) => {
            const hex = normalizarHex(valor);
            if (!hex) return 'text-dark';

            const r = parseInt(hex.slice(0, 2), 16);
            const g = parseInt(hex.slice(2, 4), 16);
            const b = parseInt(hex.slice(4, 6), 16);
            const luminosidade = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            return luminosidade > 0.5 ? 'text-dark' : 'text-white';
        };

        const aplicarClasseTexto = ($elementos, classe) => {
            if (!$elementos?.length) return;
            $elementos.removeClass(classesTexto.join(' ')).addClass(classe);
        };

        const atualizarContraste = (cores) => {
            const headerClasse = obterClasseTexto(cores.headerColor);
            const esquerdaClasse = obterClasseTexto(cores.leftColor);
            const direitaClasse = obterClasseTexto(cores.rightColor);
            const footerClasse = obterClasseTexto(cores.footerColor);

            if ($headerEl.length) {
                aplicarClasseTexto($headerEl, headerClasse);
                aplicarClasseTexto($headerEl.find('.navbar-brand, .nav-link, .btn-link'), headerClasse);
                $headerEl.removeClass('navbar-dark navbar-light').addClass(headerClasse === 'text-white' ? 'navbar-dark' : 'navbar-light');
            }

            const $menu = $('#sidebarMenu');
            aplicarClasseTexto($menu, esquerdaClasse);
            aplicarClasseTexto($menu.find('.nav-link, .nav-header, .menu-brand-text *, .menu-user *'), esquerdaClasse);

            const $temaPainel = $('#temaSidebar');
            aplicarClasseTexto($temaPainel, direitaClasse);
            aplicarClasseTexto($temaPainel.find('h5, label, .form-check-label, .btn'), direitaClasse);

            if ($footerEl.length) {
                aplicarClasseTexto($footerEl, footerClasse);
                aplicarClasseTexto($footerEl.find('a'), footerClasse);
            }
        };

        const aplicarTema = (tema) => {
            const computed = getComputedStyle(root);
            const mode = typeof tema?.modoEscuro === 'boolean'
                ? (tema.modoEscuro ? 'dark' : 'light')
                : (document.body.getAttribute('data-bs-theme') || 'light');

            document.body.setAttribute('data-bs-theme', mode);
            root.style.colorScheme = mode;
            localStorage.setItem('theme', mode);

            const headerColor = tema?.corHeader ?? computed.getPropertyValue('--header-bg').trim();
            const leftColor = tema?.corBarraEsquerda ?? computed.getPropertyValue('--sidebar-bg').trim();
            const rightColor = tema?.corBarraDireita ?? computed.getPropertyValue('--rightbar-bg').trim();
            const footerColor = tema?.corFooter ?? computed.getPropertyValue('--footer-bg').trim();

            if (headerColor) root.style.setProperty('--header-bg', headerColor);
            if (leftColor) root.style.setProperty('--sidebar-bg', leftColor);
            if (rightColor) root.style.setProperty('--rightbar-bg', rightColor);
            if (footerColor) root.style.setProperty('--footer-bg', footerColor);

            atualizarContraste({ headerColor, leftColor, rightColor, footerColor });

            const headerFixed = typeof tema?.headerFixo === 'boolean' ? tema.headerFixo : $headerEl.hasClass('fixed-top');
            const footerFixed = typeof tema?.footerFixo === 'boolean' ? tema.footerFixo : $footerEl.hasClass('fixed-bottom');

            if ($headerEl.length) {
                $headerEl.toggleClass('fixed-top', headerFixed);
                atualizarEspacamentoHeader();
                $('#HeaderFixo').prop('checked', headerFixed);
            }

            if ($footerEl.length) {
                $footerEl.toggleClass('fixed-bottom', footerFixed).toggleClass('mt-auto', !footerFixed);
                atualizarEspacamentoFooter();
                $('#FooterFixo').prop('checked', footerFixed);
            }

            if (typeof tema?.modoEscuro === 'boolean') {
                $(`input[name="ModoEscuro"][value="${tema.modoEscuro}"]`).prop('checked', true);
            }

            if (tema?.corHeader) $('#CorHeader').val(tema.corHeader);
            if (tema?.corBarraEsquerda) $('#CorBarraEsquerda').val(tema.corBarraEsquerda);
            if (tema?.corBarraDireita) $('#CorBarraDireita').val(tema.corBarraDireita);
            if (tema?.corFooter) $('#CorFooter').val(tema.corFooter);

            if (typeof tema?.menuLateralExpandido === 'boolean') {
                $('#MenuLateralExpandido').prop('checked', tema.menuLateralExpandido);
                window.sessionStorage.setItem(expandedStateKey, tema.menuLateralExpandido ? 'open' : 'closed');
            }

            atualizarEstadoMenuResponsivo();

        };

        const sidebarMenu = document.getElementById('sidebarMenu');
        const initialMenuExpanded = sidebarMenu?.dataset.menuExpanded === 'true';
        const initialTheme = {
            modoEscuro: serverTheme === 'dark',
            menuLateralExpandido: initialMenuExpanded
        };

        const menuController = typeof window.initSistemaMenu === 'function'
            ? window.initSistemaMenu({
                sidebarSelector: '#sidebarMenu',
                mobileToggleSelector: '.mobile-menu-toggle',
                appShellSelector: '.app-shell',
                expandedCheckboxSelector: '#MenuLateralExpandido',
                onMobileStateChange: null
            })
            : null;

        const atualizarEstadoMenuResponsivo = () => {
            const shouldOpen = obterPreferenciaMenuExpandido();
            menuController?.sync(shouldOpen);
        };

        const $temaToggle = $('#temaToggle');
        const $temaSidebar = $('#temaSidebar');
        const $temaBackdrop = $('#temaSidebarBackdrop');
        const $temaClose = $('#temaSidebarClose');

        const abrirPainelTema = () => {
            $temaSidebar.addClass('show').attr('aria-hidden', 'false');
            $temaBackdrop.addClass('show');
            document.body.classList.add('tema-sidebar-open');
        };

        const fecharPainelTema = () => {
            $temaSidebar.removeClass('show').attr('aria-hidden', 'true');
            $temaBackdrop.removeClass('show');
            document.body.classList.remove('tema-sidebar-open');
        };

        if ($temaToggle.length && $temaSidebar.length && $temaBackdrop.length) {
            $temaToggle.on('click', function (e) {
                e.preventDefault();
                abrirPainelTema();
            });

            $temaClose.on('click', function () {
                fecharPainelTema();
            });

            $temaBackdrop.on('click', function () {
                fecharPainelTema();
            });

            $(document).on('keydown', function (e) {
                if (e.key === 'Escape') {
                    fecharPainelTema();
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

        if (typeof initialMenuExpanded === 'boolean') {
            window.sessionStorage.setItem(expandedStateKey, initialMenuExpanded ? 'open' : 'closed');
        }

        if (sidebarMenu && !menuController) {
            console.warn('initSistemaMenu não foi encontrado. O menu lateral não foi inicializado.');
        }

        if (persistedTheme === 'light' || persistedTheme === 'dark') {
            document.body.setAttribute('data-bs-theme', persistedTheme);
            root.style.colorScheme = persistedTheme;
        }

        aplicarTema(initialTheme);
        atualizarEstadoMenuResponsivo();
        atualizarEspacamentoHeader();
        atualizarEspacamentoFooter();
        document.body.classList.add('theme-ready');

        $('input[name="ModoEscuro"]').on('change', function () {
            aplicarTema({ modoEscuro: $(this).val() === 'true' });
        });

        const $headerInput = $('#CorHeader');
        if ($headerInput.length) {
            $headerInput.on('input', function () {
                aplicarTema({ corHeader: $(this).val() });
            });
        }

        const $leftInput = $('#CorBarraEsquerda');
        if ($leftInput.length) {
            $leftInput.on('input', function () {
                aplicarTema({ corBarraEsquerda: $(this).val() });
            });
        }

        const $rightInput = $('#CorBarraDireita');
        if ($rightInput.length) {
            $rightInput.on('input', function () {
                aplicarTema({ corBarraDireita: $(this).val() });
            });
        }

        const $footerInput = $('#CorFooter');
        if ($footerInput.length) {
            $footerInput.on('input', function () {
                aplicarTema({ corFooter: $(this).val() });
            });
        }

        const $headerFix = $('#HeaderFixo');
        if ($headerFix.length && $headerEl.length) {
            $headerFix.on('change', function () {
                aplicarTema({ headerFixo: this.checked });
            });
        }

        const $footerFix = $('#FooterFixo');
        if ($footerFix.length && $footerEl.length) {
            $footerFix.on('change', function () {
                aplicarTema({ footerFixo: this.checked });
            });
        }

        const $menuExpandido = $('#MenuLateralExpandido');
        if ($menuExpandido.length) {
            $menuExpandido.on('change', function () {
                aplicarTema({ menuLateralExpandido: this.checked });
            });
        }

        const $temaForm = $('#temaSidebar form');
        if ($temaForm.length) {
            $temaForm.on('submit', function (e) {
                e.preventDefault();
                const form = this;
                const formData = new FormData(form);

                fetch(form.action, {
                    method: form.method || 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error('Falha ao salvar tema');
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (!data?.success) {
                            throw new Error('Falha ao salvar tema');
                        }

                        if (data.theme) {
                            aplicarTema({
                                modoEscuro: data.theme.modoEscuro,
                                corHeader: data.theme.corHeader,
                                corBarraEsquerda: data.theme.corBarraEsquerda,
                                corBarraDireita: data.theme.corBarraDireita,
                                corFooter: data.theme.corFooter,
                                headerFixo: data.theme.headerFixo,
                                footerFixo: data.theme.footerFixo,
                                menuLateralExpandido: data.theme.menuLateralExpandido
                            });
                        }

                        notificar('success', 'Sucesso', 'Preferências de tema salvas.');
                        fecharPainelTema();
                    })
                    .catch(err => {
                        console.error(err);
                        notificar('error', 'Erro', 'Não foi possível salvar o tema.');
                    });
            });
        }

        let resizeRaf = null;
        window.addEventListener('resize', () => {
            if (resizeRaf) {
                cancelAnimationFrame(resizeRaf);
            }

            resizeRaf = window.requestAnimationFrame(() => {
                resizeRaf = null;
                atualizarEspacamentoHeader();
                atualizarEspacamentoFooter();
                atualizarEstadoMenuResponsivo();
            });
        });

        window.addEventListener('load', () => {
            atualizarEspacamentoHeader();
            atualizarEspacamentoFooter();
        });

        if (window.ResizeObserver) {
            const observador = new window.ResizeObserver(() => {
                atualizarEspacamentoHeader();
                atualizarEspacamentoFooter();
            });

            if ($headerEl.length) observador.observe($headerEl[0]);
            if ($footerEl.length) observador.observe($footerEl[0]);
        }

    });
})();
