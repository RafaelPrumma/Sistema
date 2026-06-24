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
    importacao: document.getElementById('financeImportacaoIsland'),
    posicoes: document.getElementById('financePosicoesIsland'),
    alertas: document.getElementById('financeAlertasIsland'),
    proventos: document.getElementById('financeProventosIsland'),
    reconciliacao: document.getElementById('financeReconciliacaoIsland')
  };

  let proventosChart = null;

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
    document.getElementById('financeHeaderValor').textContent = money.format(atual);
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
                  <div class="finance-hero-value" id="financeHeaderValor">${money.format(payload.valorMercadoTotal)}</div>
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
      loadPartial(islands.reconciliacao, dashboard.dataset.reconciliacaoUrl),
      loadPartial(islands.importacao, dashboard.dataset.importacaoUrl),
      loadPartial(islands.posicoes, dashboard.dataset.posicoesUrl),
      loadPartial(islands.alertas, dashboard.dataset.alertasUrl),
      loadProventos(islands.proventos, dashboard.dataset.proventosUrl)
    ]);
  }

  window.addEventListener('pagehide', () => {
    controller.abort();
    chart?.destroy();
    proventosChart?.destroy();
  }, { once: true });

  initialize();
})();
