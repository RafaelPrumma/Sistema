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
        const serverTheme = document.body.getAttribute('data-bs-theme') || 'light';
        const $body = $(document.body);
        const $headerEl = $('header.navbar');
        const $footerEl = $('footer.footer');

        const applyTheme = (theme) => {
            const computed = getComputedStyle(root);
            const mode = typeof theme?.modoEscuro === 'boolean'
                ? (theme.modoEscuro ? 'dark' : 'light')
                : (document.body.getAttribute('data-bs-theme') || 'light');

            document.body.setAttribute('data-bs-theme', mode);
            root.style.colorScheme = mode;
            localStorage.setItem('theme', mode);

            const headerColor = theme?.corHeader ?? computed.getPropertyValue('--header-bg').trim();
            const leftColor = theme?.corBarraEsquerda ?? computed.getPropertyValue('--sidebar-bg').trim();
            const rightColor = theme?.corBarraDireita ?? computed.getPropertyValue('--rightbar-bg').trim();
            const footerColor = theme?.corFooter ?? computed.getPropertyValue('--footer-bg').trim();

            if (headerColor) root.style.setProperty('--header-bg', headerColor);
            if (leftColor) root.style.setProperty('--sidebar-bg', leftColor);
            if (rightColor) root.style.setProperty('--rightbar-bg', rightColor);
            if (footerColor) root.style.setProperty('--footer-bg', footerColor);

            const headerFixed = typeof theme?.headerFixo === 'boolean' ? theme.headerFixo : $headerEl.hasClass('fixed-top');
            const footerFixed = typeof theme?.footerFixo === 'boolean' ? theme.footerFixo : $footerEl.hasClass('fixed-bottom');

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

            if (typeof theme?.modoEscuro === 'boolean') {
                $(`input[name="ModoEscuro"][value="${theme.modoEscuro}"]`).prop('checked', true);
            }

            if (theme?.corHeader) $('#CorHeader').val(theme.corHeader);
            if (theme?.corBarraEsquerda) $('#CorBarraEsquerda').val(theme.corBarraEsquerda);
            if (theme?.corBarraDireita) $('#CorBarraDireita').val(theme.corBarraDireita);
            if (theme?.corFooter) $('#CorFooter').val(theme.corFooter);
        };

        applyTheme({ modoEscuro: serverTheme === 'dark' });

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

        const sidebarMenu = document.getElementById('sidebarMenu');
        if (sidebarMenu && window.Mmenu) {
            const homeUrl = sidebarMenu.dataset.homeUrl || '/';
            const configUrl = sidebarMenu.dataset.configUrl || '/Configuracao/Index';
            const themeUrl = sidebarMenu.dataset.themeUrl || '/Tema/Edit';
            const logoutUrl = sidebarMenu.dataset.logoutUrl || '/Account/Logout';

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
                offCanvas: false,
                sidebar: {
                    expanded: {
                        use: true,
                        initial: 'open'
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

            const api = mmenu.API;
            api.bind('setSelected:after', () => {
                sidebarMenu.querySelectorAll('.mm-listitem__btn, .mm-listitem__text')
                    .forEach(el => el.classList.remove('hovering'));
            });

            sidebarMenu.addEventListener('mouseover', (ev) => {
                const target = ev.target;
                if (!(target instanceof HTMLElement)) return;
                const listItem = target.closest('.mm-listitem');
                if (listItem) {
                    api.setSelected(listItem);
                }
            });
        }

        $('input[name="ModoEscuro"]').on('change', function () {
            applyTheme({ modoEscuro: $(this).val() === 'true' });
        });

        const $headerInput = $('#CorHeader');
        if ($headerInput.length) {
            $headerInput.on('input', function () {
                applyTheme({ corHeader: $(this).val() });
            });
        }

        const $leftInput = $('#CorBarraEsquerda');
        if ($leftInput.length) {
            $leftInput.on('input', function () {
                applyTheme({ corBarraEsquerda: $(this).val() });
            });
        }

        const $rightInput = $('#CorBarraDireita');
        if ($rightInput.length) {
            $rightInput.on('input', function () {
                applyTheme({ corBarraDireita: $(this).val() });
            });
        }

        const $footerInput = $('#CorFooter');
        if ($footerInput.length) {
            $footerInput.on('input', function () {
                applyTheme({ corFooter: $(this).val() });
            });
        }

        const $headerFix = $('#HeaderFixo');
        if ($headerFix.length && $headerEl.length) {
            $headerFix.on('change', function () {
                applyTheme({ headerFixo: this.checked });
            });
        }

        const $footerFix = $('#FooterFixo');
        if ($footerFix.length && $footerEl.length) {
            $footerFix.on('change', function () {
                applyTheme({ footerFixo: this.checked });
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
                            applyTheme({
                                modoEscuro: data.theme.modoEscuro,
                                corHeader: data.theme.corHeader,
                                corBarraEsquerda: data.theme.corBarraEsquerda,
                                corBarraDireita: data.theme.corBarraDireita,
                                corFooter: data.theme.corFooter,
                                headerFixo: data.theme.headerFixo,
                                footerFixo: data.theme.footerFixo
                            });
                        }

                        notify('success', 'Sucesso', 'Preferências de tema salvas.');
                        $temaSidebar.removeClass('show');
                    })
                    .catch(err => {
                        console.error(err);
                        notify('error', 'Erro', 'Não foi possível salvar o tema.');
                    });
            });
        }

    });
})();
