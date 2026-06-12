(function () {
  'use strict';

  const container = document.getElementById('financeEvolucao');
  if (!container || typeof ApexCharts === 'undefined') return;

  const url = container.dataset.evolucaoUrl || '/Financas/Evolucao';
  const elPeriodos = document.getElementById('financePeriodos');
  const elValor = document.getElementById('financeHeaderValor');
  const elDelta = document.getElementById('financeHeaderDelta');
  const elTitulo = document.getElementById('financeHeaderTitulo');
  const elSetores = document.getElementById('financeSetores');

  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const pct = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const cssVar = (name, fallback) => (getComputedStyle(document.body).getPropertyValue(name).trim() || fallback);

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

  function corPositiva() { return cssVar('--bs-success', '#16a34a'); }
  function corNegativa() { return cssVar('--bs-danger', '#dc2626'); }

  function valoresDe(chave) {
    if (chave === 'total') return dados.total;
    const setor = dados.setores.find(s => s.chave === chave);
    return setor ? setor.valores : dados.total;
  }

  function rotuloDe(chave) {
    if (chave === 'total') return 'Patrimônio total';
    const setor = dados.setores.find(s => s.chave === chave);
    return setor ? setor.rotulo : 'Patrimônio total';
  }

  function variacaoDiaDe(chave) {
    if (chave === 'total') return dados.variacaoDiaTotal || 0;
    const setor = dados.setores.find(s => s.chave === chave);
    return setor ? (setor.variacaoDia || 0) : 0;
  }

  function indiceInicial() {
    const n = dados.datas.length;
    const p = PERIODOS.find(x => x.cod === periodoAtual);
    if (!p || p.cod === 'MAX' || p.cod === '1A') return 0;
    if (p.cod === 'YTD') {
      const ano = new Date().getFullYear().toString();
      const idx = dados.datas.findIndex(d => d.startsWith(ano));
      return idx < 0 ? 0 : idx;
    }
    return Math.max(0, n - 1 - p.dias);
  }

  function compact(v) {
    const abs = Math.abs(v);
    if (abs >= 1e6) return 'R$ ' + (v / 1e6).toFixed(1) + 'mi';
    if (abs >= 1e3) return 'R$ ' + (v / 1e3).toFixed(0) + 'k';
    return money.format(v);
  }

  function pontos(valores, ini) {
    const out = [];
    for (let i = ini; i < dados.datas.length; i++) out.push({ x: dados.datas[i], y: valores[i] });
    return out;
  }

  function render() {
    const ini = indiceInicial();
    const valores = valoresDe(serieAtual);
    const atual = valores.length ? valores[valores.length - 1] : 0;
    const base = valores[ini] || 0;
    const variacao = atual - base;
    const positivo = variacao >= 0;
    const cor = positivo ? corPositiva() : corNegativa();

    if (elValor) elValor.textContent = money.format(atual);
    if (elTitulo) elTitulo.textContent = rotuloDe(serieAtual);
    if (elDelta) {
      const sinal = positivo ? '+' : '';
      const perc = base === 0 ? 0 : (variacao / base) * 100;
      const vd = variacaoDiaDe(serieAtual);
      const labelPeriodo = (PERIODOS.find(x => x.cod === periodoAtual) || {}).label || '';
      elDelta.innerHTML =
        `<span class="${positivo ? 'text-success' : 'text-danger'}">${sinal}${money.format(variacao)} (${sinal}${pct.format(perc)}%) <small>${labelPeriodo}</small></span>` +
        `<span class="finance-hero-today ${vd >= 0 ? 'text-success' : 'text-danger'}">${vd >= 0 ? '+' : ''}${pct.format(vd)}% hoje</span>`;
      elDelta.className = 'finance-hero-delta';
    }

    const series = [{ name: rotuloDe(serieAtual), data: pontos(valores, ini) }];
    if (!chart) {
      chart = new ApexCharts(container, baseOptions(series, cor));
      chart.render();
    } else {
      chart.updateOptions({ colors: [cor], fill: gradiente(cor) }, false, false);
      chart.updateSeries(series, true);
    }

    renderSetores(ini);
  }

  function gradiente(cor) {
    return { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.02, stops: [0, 95] } };
  }

  function baseOptions(series, cor) {
    return {
      chart: { type: 'area', height: 340, toolbar: { show: false }, zoom: { enabled: false }, fontFamily: 'inherit', background: 'transparent' },
      theme: { mode: document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light' },
      series: series,
      colors: [cor],
      dataLabels: { enabled: false },
      stroke: { curve: 'smooth', width: 2 },
      fill: gradiente(cor),
      grid: { borderColor: 'rgba(148,163,184,.18)', strokeDashArray: 4 },
      xaxis: { type: 'datetime', labels: { datetimeUTC: true, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } }, axisBorder: { show: false }, axisTicks: { show: false }, tooltip: { enabled: false } },
      yaxis: { labels: { formatter: compact, style: { colors: cssVar('--bs-secondary-color', '#6c757d') } } },
      tooltip: { x: { format: 'dd/MM/yyyy' }, y: { formatter: v => money.format(v) } }
    };
  }

  function sparkline(valores, ini, cor) {
    const slice = valores.slice(ini);
    if (slice.length < 2) return '';
    const min = Math.min(...slice), max = Math.max(...slice);
    const span = max - min || 1;
    const w = 120, h = 32;
    const step = w / (slice.length - 1);
    const pts = slice.map((v, i) => `${(i * step).toFixed(1)},${(h - ((v - min) / span) * h).toFixed(1)}`).join(' ');
    return `<svg class="finance-spark" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none"><polyline fill="none" stroke="${cor}" stroke-width="2" points="${pts}"/></svg>`;
  }

  function renderSetores(ini) {
    if (!elSetores) return;
    const cards = [];
    const totalAtual = (dados.valorAtualTotal && dados.valorAtualTotal > 0) ? dados.valorAtualTotal : (dados.total[dados.total.length - 1] || 0);
    const totalBase = dados.total[ini] || 0;
    cards.push(cardSetor('total', 'Patrimônio total', dados.total, ini, totalAtual, totalBase, dados.variacaoDiaTotal || 0));
    dados.setores.forEach(s => {
      const serieAtualFim = s.valores[s.valores.length - 1] || 0;
      const atual = (s.valorAtual && s.valorAtual > 0) ? s.valorAtual : serieAtualFim;
      const base = s.valores[ini] || 0;
      if (atual === 0 && base === 0) return;
      cards.push(cardSetor(s.chave, s.rotulo, s.valores, ini, atual, base, s.variacaoDia || 0));
    });
    elSetores.innerHTML = cards.join('');
    elSetores.querySelectorAll('[data-serie]').forEach(el => {
      el.addEventListener('click', () => { serieAtual = el.dataset.serie; render(); });
    });
  }

  function cardSetor(chave, rotulo, valores, ini, atual, base, variacaoDia) {
    const variacao = atual - base;
    const positivoPer = variacao >= 0;
    const perc = base === 0 ? 0 : (variacao / base) * 100;
    const corDia = variacaoDia >= 0 ? corPositiva() : corNegativa();
    const ativo = chave === serieAtual ? ' finance-sector-card--active' : '';
    return `<button type="button" class="finance-sector-card${ativo}" data-serie="${chave}">
        <div class="finance-sector-top">
          <span class="finance-sector-name">${rotulo}</span>
          <span class="finance-sector-perc ${variacaoDia >= 0 ? 'text-success' : 'text-danger'}">${variacaoDia >= 0 ? '+' : ''}${pct.format(variacaoDia)}% hoje</span>
        </div>
        <div class="finance-sector-value">${money.format(atual)}</div>
        ${sparkline(valores, ini, corDia)}
        <div class="finance-sector-foot ${positivoPer ? 'text-success' : 'text-danger'}">${positivoPer ? '+' : ''}${pct.format(perc)}% no período</div>
      </button>`;
  }

  function renderBotoes() {
    if (!elPeriodos) return;
    elPeriodos.innerHTML = PERIODOS.map(p =>
      `<button type="button" class="finance-period-btn${p.cod === periodoAtual ? ' active' : ''}" data-periodo="${p.cod}">${p.label}</button>`
    ).join('');
    elPeriodos.querySelectorAll('[data-periodo]').forEach(btn => {
      btn.addEventListener('click', () => {
        periodoAtual = btn.dataset.periodo;
        elPeriodos.querySelectorAll('[data-periodo]').forEach(b => b.classList.toggle('active', b === btn));
        render();
      });
    });
  }

  fetch(url, { headers: { 'Accept': 'application/json' } })
    .then(r => r.json())
    .then(d => {
      dados = d;
      if (!dados || !dados.datas || dados.datas.length === 0) {
        container.innerHTML = '<div class="text-secondary text-center py-5">Sem histórico suficiente para o gráfico. Importe relatórios ou adicione transações e atualize as cotações.</div>';
        return;
      }
      renderBotoes();
      render();
    })
    .catch(() => {
      container.innerHTML = '<div class="text-danger text-center py-5">Não foi possível carregar a evolução do patrimônio.</div>';
    });
})();
