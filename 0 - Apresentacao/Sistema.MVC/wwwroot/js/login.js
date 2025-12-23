(function () {
    'use strict';

    const $ = window.jQuery;
    if (!$) {
        console.error('jQuery é obrigatório para login.js');
        return;
    }

    $(function () {
        const $modal = $('#loadingModal');

        $('#loginForm').on('submit', function () {
            if ($modal.length && typeof $modal.iziModal === 'function') {
                $modal.iziModal({ title: 'Aguarde', subtitle: 'Autenticando...', close: false });
                $modal.iziModal('open');
            }

            if (window.showInfo) {
                window.showInfo('Enviando dados');
            }
        });

        $('#registerLink').on('click', function (e) {
            e.preventDefault();
            const $register = $('#registerModal');
            if ($register.length && typeof $register.iziModal === 'function') {
                $register.iziModal({ title: 'Cadastro' });
                $register.iziModal('open');
            }
        });

        $('#forgotLink').on('click', function (e) {
            e.preventDefault();
            const $forgot = $('#forgotModal');
            if ($forgot.length && typeof $forgot.iziModal === 'function') {
                $forgot.iziModal({ title: 'Recuperar senha' });
                $forgot.iziModal('open');
            }
        });

        $('#registerForm').on('submit', async function (e) {
            e.preventDefault();
            const registerData = {
                nome: $('#RegNome').val(),
                cpf: $('#RegCpf').val(),
                email: $('#RegEmail').val(),
                senha: $('#RegSenha').val()
            };
            try {
                const response = await fetch('/Account/Register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(registerData)
                });
                if (response.ok) {
                    const $register = $('#registerModal');
                    if ($register.length && typeof $register.iziModal === 'function') {
                        $register.iziModal('close');
                    }
                    if (window.showSuccess) {
                        window.showSuccess('Cadastro realizado');
                    }
                } else if (window.showError) {
                    window.showError('Erro ao cadastrar');
                }
            } catch (error) {
                if (window.showError) {
                    window.showError('Falha na comunicação');
                }
                console.error('Erro ao cadastrar.', error);
            }
        });

        $('#forgotForm').on('submit', async function (e) {
            e.preventDefault();
            const forgotData = {
                cpf: $('#RecCpf').val(),
                email: $('#RecEmail').val()
            };
            try {
                const response = await fetch('/Account/RecuperarSenha', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(forgotData)
                });
                if (response.ok) {
                    const $forgot = $('#forgotModal');
                    if ($forgot.length && typeof $forgot.iziModal === 'function') {
                        $forgot.iziModal('close');
                    }
                    if (window.showSuccess) {
                        window.showSuccess('Email enviado');
                    }
                } else if (response.status === 404) {
                    if (window.showError) {
                        window.showError('Usuário não encontrado');
                    }
                } else if (window.showError) {
                    window.showError('Erro ao enviar email');
                }
            } catch (error) {
                if (window.showError) {
                    window.showError('Falha na comunicação');
                }
                console.error('Erro ao enviar email.', error);
            }
        });
    });
})();
