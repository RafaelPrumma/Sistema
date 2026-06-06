(function () {
  'use strict';

  const el = document.getElementById('financeChartData');
  if (!el) return;

  const data = JSON.parse(el.textContent || '{}');
  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

  function setup(canvas) {
    if (!canvas) return null;
    const rect = canvas.parentElement.getBoundingClientRect();
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.max(320, rect.width) * ratio;
    canvas.height = Math.max(220, rect.height) * ratio;
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    const ctx = canvas.getContext('2d');
    ctx.scale(ratio, ratio);
    return { ctx, width: canvas.width / ratio, height: canvas.height / ratio };
  }

  function drawAxes(ctx, width, height, padding) {
    ctx.strokeStyle = 'rgba(148, 163, 184, .35)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(padding.left, padding.top);
    ctx.lineTo(padding.left, height - padding.bottom);
    ctx.lineTo(width - padding.right, height - padding.bottom);
    ctx.stroke();
  }

  function drawBarChart(canvasId, rows) {
    const chart = setup(document.getElementById(canvasId));
    if (!chart || !rows?.length) return;
    const { ctx, width, height } = chart;
    const padding = { top: 18, right: 18, bottom: 42, left: 58 };
    const plotWidth = width - padding.left - padding.right;
    const plotHeight = height - padding.top - padding.bottom;
    const max = Math.max(...rows.flatMap(x => [x.compras || 0, x.vendas || 0]), 1);
    const group = plotWidth / rows.length;
    const barWidth = Math.max(8, Math.min(26, group / 3));

    ctx.clearRect(0, 0, width, height);
    drawAxes(ctx, width, height, padding);
    ctx.font = '12px system-ui, sans-serif';
    ctx.fillStyle = getComputedStyle(document.body).getPropertyValue('--bs-body-color') || '#111827';
    ctx.fillText(money.format(max), 8, padding.top + 8);

    rows.forEach((row, index) => {
      const x = padding.left + index * group + group / 2;
      const comprasH = ((row.compras || 0) / max) * plotHeight;
      const vendasH = ((row.vendas || 0) / max) * plotHeight;
      const base = height - padding.bottom;
      ctx.fillStyle = '#0d6efd';
      ctx.fillRect(x - barWidth - 2, base - comprasH, barWidth, comprasH);
      ctx.fillStyle = '#20c997';
      ctx.fillRect(x + 2, base - vendasH, barWidth, vendasH);
      ctx.save();
      ctx.translate(x - 8, height - 16);
      ctx.rotate(-Math.PI / 6);
      ctx.fillStyle = 'rgba(108, 117, 125, .95)';
      ctx.fillText(row.label, 0, 0);
      ctx.restore();
    });

    ctx.fillStyle = '#0d6efd';
    ctx.fillRect(width - 150, 12, 10, 10);
    ctx.fillStyle = getComputedStyle(document.body).getPropertyValue('--bs-body-color') || '#111827';
    ctx.fillText('Compras', width - 136, 22);
    ctx.fillStyle = '#20c997';
    ctx.fillRect(width - 78, 12, 10, 10);
    ctx.fillStyle = getComputedStyle(document.body).getPropertyValue('--bs-body-color') || '#111827';
    ctx.fillText('Vendas', width - 64, 22);
  }

  function drawLineChart(canvasId, rows) {
    const chart = setup(document.getElementById(canvasId));
    if (!chart || !rows?.length) return;
    const { ctx, width, height } = chart;
    const padding = { top: 18, right: 18, bottom: 42, left: 58 };
    const plotWidth = width - padding.left - padding.right;
    const plotHeight = height - padding.top - padding.bottom;
    const max = Math.max(...rows.flatMap(x => [x.compras || 0, x.vendas || 0]), 1);

    ctx.clearRect(0, 0, width, height);
    drawAxes(ctx, width, height, padding);
    ctx.font = '12px system-ui, sans-serif';

    function point(row, index, key) {
      const x = padding.left + (rows.length === 1 ? 0 : (index / (rows.length - 1)) * plotWidth);
      const y = height - padding.bottom - ((row[key] || 0) / max) * plotHeight;
      return [x, y];
    }

    function series(key, color) {
      ctx.strokeStyle = color;
      ctx.lineWidth = 2;
      ctx.beginPath();
      rows.forEach((row, index) => {
        const [x, y] = point(row, index, key);
        if (index === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      });
      ctx.stroke();
    }

    series('compras', '#0d6efd');
    series('vendas', '#20c997');

    const step = Math.max(1, Math.ceil(rows.length / 8));
    ctx.fillStyle = 'rgba(108, 117, 125, .95)';
    rows.forEach((row, index) => {
      if (index % step !== 0) return;
      const [x] = point(row, index, 'compras');
      ctx.save();
      ctx.translate(x - 8, height - 16);
      ctx.rotate(-Math.PI / 6);
      ctx.fillText(row.label, 0, 0);
      ctx.restore();
    });
  }

  function render() {
    drawBarChart('chartB3Ano', data.b3Ano || []);
    drawLineChart('chartB3Mes', data.b3Mes || []);
  }

  window.addEventListener('resize', () => window.requestAnimationFrame(render));
  render();
})();
