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

            if (typeof initialMenuExpanded === 'boolean') {
                window.sessionStorage.setItem(expandedStateKey, initialMenuExpanded ? 'open' : 'closed');
            }

            const mmenu = new window.Mmenu('#sidebarMenu', {
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
