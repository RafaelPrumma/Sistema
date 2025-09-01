import '../css/login.css';

$(function () {
    $('#loginForm').on('submit', async function (e) {
        e.preventDefault();
        const modal = $('#loadingModal');
        modal.iziModal({ title: 'Aguarde', subtitle: 'Autenticando...', close: false });
        modal.iziModal('open');
        showInfo('Enviando dados');

        const data = {
            cpf: $('#Cpf').val(),
            senha: $('#Senha').val()
        };

        try {
            const response = await fetch('/Account/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            modal.iziModal('close');

            if (response.ok) {
                showSuccess('Login realizado');
                setTimeout(() => { window.location.href = '/Home/Index'; }, 1500);
            } else if (response.status === 400) {
                showWarning('Preencha os campos corretamente');
            } else {
                let msg = 'Credenciais inválidas';
                try {
                    const err = await response.json();
                    if (err.message) msg = err.message;
                } catch { }
                showError(msg);
            }
        } catch {
            modal.iziModal('close');
            showError('Falha na comunicação');
        }
    });

    $('#registerLink').on('click', function (e) {
        e.preventDefault();
        $('#registerModal').iziModal({ title: 'Cadastro' });
        $('#registerModal').iziModal('open');
    });

    $('#forgotLink').on('click', function (e) {
        e.preventDefault();
        $('#forgotModal').iziModal({ title: 'Recuperar senha' });
        $('#forgotModal').iziModal('open');
    });

    $('#registerForm').on('submit', async function (e) {
        e.preventDefault();
        const data = {
            nome: $('#RegNome').val(),
            cpf: $('#RegCpf').val(),
            email: $('#RegEmail').val(),
            senha: $('#RegSenha').val()
        };
        try {
            const response = await fetch('/Account/Register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            if (response.ok) {
                $('#registerModal').iziModal('close');
                showSuccess('Cadastro realizado');
            } else {
                showError('Erro ao cadastrar');
            }
        } catch {
            showError('Falha na comunicação');
        }
    });

    $('#forgotForm').on('submit', async function (e) {
        e.preventDefault();
        const data = {
            cpf: $('#RecCpf').val(),
            email: $('#RecEmail').val()
        };
        try {
            const response = await fetch('/Account/RecuperarSenha', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            if (response.ok) {
                $('#forgotModal').iziModal('close');
                showSuccess('Email enviado');
            } else if (response.status === 404) {
                showError('Usuário não encontrado');
            } else {
                showError('Erro ao enviar email');
            }
        } catch {
            showError('Falha na comunicação');
        }
    });
});
