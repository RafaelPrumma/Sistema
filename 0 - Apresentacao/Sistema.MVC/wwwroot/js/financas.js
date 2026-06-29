(function () {
  'use strict';

  const dashboard = document.getElementById('financeDashboard');
  if (!dashboard) return;

  const controller = new AbortController();
  const signal = controller.signal;
  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const pct = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const cssVar = (name, fallback) => getComputedStyle(document.body).getPropertyValue(name).trim() || fallback;
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  const islands = {
    patrimonio: document.getElementById('financePatrimonioIsland'),
    carteiras: document.getElementById('financeCarteirasIsland'),
    metas: document.getElementById('financeMetasIsland'),
    importacao: document.getElementById('financeImportacaoIsland'),
    posicoes: document.getElementById('financePosicoesIsland'),
    alertas: document.getElementById('financeAlertasIsland'),
    proventos: document.getElementById('financeProventosIsland'),
    calendarioProventos: document.getElementById('financeCalendarioProventosIsland'),
    reconciliacao: document.getElementById('financeReconciliacaoIsland'),
    reconciliacaoProventos: document.getElementById('financeReconciliacaoProventosIsland'),
    saudeCotacoes: document.getElementById('financeSaudeCotacoesIsland')
  };

  let proventosChart = null;
  let calendarioProventosChart = null;

  const PERIODOS = [
    { cod: '1D', label: '1D', dias: 1 },
    { cod: '5D', label: '5D', dias: 5 },
    { cod: '1M', label: '1M', dias: 31 },
    { cod: '6M', label: '6M', dias: 183 },
    { cod: 'YTD', label: 'YTD', dias: null },
    { cod: '1A', label: '1A', dias: 366 },
    { cod: 'MAX', label: 'MÁX', dias: null }
  ];

  let dados = null;
  let chart = null;
  let periodoAtual = '6M';
  let serieAtual = 'total';

  function escapeHtml(value) {
    return String(value ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  function setLoaded(island) {
    island?.setAttribute('aria-busy', 'false');
    if (window.AOS && island?.querySelector('[data-aos]')) {
      window.AOS.refreshHard();
    }
  }

  function setError(island, error) {
    if (!island) return;
    console.error('Falha ao carregar ilha financeira.', error);
    island.setAttribute('aria-busy', 'false');
    island.innerHTML = `
      <div class="finance-island-error" role="alert">
        <div>
          <i class="bi bi-exclamation-triangle d-block fs-4 mb-2"></i>
          Não foi possível carregar este conteúdo.
        </div>
      </div>`;
  }

  async function fetchChecked(url, accept) {
    const response = await fetch(url, {
      signal,
      headers: { Accept: accept }
    });
    if (!response.ok) throw new Error(`HTTP ${response.status} em ${url}`);
    return response;
  }

  async function loadPartial(island, url) {
    try {
      const response = await fetchChecked(url, 'text/html');
      island.innerHTML = await response.text();
      setLoaded(island);
    } catch (error) {
      if (error.name !== 'AbortError') setError(island, error);
      throw error;
    }
  }

  // Proventos: carrega a parcial e, como scripts em innerHTML não executam,
  // monta o gráfico ApexCharts aqui a partir dos dados embutidos em data-*.
  async function loadProventos(island, url) {
    await loadPartial(island, url);
    const el = island.querySelector('#financeProventosChart');
    if (!el || !window.ApexCharts) return;

    let labels = [];
    let recebido = [];
    let aReceber = [];
    try {
      labels = JSON.parse(el.dataset.labels || '[]');
      recebido = JSON.parse(el.dataset.recebido || '[]');
      aReceber = JSON.parse(el.dataset.areceber || '[]');
    } catch (error) {
      console.error('Falha ao ler série de proventos.', error);
      return;
    }

    // Ilha vem com 24 meses; o card mostra os últimos 12.
    const ini = Math.max(0, labels.length - 12);
    const dark = document.body.getAttribute('data-bs-theme') === 'dark';
    proventosChart = new ApexCharts(el, {
      chart: { type: 'bar', height: 260, stacked: true, fontFamily: 'inherit', toolbar: { show: false }, background: 'transparent', animations: { enabled: !prefersReducedMotion } },
      theme: { mode: dark ? 'dark' : 'light' },
      series: [
        { name: 'Recebido', data: recebido.slice(ini) },
        { name: 'A receber', data: aReceber.slice(ini) }
      ],
      colors: [corPositiva(), cssVar('--bs-info', '#0dcaf0')],
      plotOptions: { bar: { columnWidth: '55%', borderRadius: 4 } },
      dataLabels: { enabled: false },
      grid: { borderColor: 'rgba(148,163,184,.18)' },
      xaxis: { categories: labels.slice(ini), axisBorder: { show: false }, axisTicks: { show: false }, labels: { style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      yaxis: { labels: { formatter: compact, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      legend: { position: 'top', horizontalAlign: 'left' },
      tooltip: { theme: dark ? 'dark' : 'light', y: { formatter: value => money.format(value) } }
    });
    proventosChart.render();
  }

  // F-T calendário de proventos: gráfico mensal empilhado por tipo (Dividendo/JCP/Rendimento FII/Earn).
  // Mesma regra dos demais: script em parcial não roda; dados via data-*, monta aqui.
  async function loadCalendarioProventos(island, url) {
    await loadPartial(island, url);
    const el = island.querySelector('#financeCalendarioProventosChart');
    if (!el || !window.ApexCharts) return;

    let labels = [];
    let series = [];
    try {
      labels = JSON.parse(el.dataset.labels || '[]');
      series = JSON.parse(el.dataset.series || '[]');
    } catch (error) {
      console.error('Falha ao ler série do calendário de proventos.', error);
      return;
    }
    if (!series.length) return;

    const dark = document.body.getAttribute('data-bs-theme') === 'dark';
    // Paleta estável por tipo; cai para a paleta padrão do Apex se surgir um tipo novo.
    const coresTipo = {
      'Dividendo': cssVar('--bs-success', '#16a34a'),
      'JCP': cssVar('--bs-primary', '#0d6efd'),
      'Rendimento FII': cssVar('--bs-info', '#0dcaf0'),
      'Earn': cssVar('--bs-warning', '#ffc107'),
      'Outro': cssVar('--bs-secondary', '#6c757d')
    };
    const colors = series.map(s => coresTipo[s.name] || cssVar('--bs-secondary', '#6c757d'));

    calendarioProventosChart = new ApexCharts(el, {
      chart: { type: 'bar', height: 280, stacked: true, fontFamily: 'inherit', toolbar: { show: false }, background: 'transparent', animations: { enabled: !prefersReducedMotion } },
      theme: { mode: dark ? 'dark' : 'light' },
      series: series.map(s => ({ name: s.name, data: s.data })),
      colors,
      plotOptions: { bar: { columnWidth: '60%', borderRadius: 3 } },
      dataLabels: { enabled: false },
      grid: { borderColor: 'rgba(148,163,184,.18)' },
      xaxis: { categories: labels, axisBorder: { show: false }, axisTicks: { show: false }, labels: { rotate: -45, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      yaxis: { labels: { formatter: compact, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      legend: { position: 'top', horizontalAlign: 'left', labels: { colors: cssVar('--bs-body-color', '#212529') } },
      tooltip: { theme: dark ? 'dark' : 'light', y: { formatter: value => money.format(value) } }
    });
    calendarioProventosChart.render();
  }

  function corPositiva() { return cssVar('--bs-success', '#16a34a'); }
  function corNegativa() { return cssVar('--bs-danger', '#dc2626'); }

  function valoresDe(chave) {
    if (chave === 'total') return dados.total;
    return dados.setores.find(s => s.chave === chave)?.valores ?? dados.total;
  }

  function rotuloDe(chave) {
    if (chave === 'total') return 'Patrimônio total';
    return dados.setores.find(s => s.chave === chave)?.rotulo ?? 'Patrimônio total';
  }

  function variacaoDiaDe(chave) {
    if (chave === 'total') return dados.variacaoDiaTotal || 0;
    return dados.setores.find(s => s.chave === chave)?.variacaoDia || 0;
  }

  function indiceInicial() {
    const quantidade = dados.datas.length;
    const periodo = PERIODOS.find(x => x.cod === periodoAtual);
    if (!periodo || periodo.cod === 'MAX' || periodo.cod === '1A') return 0;
    if (periodo.cod === 'YTD') {
      const ano = new Date().getFullYear().toString();
      const indice = dados.datas.findIndex(data => data.startsWith(ano));
      return indice < 0 ? 0 : indice;
    }
    return Math.max(0, quantidade - 1 - periodo.dias);
  }

  function compact(value) {
    const absolute = Math.abs(value);
    if (absolute >= 1e6) return `R$ ${(value / 1e6).toFixed(1)}mi`;
    if (absolute >= 1e3) return `R$ ${(value / 1e3).toFixed(0)}k`;
    return money.format(value);
  }

  function pontos(valores, inicio) {
    return dados.datas.slice(inicio).map((data, index) => ({ x: data, y: valores[inicio + index] }));
  }

  function gradiente() {
    return {
      type: 'gradient',
      gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.02, stops: [0, 95] }
    };
  }

  function corAportes() { return cssVar('--bs-secondary-color', '#6c757d'); }

  // 'cor' pode ser uma cor única (só patrimônio) ou um array [patrimônio, aportes] quando a linha de
  // aportes está sobreposta. A linha de aportes é discreta (tracejada, cor secundária, sem preenchimento).
  function chartOptions(series, cor) {
    const dark = document.body.getAttribute('data-bs-theme') === 'dark';
    const comAportes = series.length > 1;
    const cores = Array.isArray(cor) ? cor : [cor];
    return {
      chart: {
        type: 'line',
        height: 340,
        toolbar: { show: false },
        zoom: { enabled: false },
        animations: { enabled: !prefersReducedMotion },
        fontFamily: 'inherit',
        background: 'transparent'
      },
      theme: { mode: dark ? 'dark' : 'light' },
      series,
      colors: cores,
      dataLabels: { enabled: false },
      stroke: { curve: 'smooth', width: comAportes ? [2, 2] : 2, dashArray: comAportes ? [0, 6] : 0 },
      fill: comAportes
        ? { type: ['gradient', 'solid'], gradient: gradiente().gradient, opacity: [1, 0] }
        : gradiente(),
      legend: { show: comAportes, position: 'top', horizontalAlign: 'left', labels: { colors: cssVar('--bs-body-color', '#212529') } },
      grid: { borderColor: 'rgba(148,163,184,.18)', strokeDashArray: 4 },
      xaxis: {
        type: 'datetime',
        labels: { datetimeUTC: true, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } },
        axisBorder: { show: false },
        axisTicks: { show: false },
        tooltip: { enabled: false }
      },
      yaxis: { labels: { formatter: compact, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      tooltip: { theme: dark ? 'dark' : 'light', shared: comAportes, x: { format: 'dd/MM/yyyy' }, y: { formatter: value => money.format(value) } }
    };
  }

  function sparkline(valores, inicio, cor) {
    const slice = valores.slice(inicio);
    if (slice.length < 2) return '';
    const min = Math.min(...slice);
    const max = Math.max(...slice);
    const span = max - min || 1;
    const width = 120;
    const height = 32;
    const step = width / (slice.length - 1);
    const points = slice
      .map((value, index) => `${(index * step).toFixed(1)},${(height - ((value - min) / span) * height).toFixed(1)}`)
      .join(' ');
    return `<svg class="finance-spark" viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" aria-hidden="true"><polyline fill="none" stroke="${cor}" stroke-width="2" points="${points}"/></svg>`;
  }

  function sectorCard(chave, rotulo, valores, inicio, atual, base, variacaoDia) {
    const variacao = atual - base;
    const positivoPeriodo = variacao >= 0;
    const percentual = base === 0 ? 0 : (variacao / base) * 100;
    const corDia = variacaoDia >= 0 ? corPositiva() : corNegativa();
    const ativo = chave === serieAtual ? ' finance-sector-card--active' : '';
    return `<button type="button" class="finance-sector-card${ativo}" data-serie="${escapeHtml(chave)}">
      <div class="finance-sector-top">
        <span class="finance-sector-name">${escapeHtml(rotulo)}</span>
        <span class="finance-sector-perc ${variacaoDia >= 0 ? 'text-success' : 'text-danger'}">${variacaoDia >= 0 ? '+' : ''}${pct.format(variacaoDia)}% hoje</span>
      </div>
      <div class="finance-sector-value">${money.format(atual)}</div>
      ${sparkline(valores, inicio, corDia)}
      <div class="finance-sector-foot ${positivoPeriodo ? 'text-success' : 'text-danger'}">${positivoPeriodo ? '+' : ''}${pct.format(percentual)}% no período</div>
    </button>`;
  }

  function renderSetores(inicio) {
    const container = document.getElementById('financeSetores');
    if (!container) return;
    const cards = [];
    const totalAtual = dados.valorAtualTotal > 0 ? dados.valorAtualTotal : dados.total.at(-1) || 0;
    cards.push(sectorCard('total', 'Patrimônio total', dados.total, inicio, totalAtual, dados.total[inicio] || 0, dados.variacaoDiaTotal || 0));
    dados.setores.forEach(setor => {
      const atual = setor.valorAtual > 0 ? setor.valorAtual : setor.valores.at(-1) || 0;
      const base = setor.valores[inicio] || 0;
      if (atual !== 0 || base !== 0) {
        cards.push(sectorCard(setor.chave, setor.rotulo, setor.valores, inicio, atual, base, setor.variacaoDia || 0));
      }
    });
    container.innerHTML = cards.join('');
    container.querySelectorAll('[data-serie]').forEach(button => {
      button.addEventListener('click', () => {
        serieAtual = button.dataset.serie;
        renderPatrimonioData();
      });
    });
  }

  function renderPeriodos() {
    const container = document.getElementById('financePeriodos');
    if (!container) return;
    container.innerHTML = PERIODOS.map(periodo =>
      `<button type="button" class="finance-period-btn${periodo.cod === periodoAtual ? ' active' : ''}" data-periodo="${periodo.cod}">${periodo.label}</button>`
    ).join('');
    container.querySelectorAll('[data-periodo]').forEach(button => {
      button.addEventListener('click', () => {
        periodoAtual = button.dataset.periodo;
        renderPeriodos();
        renderPatrimonioData();
      });
    });
  }

  function renderPatrimonioData() {
    const inicio = indiceInicial();
    const valores = valoresDe(serieAtual);
    const atual = valores.at(-1) || 0;
    const base = valores[inicio] || 0;
    const variacao = atual - base;
    const positivo = variacao >= 0;
    const percentual = base === 0 ? 0 : (variacao / base) * 100;
    const variacaoDia = variacaoDiaDe(serieAtual);
    const cor = positivo ? corPositiva() : corNegativa();

    document.getElementById('financeHeaderTitulo').textContent = rotuloDe(serieAtual);
    // F-Q: "Explique este valor" só faz sentido sobre o patrimônio total (a explicação decompõe o total
    // por fonte do preço). Em uma série de setor, mostra só o valor sem o gatilho.
    const valorEl = document.getElementById('financeHeaderValor');
    if (serieAtual === 'total') {
      valorEl.innerHTML = `${money.format(atual)} <i class="bi bi-info-circle fs-6 text-secondary align-text-top"></i>`;
      valorEl.setAttribute('data-explicar', 'patrimonio');
      valorEl.removeAttribute('disabled');
      valorEl.classList.remove('pe-none');
    } else {
      valorEl.textContent = money.format(atual);
      valorEl.removeAttribute('data-explicar');
      valorEl.classList.add('pe-none');
    }
    document.getElementById('financeHeaderDelta').innerHTML =
      `<span class="${positivo ? 'text-success' : 'text-danger'}">${positivo ? '+' : ''}${money.format(variacao)} (${positivo ? '+' : ''}${pct.format(percentual)}%)</span>` +
      `<span class="finance-hero-today ${variacaoDia >= 0 ? 'text-success' : 'text-danger'}">${variacaoDia >= 0 ? '+' : ''}${pct.format(variacaoDia)}% hoje</span>`;

    // Linha de aportes (custo acumulado) só faz sentido sobre o patrimônio total — não por setor.
    const aportes = serieAtual === 'total' ? (dados.custoAcumulado || []) : [];
    const temAportes = aportes.length === dados.datas.length && aportes.some(v => v !== 0);
    const series = [{ name: 'Patrimônio', type: 'area', data: pontos(valores, inicio) }];
    if (temAportes) series.push({ name: 'Aportes', type: 'line', data: pontos(aportes, inicio) });
    const cores = temAportes ? [cor, corAportes()] : cor;

    const chartContainer = document.getElementById('financeEvolucao');
    if (!chart && chartContainer && window.ApexCharts) {
      chart = new ApexCharts(chartContainer, chartOptions(series, cores));
      chart.render();
    } else if (chart) {
      chart.updateOptions(chartOptions(series, cores), false, !prefersReducedMotion);
    }
    renderSetores(inicio);
  }

  async function loadPatrimonio() {
    const island = islands.patrimonio;
    try {
      const response = await fetchChecked(dashboard.dataset.patrimonioUrl, 'application/json');
      const payload = await response.json();
      dados = payload.evolucao;
      island.innerHTML = `
        <div data-aos="fade-up" data-aos-duration="220">
          <div class="card finance-hero mb-3">
            <div class="card-body">
              <div class="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-2">
                <div>
                  <div class="text-secondary small" id="financeHeaderTitulo">Patrimônio total</div>
                  <button type="button" class="btn btn-link p-0 text-reset text-decoration-none border-0 finance-hero-value" id="financeHeaderValor" data-explicar="patrimonio" title="Explicar este valor">${money.format(payload.valorMercadoTotal)} <i class="bi bi-info-circle fs-6 text-secondary align-text-top"></i></button>
                  <div class="finance-hero-delta ${payload.resultadoNaoRealizadoTotal >= 0 ? 'text-success' : 'text-danger'}" id="financeHeaderDelta">${money.format(payload.resultadoNaoRealizadoTotal)}</div>
                </div>
                <div class="d-flex flex-column align-items-end gap-2">
                  <button class="btn btn-primary" type="button" data-abrir-modal="novaTransacaoModal"><i class="bi bi-plus-circle me-1"></i>Adicionar transação</button>
                  <a class="btn btn-outline-secondary btn-sm" href="/Financas/Resumo?preset=mes"><i class="bi bi-clipboard-data me-1"></i>Resumo do período</a>
                </div>
              </div>
              <div class="finance-period-bar" id="financePeriodos"></div>
              <div id="financeEvolucao" class="finance-chart"></div>
              <div class="small text-secondary mt-2"><i class="bi bi-info-circle me-1"></i>Leitura estimada a partir das suas transações e cotações públicas. Não é recomendação de investimento.</div>
            </div>
          </div>
          <h5 class="mb-2">Carteiras</h5>
          <div class="finance-sectors mb-4" id="financeSetores"></div>
        </div>`;

      if (!dados?.datas?.length) {
        document.getElementById('financeEvolucao').innerHTML = '<div class="text-secondary text-center py-5">Sem histórico suficiente para o gráfico.</div>';
      } else {
        renderPeriodos();
        renderPatrimonioData();
      }
      setLoaded(island);
    } catch (error) {
      if (error.name !== 'AbortError') setError(island, error);
      throw error;
    }
  }

  // ───────────────────────────────────────────────────────────────────────────
  // F-Q — "Explique este valor". Mecanismo reutilizável: qualquer elemento com
  // data-explicar="posicao|patrimonio" abre um modal Bootstrap com a composição/fonte do número,
  // buscada de um endpoint que lê SÓ os read models (sem recalcular transações na UI).
  // ───────────────────────────────────────────────────────────────────────────
  let explicarModal = null;
  let explicarModalEl = null;

  function tomClasse(tipo) {
    switch (tipo) {
      case 'positivo': return 'text-success';
      case 'negativo': return 'text-danger';
      case 'atencao': return 'text-warning-emphasis';
      default: return '';
    }
  }

  function severidadeBadge(sev) {
    const map = { ok: 'text-bg-success', atencao: 'text-bg-warning', critico: 'text-bg-danger' };
    return map[sev] || 'text-bg-secondary';
  }

  function garantirModal() {
    if (explicarModal) return explicarModal;
    if (!window.bootstrap) return null;
    explicarModalEl = document.createElement('div');
    explicarModalEl.className = 'modal fade';
    explicarModalEl.tabIndex = -1;
    explicarModalEl.setAttribute('aria-hidden', 'true');
    explicarModalEl.innerHTML = `
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title"><i class="bi bi-info-circle me-2"></i><span data-explicar-titulo>Explique este valor</span></h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Fechar"></button>
          </div>
          <div class="modal-body" data-explicar-corpo></div>
        </div>
      </div>`;
    document.body.appendChild(explicarModalEl);
    explicarModal = new window.bootstrap.Modal(explicarModalEl);
    return explicarModal;
  }

  function linhasHtml(linhas) {
    if (!linhas || !linhas.length) return '';
    return `<ul class="list-group list-group-flush">${linhas.map(l => {
      // Linha sem rótulo = nota explicativa (texto secundário em largura total).
      if (!l.rotulo) {
        return `<li class="list-group-item px-0 small text-secondary">${escapeHtml(l.valor)}</li>`;
      }
      return `<li class="list-group-item px-0 d-flex justify-content-between align-items-center gap-3">
        <span class="text-secondary">${escapeHtml(l.rotulo)}</span>
        <span class="fw-medium text-end ${tomClasse(l.tipo)}">${escapeHtml(l.valor)}</span>
      </li>`;
    }).join('')}</ul>`;
  }

  function linkTransacoes(busca) {
    const base = dashboard.dataset.transacoesUrl;
    if (!base || !busca) return '';
    const url = `${base}?busca=${encodeURIComponent(busca)}`;
    return `<div class="mt-3 text-end">
      <a class="btn btn-sm btn-outline-secondary" href="${url}">
        <i class="bi bi-list-ul me-1"></i>Ver transações de ${escapeHtml(busca)}
      </a></div>`;
  }

  function corpoCarregando() {
    return `<div class="text-center text-secondary py-4">
      <div class="spinner-border spinner-border-sm me-2" role="status"></div>Carregando explicação...</div>`;
  }

  function corpoErro() {
    return `<div class="text-center text-secondary py-4">
      <i class="bi bi-exclamation-triangle d-block fs-4 mb-2"></i>Não foi possível explicar este valor agora.</div>`;
  }

  function renderPosicao(corpo, titulo, dto) {
    if (!dto || !dto.encontrada) {
      titulo.textContent = 'Explique este valor';
      corpo.innerHTML = `<div class="text-secondary py-2">Posição não encontrada na projeção atual.</div>`;
      return;
    }
    titulo.textContent = `${dto.ticker} — composição do valor`;
    const fonte = `<div class="d-flex flex-wrap align-items-center gap-2 mb-3">
        <span class="badge ${severidadeBadge(dto.fonteSeveridade)}">Fonte: ${escapeHtml(dto.fontePreco)}</span>
        <span class="small text-secondary">${escapeHtml(dto.fonteStatus)}</span>
      </div>`;
    corpo.innerHTML = fonte + linhasHtml(dto.linhas) + linkTransacoes(dto.buscaTransacoes);
  }

  function renderPatrimonio(corpo, titulo, dto) {
    titulo.textContent = 'Patrimônio total — composição';
    if (!dto || !dto.temDados) {
      corpo.innerHTML = `<div class="text-secondary py-2">Sem posições para compor o patrimônio.</div>`;
      return;
    }
    corpo.innerHTML = linhasHtml(dto.linhas) +
      `<div class="small text-secondary mt-3"><i class="bi bi-info-circle me-1"></i>Soma das posições valoradas — sem recalcular transações.</div>`;
  }

  function renderCarteira(corpo, titulo, dto) {
    if (!dto || !dto.encontrada) {
      titulo.textContent = 'Explique este valor';
      corpo.innerHTML = `<div class="text-secondary py-2">Carteira não encontrada na projeção atual.</div>`;
      return;
    }
    titulo.textContent = `${dto.nome} — composição do valor`;
    corpo.innerHTML = linhasHtml(dto.linhas) +
      `<div class="small text-secondary mt-3"><i class="bi bi-info-circle me-1"></i>Soma das posições da carteira e subcarteiras — sem recalcular transações.</div>`;
  }

  function renderProventos(corpo, titulo, dto) {
    titulo.textContent = 'Proventos — composição (12M)';
    if (!dto || !dto.temDados) {
      corpo.innerHTML = `<div class="text-secondary py-2">Sem proventos recebidos nos últimos 12 meses.</div>`;
      return;
    }
    corpo.innerHTML = linhasHtml(dto.linhas) +
      `<div class="small text-secondary mt-3"><i class="bi bi-info-circle me-1"></i>Recebido (líquido) dos últimos 12 meses, lido dos proventos registrados.</div>`;
  }

  async function abrirExplicacao(trigger) {
    const modal = garantirModal();
    if (!modal) return;
    const tipo = trigger.dataset.explicar;
    const corpo = explicarModalEl.querySelector('[data-explicar-corpo]');
    const titulo = explicarModalEl.querySelector('[data-explicar-titulo]');
    titulo.textContent = trigger.dataset.titulo ? `${trigger.dataset.titulo}` : 'Explique este valor';
    corpo.innerHTML = corpoCarregando();
    modal.show();

    try {
      if (tipo === 'posicao') {
        const id = trigger.dataset.ativoId;
        const url = `${dashboard.dataset.explicarPosicaoUrl}?ativoId=${encodeURIComponent(id)}`;
        const dto = await (await fetchChecked(url, 'application/json')).json();
        renderPosicao(corpo, titulo, dto);
      } else if (tipo === 'patrimonio') {
        const dto = await (await fetchChecked(dashboard.dataset.explicarPatrimonioUrl, 'application/json')).json();
        renderPatrimonio(corpo, titulo, dto);
      } else if (tipo === 'carteira') {
        const id = trigger.dataset.carteiraId;
        const url = `${dashboard.dataset.explicarCarteiraUrl}?carteiraId=${encodeURIComponent(id)}`;
        const dto = await (await fetchChecked(url, 'application/json')).json();
        renderCarteira(corpo, titulo, dto);
      } else if (tipo === 'proventos') {
        const dto = await (await fetchChecked(dashboard.dataset.explicarProventosUrl, 'application/json')).json();
        renderProventos(corpo, titulo, dto);
      }
    } catch (error) {
      if (error.name === 'AbortError') return;
      console.error('Falha ao explicar valor.', error);
      corpo.innerHTML = corpoErro();
    }
  }

  // Delegação: dispara para qualquer gatilho atual ou futuro (ilhas carregam depois via innerHTML).
  dashboard.addEventListener('click', event => {
    const trigger = event.target.closest('[data-explicar]');
    if (!trigger || !dashboard.contains(trigger)) return;
    event.preventDefault();
    abrirExplicacao(trigger);
  });

  async function initialize() {
    try {
      await fetchChecked(dashboard.dataset.prepareUrl, 'application/json');
    } catch (error) {
      if (error.name === 'AbortError') return;
      Object.values(islands).forEach(island => setError(island, error));
      return;
    }

    await Promise.allSettled([
      loadPatrimonio(),
      loadPartial(islands.carteiras, dashboard.dataset.carteirasUrl),
      loadPartial(islands.metas, dashboard.dataset.metasUrl),
      loadPartial(islands.reconciliacao, dashboard.dataset.reconciliacaoUrl),
      loadPartial(islands.reconciliacaoProventos, dashboard.dataset.reconciliacaoProventosUrl),
      loadPartial(islands.saudeCotacoes, dashboard.dataset.saudeCotacoesUrl),
      loadPartial(islands.importacao, dashboard.dataset.importacaoUrl),
      loadPartial(islands.posicoes, dashboard.dataset.posicoesUrl),
      loadPartial(islands.alertas, dashboard.dataset.alertasUrl),
      loadProventos(islands.proventos, dashboard.dataset.proventosUrl),
      loadCalendarioProventos(islands.calendarioProventos, dashboard.dataset.calendarioProventosUrl)
    ]);
  }

  window.addEventListener('pagehide', () => {
    controller.abort();
    chart?.destroy();
    proventosChart?.destroy();
    calendarioProventosChart?.destroy();
    explicarModal?.dispose();
  }, { once: true });

  initialize();
})();
