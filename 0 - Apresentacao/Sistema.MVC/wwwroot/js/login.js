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
                setTimeout(() => { window.location.href = '/'; }, 1500);
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
});
