// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function showSuccess(message) {
    iziToast.success({ title: 'Sucesso', message });
}

function showError(message) {
    iziToast.error({ title: 'Erro', message });
}

function showWarning(message) {
    iziToast.warning({ title: 'Alerta', message });
}

function showInfo(message) {
    iziToast.info({ title: 'Info', message });
}
