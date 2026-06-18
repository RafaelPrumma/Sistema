import { chromium } from 'playwright';
import fs from 'node:fs/promises';

const baseUrl = process.env.LAYOUT_BASE_URL || 'http://127.0.0.1:5080';
const outDir = 'artifacts/screenshots';

async function ensureNoLayoutBreak(page, name) {
  const issues = await page.evaluate(() => {
    const selectors = [
      'html',
      'body',
      'header',
      'footer',
      'nav',
      '.layout-root',
      '.layout-main',
      '.layout-content',
      '.app-menu',
      '.container',
      '.container-fluid',
      '.surface-card',
      '.card',
      '.message-card',
      '.table-responsive'
    ];

    const offenders = [];
    const nodes = selectors.flatMap(selector => Array.from(document.querySelectorAll(selector)));
    for (const el of nodes) {
      if (!el) continue;
      if (el.scrollWidth - el.clientWidth > 8) {
        offenders.push({
          selector: el.tagName.toLowerCase(),
          className: String(el.className || ''),
          scrollWidth: el.scrollWidth,
          clientWidth: el.clientWidth
        });
      }
    }
    return offenders;
  });

  if (issues.length) {
    throw new Error(`Layout com overflow horizontal em ${name}: ${JSON.stringify(issues.slice(0, 5))}`);
  }
}

async function screenshot(page, file) {
  await fs.mkdir(outDir, { recursive: true });
  await page.screenshot({ path: `${outDir}/${file}`, fullPage: true });
}

async function openThemePanel(page) {
  await page.click('#temaToggle');
  await page.waitForSelector('#temaSidebar.show', { state: 'visible' });
}

async function closeThemePanel(page) {
  if (await page.locator('#temaSidebar.show').count()) {
    await page.click('#temaSidebarClose');
    await page.waitForSelector('#temaSidebar.show', { state: 'hidden' });
  }
}

async function setThemeToggle(page, id, checked) {
  const locator = page.locator(id);
  const isChecked = await locator.isChecked();
  if (isChecked !== checked) {
    await locator.click();
  }
  await page.waitForTimeout(150);
}

async function verifyRoute(page, url, name, file) {
  await page.goto(`${baseUrl}${url}`, { waitUntil: 'networkidle' });
  if (url === '/Financas') {
    await page.waitForSelector('#financePatrimonioIsland[aria-busy="false"]');
    await page.waitForSelector('#financeCarteirasIsland[aria-busy="false"]');
    await page.waitForSelector('#financeImportacaoIsland[aria-busy="false"]');
    await page.waitForSelector('#financeOperacionalIsland[aria-busy="false"]');
  }
  await ensureNoLayoutBreak(page, name);
  await screenshot(page, file);
}

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await context.newPage();

try {
  await page.goto(`${baseUrl}/Account/Login`, { waitUntil: 'networkidle' });
  await screenshot(page, '01-login.png');

  await page.fill('input[name="Cpf"]', '00000000000');
  await page.fill('input[name="Senha"]', 'admin123');
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/Home\/Index|\/$/);
  await page.waitForLoadState('networkidle');
  await page.waitForSelector('#appSplash', { state: 'detached' });

  await ensureNoLayoutBreak(page, 'home-default');
  await screenshot(page, '02-home-default.png');
  await page.goto(`${baseUrl}/Configuracao`, { waitUntil: 'networkidle' });
  if (await page.locator('#appSplash').count()) {
    throw new Error('Splash reapareceu durante a mesma sessão autenticada.');
  }
  await page.goto(`${baseUrl}/Home/Index`, { waitUntil: 'networkidle' });

  await openThemePanel(page);
  await page.click('[data-theme-preset="executivo"]');
  await setThemeToggle(page, '#HeaderFixo', true);
  await setThemeToggle(page, '#FooterFixo', false);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'theme-executivo');
  await screenshot(page, '03-theme-executivo.png');

  await openThemePanel(page);
  await page.click('[data-theme-preset="grafite"]');
  await setThemeToggle(page, '#MenuLateralExpandido', false);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'menu-colapsado');
  await screenshot(page, '04-menu-colapsado.png');

  const delayedPatrimonio = async route => {
    await new Promise(resolve => setTimeout(resolve, 450));
    await route.continue();
  };
  await page.route('**/Financas/Dashboard/Patrimonio', delayedPatrimonio);
  await page.goto(`${baseUrl}/Financas`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('#financePatrimonioIsland[aria-busy="true"] .finance-skeleton');
  await page.waitForSelector('#financePatrimonioIsland[aria-busy="false"]');
  await page.unroute('**/Financas/Dashboard/Patrimonio', delayedPatrimonio);

  const failOperacional = route => route.fulfill({ status: 500, body: 'falha simulada' });
  await page.route('**/Financas/Dashboard/Operacional', failOperacional);
  await page.goto(`${baseUrl}/Financas`, { waitUntil: 'networkidle' });
  await page.waitForSelector('#financeOperacionalIsland .finance-island-error');
  await page.waitForSelector('#financePatrimonioIsland[aria-busy="false"]');
  await page.unroute('**/Financas/Dashboard/Operacional', failOperacional);

  const routes = [
    ['/Home/Index', 'home', '05-home.png'],
    ['/Configuracao', 'configuracoes', '06-configuracoes.png'],
    ['/Tema/Edit', 'tema', '07-tema.png'],
    ['/Mensagem', 'mensagem-feed', '08-mensagem-feed.png'],
    ['/Mensagem/CaixaSaida', 'mensagem-saida', '09-mensagem-saida.png'],
    ['/Mensagem/Nova', 'mensagem-nova', '10-mensagem-nova.png'],
    ['/Documentacao', 'documentacao', '11-documentacao.png'],
    ['/Financas', 'financas-dashboard', '12-financas-dashboard.png'],
    ['/Financas/Documentos', 'financas-documentos', '13-financas-documentos.png'],
    ['/Financas/OperacoesB3', 'financas-b3', '14-financas-b3.png'],
    ['/Financas/OperacoesCripto', 'financas-cripto', '15-financas-cripto.png']
  ];

  for (const [url, name, file] of routes) {
    await verifyRoute(page, url, name, file);
  }

  const mobile = await context.newPage();
  await mobile.setViewportSize({ width: 390, height: 844 });
  await mobile.goto(`${baseUrl}/Home/Index`, { waitUntil: 'networkidle' });
  await mobile.click('.menu-hamburger');
  await mobile.waitForSelector('body.menu-mobile-open');
  await ensureNoLayoutBreak(mobile, 'mobile-menu-open');
  await screenshot(mobile, '16-mobile-menu-open.png');

  await openThemePanel(mobile);
  await mobile.click('[data-theme-preset="financeiro"]');
  await setThemeToggle(mobile, '#HeaderFixo', true);
  await closeThemePanel(mobile);
  await ensureNoLayoutBreak(mobile, 'mobile-theme-panel');
  await screenshot(mobile, '17-mobile-theme-panel.png');

  await mobile.close();

  const files = (await fs.readdir(outDir)).filter(f => f.endsWith('.png')).sort();
  console.log('Screenshots geradas:');
  for (const f of files) console.log(` - ${outDir}/${f}`);
} finally {
  await browser.close();
}
