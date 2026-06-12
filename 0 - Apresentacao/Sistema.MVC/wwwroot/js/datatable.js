(function () {
  'use strict';

  const $ = window.jQuery;
  if (!$) {
    console.error('jQuery é obrigatório para datatable.js');
    return;
  }

  const PT_BR = {
    sEmptyTable: 'Nenhum registro encontrado',
    sInfo: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
    sInfoEmpty: 'Mostrando 0 a 0 de 0 registros',
    sInfoFiltered: '(filtrado de _MAX_ registros)',
    sLengthMenu: 'Mostrar _MENU_ registros',
    sLoadingRecords: 'Carregando...',
    sProcessing: 'Processando...',
    sSearch: 'Buscar:',
    sZeroRecords: 'Nenhum registro encontrado',
    oPaginate: { sFirst: 'Primeiro', sLast: 'Último', sNext: 'Próximo', sPrevious: 'Anterior' },
    oAria: { sSortAscending: ': ativar para ordenar (crescente)', sSortDescending: ': ativar para ordenar (decrescente)' }
  };

  // Inicializa um DataTable em modo server-side ligado ao contrato DataTablesResponse do backend.
  // options: { url, method?, columns, order?, pageLength?, lengthMenu?, searching? }
  // Cada coluna deve ter `data` (nome do campo no DTO, camelCase) que vira o `orderColumn` enviado.
  window.initDataTable = function (selector, options) {
    options = options || {};
    return $(selector).DataTable({
      serverSide: true,
      processing: true,
      searching: options.searching !== false,
      lengthMenu: options.lengthMenu || [[10, 25, 50, 100], [10, 25, 50, 100]],
      pageLength: options.pageLength || 10,
      order: options.order || [],
      columns: options.columns,
      language: PT_BR,
      ajax: {
        url: options.url,
        type: options.method || 'GET',
        data: function (d) {
          const temOrdem = d.order && d.order.length > 0;
          return {
            draw: d.draw,
            start: d.start,
            length: d.length,
            search: d.search ? d.search.value : '',
            orderColumn: temOrdem ? d.columns[d.order[0].column].data : '',
            orderDir: temOrdem ? d.order[0].dir : 'asc'
          };
        }
      }
    });
  };
})();
