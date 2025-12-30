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
        const $body = $(document.body);
        const $headerEl = $('header.navbar');
        const $footerEl = $('footer.footer');

        let mmApi = null;
        const expandedStateKey = 'mmenuExpandedState';

        const classesTexto = ['text-white', 'text-dark'];

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
            const $iconbar = $('.app-iconbar');
            aplicarClasseTexto($menu, esquerdaClasse);
            aplicarClasseTexto($menu.find('.nav-link, .nav-header, .menu-brand-text *, .menu-user *'), esquerdaClasse);
            aplicarClasseTexto($iconbar, esquerdaClasse);
            aplicarClasseTexto($iconbar.find('.icon-link'), esquerdaClasse);

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
                $body.toggleClass('pt-5', headerFixed);
                $('#HeaderFixo').prop('checked', headerFixed);
            }

            if ($footerEl.length) {
                $footerEl.toggleClass('fixed-bottom', footerFixed).toggleClass('mt-auto', !footerFixed);
                $body.toggleClass('pb-5', footerFixed);
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

                if (mmApi) {
                    if (tema.menuLateralExpandido) {
                        mmApi.open();
                    } else {
                        mmApi.close();
                    }
                }
            }

            if (mmApi) {
                mmApi.theme(mode === 'dark' ? 'dark' : 'light');
            }
        };

        const sidebarMenu = document.getElementById('sidebarMenu');
        const initialMenuExpanded = sidebarMenu?.dataset.menuExpanded === 'true';

        aplicarTema({ modoEscuro: serverTheme === 'dark', menuLateralExpandido: initialMenuExpanded });

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

        if (sidebarMenu && window.Mmenu) {
            const homeUrl = sidebarMenu.dataset.homeUrl || '/';
            const configUrl = sidebarMenu.dataset.configUrl || '/Configuracao/Index';
            const themeUrl = sidebarMenu.dataset.themeUrl || '/Tema/Edit';
            const logoutUrl = sidebarMenu.dataset.logoutUrl || '/Account/Logout';
            const isDarkMode = (document.body.getAttribute('data-bs-theme') || 'light') === 'dark';

            if (typeof initialMenuExpanded === 'boolean') {
                window.sessionStorage.setItem(expandedStateKey, initialMenuExpanded ? 'open' : 'closed');
            }

            const mmenu = new window.Mmenu('#sidebarMenu', {
                theme: isDarkMode ? 'dark' : 'light',
                iconPanels: {
                    add: true,
                    visible: 1
                },
                setSelected: {
                    hover: true,
                    parent: true,
                    current: true
                },
                backButton: {
                    close: true,
                    open: false
                },
                navbars: [
                    {
                        position: 'top',
                        content: ['prev', 'breadcrumbs']
                    }
                ]
            }, {
                offCanvas: {
                    use: true,
                    position: 'left'
                },
                sidebar: {
                    expanded: {
                        use: true,
                        initial: initialMenuExpanded ? 'open' : 'closed'
                    }
                },
                iconbar: {
                    use: true,
                    position: 'left',
                    top: [
                        `<a href="${homeUrl}" title="Home"><i class="bi bi-house"></i></a>`
                    ],
                    bottom: [
                        `<a href="${logoutUrl}" title="Sair"><i class="bi bi-box-arrow-right"></i></a>`,
                        `<a href="${configUrl}" title="Configurações"><i class="bi bi-gear"></i></a>`,
                        `<a href="${themeUrl}" title="Tema"><i class="bi bi-palette"></i></a>`
                    ]
                }
            });

            mmApi = mmenu.API;

            mmApi.bind('setSelected:after', () => {
                sidebarMenu.querySelectorAll('.mm-listitem__btn, .mm-listitem__text')
                    .forEach(el => el.classList.remove('hovering'));
            });

            sidebarMenu.addEventListener('mouseover', (ev) => {
                const target = ev.target;
                if (!(target instanceof HTMLElement)) return;
                const listItem = target.closest('.mm-listitem');
                if (listItem) {
                    mmApi.setSelected(listItem);
                }
            });

            aplicarTema({ menuLateralExpandido: initialMenuExpanded });
        }

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
                        $temaSidebar.removeClass('show');
                    })
                    .catch(err => {
                        console.error(err);
                        notificar('error', 'Erro', 'Não foi possível salvar o tema.');
                    });
            });
        }

    });
})();
